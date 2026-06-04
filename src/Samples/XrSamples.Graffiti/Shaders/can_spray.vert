#include "./uniforms.glsl"
#include "./functions.glsl"
#include "[XrEngine.Core]Shared/uniforms.glsl" 
#include "[XrEngine.Core]Shared/position.glsl"


layout(location = 0) in vec3 aPosition;

out float vRayT;
out float vRayLength;
out vec3 vWorldPos;

flat out float vLineSeed;
flat out vec3 vLineStartWorld;
flat out vec3 vLineDirWorld;

uniform float uSprayFarDistance;
uniform float uTime;

/*
    Direction jitter amount.

    0.015 = subtle
    0.030 = visible but still coherent
    0.060 = more chaotic

    With SprayFarDistance = 1.0, 0.03 means roughly a few cm
    transverse deviation at the far endpoint.
*/
const float RayJitter = 0.02;

float Hash11(float x)
{
    return fract(sin(x * 127.1) * 43758.5453123);
}

vec2 Hash12(float x)
{
    return vec2(
        Hash11(x + 11.13),
        Hash11(x + 47.71)
    );
}

vec2 Rotate2(vec2 p, float a)
{
    float s = sin(a);
    float c = cos(a);

    return vec2(
        p.x * c - p.y * s,
        p.x * s + p.y * c
    );
}

void main()
{
    mat4 hostLocalToWorld = GetHostLocalToWorld();

    bool isFar =
        (gl_VertexID & 1) != 0;

    float lineSeed =
        float(gl_VertexID >> 1);

    vec2 circlePoint =
        aPosition.xy;

    const float ApertureRotationSpeed = 0.75;

    float rndRot =
        Hash11(lineSeed + 91.37);

    float apertureAngle =
        uTime * ApertureRotationSpeed
        + rndRot * 6.28318530718;

    circlePoint =
        Rotate2(circlePoint, apertureAngle);

    vec3 sprayDirLocal =
        normalize(uSprayDirectionLocal);

    vec3 tangentLocal;
    vec3 bitangentLocal;

    BuildBasis(
        sprayDirLocal,
        tangentLocal,
        bitangentLocal);

    /*
        Aperture/source point on the spray disk.

        Source mesh radius = 0.5.
        Multiplying by 2.0 maps it to radius 1.0.
        Scaling by uSprayRadius gives real aperture radius.
    */
    vec3 sourceLocal =
        uSprayCenterLocal
        + tangentLocal   * (circlePoint.x * uSprayRadius * 2.0)
        + bitangentLocal * (circlePoint.y * uSprayRadius * 2.0);

    /*
        Cone apex from spread angle.
    */
    float angle =
        max(uSpreadAngle, 0.0001);

    float h =
        uSprayRadius / tan(angle);

    vec3 apexLocal =
        uSprayCenterLocal - sprayDirLocal * h;

    /*
        Ideal cone ray direction:
        from apex through source aperture point.
    */
    vec3 rayDirLocal =
        normalize(sourceLocal - apexLocal);

    vec3 sourceWorld =
        (hostLocalToWorld * vec4(sourceLocal, 1.0)).xyz;

    vec3 rayDirWorld =
        normalize((hostLocalToWorld * vec4(rayDirLocal, 0.0)).xyz);

    /*
        Deterministic per-line random angular deviation.
        The near point remains fixed.
        Only the visual ray direction is perturbed.
    */
    vec2 rnd =
        Hash12(lineSeed) * 2.0 - 1.0;

    vec3 helper =
        abs(dot(rayDirWorld, vec3(0.0, 1.0, 0.0))) < 0.999
            ? vec3(0.0, 1.0, 0.0)
            : vec3(1.0, 0.0, 0.0);

    vec3 rayTangentWorld =
        normalize(cross(helper, rayDirWorld));

    vec3 rayBitangentWorld =
        normalize(cross(rayDirWorld, rayTangentWorld));

    vec3 jitterDirWorld =
        rayTangentWorld * rnd.x
        + rayBitangentWorld * rnd.y;

    float jitterLen =
        length(jitterDirWorld);

    if (jitterLen > 0.00001)
        jitterDirWorld /= jitterLen;

    vec3 visualRayDirWorld =
        normalize(rayDirWorld + jitterDirWorld * RayJitter);

    /*
        Near point is exactly the source point.
        Far point is source + jittered direction * uSprayFarDistance.
    */
    float finalDistance =
        isFar ? uSprayFarDistance : 0.0;

    if (isFar)
    {
        /*
            Clip the far point if the ray hits the actual canvas first.

            Canvas plane is local z = 0.
            We also require the hit to lie inside the canvas rectangle.
        */
        vec3 rayOriginCanvas =
            (uCanvasWorldToLocal * vec4(sourceWorld, 1.0)).xyz;

        vec3 rayDirCanvas =
            (uCanvasWorldToLocal * vec4(visualRayDirWorld, 0.0)).xyz;

        if (abs(rayDirCanvas.z) > 0.00001)
        {
            float tHit =
                -rayOriginCanvas.z / rayDirCanvas.z;

            if (tHit > 0.0 && tHit < finalDistance)
            {
                vec3 hitCanvas =
                    rayOriginCanvas + rayDirCanvas * tHit;

                vec2 halfCanvas =
                    uCanvasSize * 0.5;

                bool insideCanvas =
                    abs(hitCanvas.x) <= halfCanvas.x &&
                    abs(hitCanvas.y) <= halfCanvas.y;

                if (insideCanvas)
                    finalDistance = tHit;
            }
        }
    }

    vec3 pointWorld =
        sourceWorld + visualRayDirWorld * finalDistance;

    vRayT =
        uSprayFarDistance > 0.0
            ? finalDistance / uSprayFarDistance
            : 0.0;

    vRayT =
        clamp(vRayT, 0.0, 1.0);

    vRayLength =
        finalDistance;

    vLineSeed =
        lineSeed;

    vWorldPos =
        pointWorld;

    vLineStartWorld =
        sourceWorld;

    /*
        Important: fragment shader must use the same visual direction,
        otherwise meter-based dot spacing is slightly wrong after jitter.
    */
    vLineDirWorld =
        visualRayDirWorld;

    gl_Position =
        getViewProj() * vec4(pointWorld, 1.0);
}