const float EPS = 1e-6;

// Shared by paint compute shaders.
// Fields may be unused by a specific pass, but are used by the paint pipeline.
layout(std140, binding = 11) uniform PaintParams
{
    // Full paint texture/canvas size in texels.
    ivec2 CanvasSize;

    // Compute dispatch rectangle.
    // Shader pixel:
    //   local = gl_GlobalInvocationID.xy
    //   p     = ComputeOffset + local
    //
    // ComputeSize is the valid local size; dispatch groups may be rounded up.
    ivec2 ComputeOffset;
    ivec2 ComputeSize;

    // Simulation timing.
    float DeltaTime;
    float DryRate;

    // Resolve opacity:
    // coverage = 1.0 - exp(-dryDensity * PaintOpacityScale)
    float PaintOpacityScale;

    // Resolve normal slope scale.
    float NormalScale;

    // Incoming paint color.
    // rgb = paint color
    // a   = incoming density multiplier
    vec4 PaintColor;

    // Resolve material response.
    float DryRoughness;
    float WetRoughness;
    float Metallic;

    // Drip simulation.
    float WetDripRate;
    vec2 GravityCanvas;
    float GravityStrength;
};


float DensityToCoverage(float density)
{
    return 1.0 - exp(-density * PaintOpacityScale);
}