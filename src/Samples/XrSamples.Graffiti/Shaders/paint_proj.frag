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
    float radialSq01 = dot(vBrushCoord01, vBrushCoord01);

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