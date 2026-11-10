#include "./uniforms.glsl"

in vec2 vBrushCoord01;
in float vRayLength;

out float FragColor;


void RegisterWrittenFragment()
{
    uvec2 p = uvec2(gl_FragCoord.xy);

    //atomicOr(HasSprayFragments, 1u);

    atomicMin(SprayMinX, p.x);
    atomicMin(SprayMinY, p.y);

    atomicMax(SprayMaxX, p.x);
    atomicMax(SprayMaxY, p.y);
}




void main()
{
    float maskSin = sin(-uMaskAngle);
    float maskCos = cos(-uMaskAngle);

    vec2 maskCoord = vec2(
        vBrushCoord01.x * maskCos - vBrushCoord01.y * maskSin,
        vBrushCoord01.x * maskSin + vBrushCoord01.y * maskCos) / min(uMaskSize, vec2(1.0));

    float radialSq01 = dot(maskCoord, maskCoord);

    if (radialSq01 > 1.0)
        discard;

    float radialDensity =
        pow(clamp(1.0 - radialSq01, 0.0, 1.0), uRadialFalloff);

    float coneDensityK = sin(uSpreadAngle) / uSprayRadius;
    float ratio = 1.0 + vRayLength * coneDensityK;
    float distanceDensity = 1.0 / (ratio * ratio);

    float density =
        radialDensity *
        distanceDensity *
        uDensityScale;

    if (density <= 0.0)
        discard;

    if (HasSprayFragments == 1u)
        RegisterWrittenFragment();

    FragColor = density;
}