#include "./uniforms.glsl"
#include "./functions.glsl"

layout(location = 0) in vec3 aPosition;

out float vRayT;
out float vRayLength;
out vec3 vWorldPos;

flat out float vLineSeed;
flat out vec3 vLineStartWorld;
flat out vec3 vLineDirWorld;

uniform mat4 uViewProjection;
uniform float uSprayFarDistance;

void main()
{
    mat4 hostLocalToWorld = GetHostLocalToWorld();

    /*
        Mesh layout must be:
        A, A, B, B, C, C, ...
        Draw with GL_LINES.

        Even vertex = near point
        Odd  vertex = far  point
    */
    bool isFar = (gl_VertexID & 1) != 0;

    /*
        Source circle point.
        Input circle radius is 0.5.
    */
    vec2 circlePoint = aPosition.xy;

    vec3 sprayDirLocal = normalize(uSprayDirectionLocal);

    vec3 tangentLocal;
    vec3 bitangentLocal;
    BuildBasis(sprayDirLocal, tangentLocal, bitangentLocal);

    /*
        Aperture/source point on the spray disk.
        Since the source mesh radius is 0.5,
        multiplying by 2.0 maps it to radius 1.0,
        then scaling by uSprayRadius gives the real radius.
    */
    vec3 sourceLocal =
        uSprayCenterLocal
        + tangentLocal   * (circlePoint.x * uSprayRadius * 2.0)
        + bitangentLocal * (circlePoint.y * uSprayRadius * 2.0);

    /*
        Cone apex from spread angle.
    */
    float angle = max(uSpreadAngle, 0.0001);
    float h = uSprayRadius / tan(angle);

    vec3 apexLocal =
        uSprayCenterLocal - sprayDirLocal * h;

    /*
        True ray direction from apex through source point.
    */
    vec3 rayDirLocal =
        normalize(sourceLocal - apexLocal);

    vec3 sourceWorld =
        (hostLocalToWorld * vec4(sourceLocal, 1.0)).xyz;

    vec3 rayDirWorld =
        normalize((hostLocalToWorld * vec4(rayDirLocal, 0.0)).xyz);

    /*
        Near point is exactly the source point.
        Far point is source + rayDir * uSprayFarDistance.
    */
    float finalDistance = isFar ? uSprayFarDistance : 0.0;

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
            (uCanvasWorldToLocal * vec4(rayDirWorld, 0.0)).xyz;

        if (abs(rayDirCanvas.z) > 0.00001)
        {
            float tHit = -rayOriginCanvas.z / rayDirCanvas.z;

            if (tHit > 0.0 && tHit < finalDistance)
            {
                vec3 hitCanvas =
                    rayOriginCanvas + rayDirCanvas * tHit;

                vec2 halfCanvas = uCanvasSize * 0.5;

                bool insideCanvas =
                    abs(hitCanvas.x) <= halfCanvas.x &&
                    abs(hitCanvas.y) <= halfCanvas.y;

                if (insideCanvas)
                    finalDistance = tHit;
            }
        }
    }

    vec3 pointWorld =
        sourceWorld + rayDirWorld * finalDistance;

    vRayT =
        uSprayFarDistance > 0.0
            ? finalDistance / uSprayFarDistance
            : 0.0;

    vRayT = clamp(vRayT, 0.0, 1.0);

    /*
        Interpolates from 0 at the source to the real segment length
        at the far endpoint (or clipped endpoint).
    */
    vRayLength = finalDistance;

    /*
        Same seed for both duplicated vertices of the same line.
    */

    vLineSeed = float(gl_VertexID >> 1);

    vWorldPos = pointWorld;
    vLineStartWorld = sourceWorld;
    vLineDirWorld = rayDirWorld;

    gl_Position = uViewProjection * vec4(pointWorld, 1.0);
}