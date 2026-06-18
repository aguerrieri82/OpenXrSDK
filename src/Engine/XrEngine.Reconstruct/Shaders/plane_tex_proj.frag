#version 310 es
precision highp float;

layout(location = 0) out vec4 outAccum;

in vec2 vUv; // 0..1 atlas uv

layout(binding = 0) uniform sampler2D uPrevAccum;
layout(binding = 1) uniform sampler2D uDepth;
layout(binding = 2) uniform sampler2D uColor;

layout(std140, binding = 11) uniform PlaneAtlasBlock
{
    mat4 uDepthViewProj;
    mat4 uDepthViewProjInv;
    mat4 uColorViewProj;

    vec3 uPlaneOrigin;
    vec3 uPlaneU;
    vec3 uPlaneV;
    vec3 uPlaneNormal;
    vec2 uPlaneSizeMeters;

    vec3 uColorCameraWorldPos;

    vec2 uDepthTexSize;
    vec2 uColorTexSize;

    float uPlaneTolerance;
    float uPlaneErrorFalloff;
    float uAnglePower;
    float uGoodDistance;
    float uDistanceFalloff;
    float uFootprintStart;
    float uFootprintEnd;
    float uFrameWeight;
    float uMaxWeight;
    float uMinAccumWeight;
};

vec3 ndcToWorld(vec3 ndc)
{
    vec4 w = uDepthViewProjInv * vec4(ndc, 1.0);
    return w.xyz / w.w;
}

bool projectToUv(mat4 m, vec3 world, out vec2 uv, out float ndcZ)
{
    vec4 clip = m * vec4(world, 1.0);

    if (clip.w <= 0.0)
        return false;

    vec3 ndc = clip.xyz / clip.w;

    if (ndc.x < -1.0 || ndc.x > 1.0 ||
        ndc.y < -1.0 || ndc.y > 1.0 ||
        ndc.z < -1.0 || ndc.z > 1.0)
        return false;

    uv = ndc.xy * 0.5 + 0.5;
    uv.y = 1.0 - uv.y;
    ndcZ = ndc.z;
    return true;
}

void main()
{
    vec4 prev = texture(uPrevAccum, vUv);

    vec2 p = (vUv - 0.5) * uPlaneSizeMeters;

    vec3 planeWorld =
        uPlaneOrigin +
        uPlaneU * p.x +
        uPlaneV * p.y;

    vec2 depthUv;
    float expectedNdcZ;

    if (!projectToUv(uDepthViewProj, planeWorld, depthUv, expectedNdcZ))
    {
        outAccum = prev;
        return;
    }

    vec2 depthBorder = vec2(2.0) / uDepthTexSize;
    if (depthUv.x < depthBorder.x || depthUv.x > 1.0 - depthBorder.x ||
        depthUv.y < depthBorder.y || depthUv.y > 1.0 - depthBorder.y)
    {
        outAccum = prev;
        return;
    }

    float depth01 = texture(uDepth, depthUv).r;

    if (depth01 <= 0.0 || depth01 >= 1.0)
    {
        outAccum = prev;
        return;
    }

    float depthNdcZ = depth01 * 2.0 - 1.0;

    vec2 depthNdcXY = depthUv * 2.0 - 1.0;
    depthNdcXY.y = -depthNdcXY.y;

    vec3 measuredWorld = ndcToWorld(vec3(depthNdcXY, depthNdcZ));

    vec3 N = normalize(uPlaneNormal);
    vec3 delta = measuredWorld - planeWorld;

    float planeError = abs(dot(delta, N));
    if (planeError > uPlaneTolerance)
    {
        outAccum = prev;
        return;
    }

    vec2 colorUv;
    float colorNdcZ;

    if (!projectToUv(uColorViewProj, planeWorld, colorUv, colorNdcZ))
    {
        outAccum = prev;
        return;
    }

    vec2 colorBorder = vec2(2.0) / uColorTexSize;
    if (colorUv.x < colorBorder.x || colorUv.x > 1.0 - colorBorder.x ||
        colorUv.y < colorBorder.y || colorUv.y > 1.0 - colorBorder.y)
    {
        outAccum = prev;
        return;
    }

    vec3 color = texture(uColor, colorUv).rgb;

    float planeT = planeError / max(uPlaneTolerance, 1e-6);
    float planeQuality = exp(-uPlaneErrorFalloff * planeT * planeT);

    vec3 V = normalize(uColorCameraWorldPos - planeWorld);

    float incidence = abs(dot(N, V));
    float angleWeight = pow(incidence, uAnglePower);

    float dist = length(uColorCameraWorldPos - planeWorld);
    float distX = max(dist - uGoodDistance, 0.0);
    float distanceWeight = exp(-uDistanceFalloff * distX * distX);

    vec2 colorPix = colorUv * uColorTexSize;
    vec2 ddxPix = dFdx(colorPix);
    vec2 ddyPix = dFdy(colorPix);
    float footprint = max(length(ddxPix), length(ddyPix));

    float footprintWeight =
        1.0 - smoothstep(uFootprintStart, uFootprintEnd, footprint);

    float confidence =
        planeQuality *
        angleWeight *
        distanceWeight *
        footprintWeight;

    float w = uFrameWeight * confidence;

    if (w < uMinAccumWeight)
    {
        outAccum = prev;
        return;
    }

    float oldW = prev.a;
    float addW = min(w, max(uMaxWeight - oldW, 0.0));

    outAccum = vec4(prev.rgb + color * addW, oldW + addW);
}