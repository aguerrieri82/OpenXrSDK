const float EPS = 1e-6;


// One UBO shared by all paint compute shaders.
// Some fields are unused by accumulate, some unused by resolve.
layout(std140, binding = 0) uniform PaintParams
{
    // Texture/canvas size.
    ivec2 CanvasSize;

    // Simulation timing.
    float DeltaTime;
    float DryRate;

    // Incoming paint color.
    // Incoming density comes from uIncomingDensity.r.
    // PaintColor.a is an optional density multiplier, normally 1.0.
    vec4 PaintColor;

    // Resolve: alpha/coverage conversion.
    // coverage = clamp(totalDensity * DensityToCoverage, 0, 1)
    float DensityToCoverage;

    // Resolve: normal map height conversion.
    // height = totalDensity * DensityToHeight
    float DensityToHeight;

    // Resolve: scales the normal slope.
    float NormalScale;

    // Resolve: material response.
    float DryRoughness;

    float WetRoughness;
    float Metallic;

    
    // Drip simulation.
    vec2 GravityCanvas;
    float GravityStrength;
    float WetDripThreshold;
    float WetDripRate;
};

vec3 UnitColor(vec4 paint)
{
    return paint.a > EPS ? paint.rgb / paint.a : vec3(0.0);
}

float TotalDensity(vec4 dry, vec4 wet)
{
    return dry.a + wet.a;
}

float CoverageFromDensity(float density)
{
    return clamp(density * DensityToCoverage, 0.0, 1.0);
}