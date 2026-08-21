#include "./uniforms.glsl"
#include "./functions.glsl"

layout(location = 0) in vec3 aPosition;

out vec2 vBrushCoord01;
out float vRayLength;

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