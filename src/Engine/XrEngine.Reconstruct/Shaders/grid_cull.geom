layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in VS_OUT
{
    vec3 worldPos;
    vec2 uv;
} gs_in[];

out GS_OUT
{
    vec3 worldPos;
    vec2 uv;

    flat int reason;
    flat float maxEdge;
    flat float maxLen;
    flat float distanceToCapture;
    flat float frontness;
} gs_out;

uniform vec3 uCaptureCameraPos;
uniform vec3 uCaptureCameraForward;
uniform vec3 uCaptureCameraRight;

uniform bool uCullInvalidUv;
uniform bool uCullLongEdge;
uniform bool uCullLateralFaces;
uniform bool uCullDistance;

uniform float uMaxEdgeBase;
uniform float uMaxEdgePerMeter;

uniform float uMinFrontness;
uniform float uMaxCaptureDistance;

bool IsUvValid(vec2 uv)
{
    return uv.x >= 0.0 && uv.x <= 1.0 &&
           uv.y >= 0.0 && uv.y <= 1.0;
}

void EmitTriangle(
    int reason,
    float maxEdge,
    float maxLen,
    float distanceToCapture,
    float frontness)
{
    for (int i = 0; i < 3; i++)
    {
        gs_out.worldPos = gs_in[i].worldPos;
        gs_out.uv = gs_in[i].uv;

        gs_out.reason = reason;
        gs_out.maxEdge = maxEdge;
        gs_out.maxLen = maxLen;
        gs_out.distanceToCapture = distanceToCapture;
        gs_out.frontness = frontness;

        gl_Position = gl_in[i].gl_Position;
        EmitVertex();
    }

    EndPrimitive();
}

void main()
{
    if (uCullInvalidUv &&
        (!IsUvValid(gs_in[0].uv) ||
         !IsUvValid(gs_in[1].uv) ||
         !IsUvValid(gs_in[2].uv)))
    {
        EmitTriangle(1, 0.0, 0.0, 0.0, 0.0);
        return;
    }

    vec3 p0 = gs_in[0].worldPos;
    vec3 p1 = gs_in[1].worldPos;
    vec3 p2 = gs_in[2].worldPos;

    vec3 e01 = p1 - p0;
    vec3 e12 = p2 - p1;
    vec3 e20 = p0 - p2;

    float l01 = length(e01);
    float l12 = length(e12);
    float l20 = length(e20);

    float maxLen = max(l01, max(l12, l20));

    vec3 center = (p0 + p1 + p2) / 3.0;
    vec3 captureForward = normalize(uCaptureCameraForward);

    float d0 = abs(dot(p0 - uCaptureCameraPos, captureForward));
    float d1 = abs(dot(p1 - uCaptureCameraPos, captureForward));
    float d2 = abs(dot(p2 - uCaptureCameraPos, captureForward));

    float distanceToCapture = min(d0, min(d1, d2));

    float maxEdge = uMaxEdgeBase + distanceToCapture * uMaxEdgePerMeter;

    if (uCullDistance && distanceToCapture > uMaxCaptureDistance)
    {
        EmitTriangle(4, maxEdge, maxLen, distanceToCapture, 0.0);
        return;
    }

    if (uCullLongEdge && maxLen > maxEdge)
    {
        EmitTriangle(2, maxEdge, maxLen, distanceToCapture, 0.0);
        return;
    }

    /*
    vec3 n = normalize(cross(p1 - p0, p2 - p0));

    vec3 camRight = normalize(uCaptureCameraRight);
    vec3 camForward = normalize(uCaptureCameraForward);
    vec3 camUp = normalize(cross(camRight, camForward));

    vec3 r0 = normalize(p0 - uCaptureCameraPos);
    vec3 r1 = normalize(p1 - uCaptureCameraPos);
    vec3 r2 = normalize(p2 - uCaptureCameraPos);

    // Normal of the local vertical ray-plane.
    // At image center this is ~cameraRight.
    // Near the frustum sides it rotates with the ray.
    vec3 side0 = normalize(cross(camUp, r0));
    vec3 side1 = normalize(cross(camUp, r1));
    vec3 side2 = normalize(cross(camUp, r2));

    float lateral0 = abs(dot(n, side0));
    float lateral1 = abs(dot(n, side1));
    float lateral2 = abs(dot(n, side2));

    // Require all triangle rays to agree that this is lateral.
    float lateral = min(lateral0, min(lateral1, lateral2));

    if (uCullLateralFaces && lateral > uMinFrontness)
    {
        EmitTriangle(3, maxEdge, maxLen, distanceToCapture, lateral);
        return;
    }
    */

    float frontness = 0.0;

    if (uCullLateralFaces)
    {
        vec2 uv0 = gs_in[0].uv;
        vec2 uv1 = gs_in[1].uv;
        vec2 uv2 = gs_in[2].uv;

        vec3 e1 = p1 - p0;
        vec3 e2 = p2 - p0;

        vec2 duv1 = uv1 - uv0;
        vec2 duv2 = uv2 - uv0;

        float det = duv1.x * duv2.y - duv2.x * duv1.y;

        if (abs(det) > 0.00000001)
        {
            float invDet = 1.0 / det;

            vec3 tangent = normalize((e1 * duv2.y - e2 * duv1.y) * invDet);

            vec3 ray = normalize(center - uCaptureCameraPos);

            frontness = abs(dot(tangent, ray));

            if (frontness > uMinFrontness)
            {
                EmitTriangle(3, maxEdge, maxLen, distanceToCapture, frontness);
                return;
            }
        }
    }

    EmitTriangle(0, maxEdge, maxLen, distanceToCapture, frontness);
}