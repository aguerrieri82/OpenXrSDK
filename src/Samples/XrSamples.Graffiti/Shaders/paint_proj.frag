#include "./uniforms.glsl"

in vec2 vBrushCoord01;
in float vRayLength;

out float FragColor;

void main()
{
    float radialSq01 = dot(vBrushCoord01, vBrushCoord01);

    if (radialSq01 > 1.0)
        discard;

    float radialDensity =
        pow(clamp(1.0 - radialSq01, 0.0, 1.0), uRadialFalloff);

    float coneDensityK = sin(uSpreadAngle) / uSprayRadius;
    float ratio = 1.0 + vRayLength * coneDensityK;
    float distanceDensity = 1.0 / (ratio * ratio);

    FragColor =
        radialDensity * distanceDensity * uDensityScale;
}