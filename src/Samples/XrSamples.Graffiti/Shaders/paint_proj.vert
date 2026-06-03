
#include "./uniforms.glsl"

layout(location = 0) in vec3 aPosition;


out vec2 vBrushCoord01;
out float vRayLength;

void BuildBasis(
    vec3 n,
    out vec3 tangent,
    out vec3 bitangent)
{
    vec3 helper =
        abs(dot(n, vec3(0.0, 1.0, 0.0))) < 0.999
            ? vec3(0.0, 1.0, 0.0)
            : vec3(1.0, 0.0, 0.0);

    tangent = normalize(cross(helper, n));
    bitangent = normalize(cross(n, tangent));
}

vec4 NormalizeQuat(vec4 q)
{
    return normalize(q);
}

vec4 NlerpQuat(vec4 a, vec4 b, float t)
{
    if (dot(a, b) < 0.0)
        b = -b;
    return normalize(mix(a, b, t));
}

mat3 QuatToMat3(vec4 q)
{
    q = normalize(q);

    float x = q.x;
    float y = q.y;
    float z = q.z;
    float w = q.w;

    float xx = x * x;
    float yy = y * y;
    float zz = z * z;

    float xy = x * y;
    float xz = x * z;
    float yz = y * z;

    float wx = w * x;
    float wy = w * y;
    float wz = w * z;

    return mat3(
        1.0 - 2.0 * (yy + zz),
        2.0 * (xy + wz),
        2.0 * (xz - wy),

        2.0 * (xy - wz),
        1.0 - 2.0 * (xx + zz),
        2.0 * (yz + wx),

        2.0 * (xz + wy),
        2.0 * (yz - wx),
        1.0 - 2.0 * (xx + yy)
    );
}

mat4 BuildTransform(vec3 position, vec4 rotation, vec3 scale)
{
    mat3 r = QuatToMat3(rotation);

    r[0] *= scale.x;
    r[1] *= scale.y;
    r[2] *= scale.z;

    return mat4(
        vec4(r[0], 0.0),
        vec4(r[1], 0.0),
        vec4(r[2], 0.0),
        vec4(position, 1.0)
    );
}


mat4 GetHostLocalToWorld()
{
#ifndef USE_INSTANCE
    return uHostLocalToWorld;
#else
    float t =
        uStepCount <= 1
            ? 1.0
            : (float(gl_InstanceID) + 0.5) / float(uStepCount);

    vec3 position = mix(uPrevPosition, uCurPosition, t);
    vec4 rotation = NlerpQuat(uPrevRotation, uCurRotation, t);

    return BuildTransform(position, rotation, uHostScale);
#endif
}

void main()
{
    mat4 hostLocalToWord = GetHostLocalToWorld();

    vec3 sprayDirLocal = normalize(uSprayDirectionLocal);

    vec3 tangentLocal;
    vec3 bitangentLocal;

    BuildBasis(sprayDirLocal, tangentLocal, bitangentLocal);

    float angle = max(uSpreadAngle, 0.0001);
    float h = uSprayRadius / tan(angle);

    vec3 apexLocal =
        uSprayCenterLocal - sprayDirLocal * h;

    /*
        Brush mesh is normalized in local XY:
        diameter = 1
        radius   = 0.5

        aPosition.xy therefore gets scaled by:
        2 * uSprayRadius
    */
    vec3 apertureLocal =
        uSprayCenterLocal
        + tangentLocal   * (aPosition.x * uSprayRadius * 2.0)
        + bitangentLocal * (aPosition.y * uSprayRadius * 2.0);

    vec3 rayDirLocal =
        normalize(apertureLocal - apexLocal);

    vec3 rayOriginWorld =
        (hostLocalToWord * vec4(apertureLocal, 1.0)).xyz;

    vec3 rayDirWorld =
        normalize((hostLocalToWord * vec4(rayDirLocal, 0.0)).xyz);

    /*
        Intersect ray with canvas plane in canvas-local space.
        Canvas plane is local z = 0.
    */


    vec3 rayOriginCanvas =
        (uCanvasWorldToLocal * vec4(rayOriginWorld, 1.0)).xyz;

    vec3 rayDirCanvas =
        normalize((uCanvasWorldToLocal * vec4(rayDirWorld, 0.0)).xyz);

    float denom = rayDirCanvas.z;

    if (abs(denom) < 0.00001)
    {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
        vRayLength = 999999.0;
        return;
    }

    float t = -rayOriginCanvas.z / denom;

    if (t <= 0.0)
    {
        gl_Position = vec4(2.0, 2.0, 2.0, 1.0);
        vRayLength = 999999.0;
        return;
    }

    vec3 hitCanvas =
        rayOriginCanvas + rayDirCanvas * t;

    vec2 uv =
        hitCanvas.xy / uCanvasSize + vec2(0.5);

    vec3 hitWorld =
        (uCanvasLocalToWorld * vec4(hitCanvas, 1.0)).xyz;

    vRayLength =
        length(hitWorld - rayOriginWorld);

    vec2 ndc = vec2(uv.x, 1.0 - uv.y) * 2.0 - 1.0;
    
    vBrushCoord01 = aPosition.xy * 2.0;

    gl_Position = vec4(ndc, 0.0, 1.0);
}