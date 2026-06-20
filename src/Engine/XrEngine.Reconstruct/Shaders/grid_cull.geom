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

    vec3 normal = normalize(cross(p1 - p0, p2 - p0));

    float frontness = abs(dot(normal, normalize(uCaptureCameraRight)));

    if (uCullLateralFaces && frontness > uMinFrontness)
    {
        EmitTriangle(3, maxEdge, maxLen, distanceToCapture, frontness);
        return;
    }

    EmitTriangle(0, maxEdge, maxLen, distanceToCapture, frontness);
}