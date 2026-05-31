
struct PaintLayerParams
{
    float DryRateToNext;
    float Wetness;
    float DripRate;
    float DripThreshold;

    float MixStrength;
    float StainStrength;
};

layout(std140, binding = 3) uniform PaintSimulationBlock
{
    vec2 CanvasSize;
    float DeltaTime;
    int LayerCount;

    vec3 SprayColor;
    float SprayDensityScale;

    vec2 GravityCanvas;
    float GravityStrength;
    float GlobalDryScale;

    float GlobalDripScale;
    float GlobalMixScale;

    float uDryRoughness;    // e.g. 0.75
    float uWetRoughness;    // e.g. 0.18
    float uHeightScale;     // e.g. 2.0
    float uDensityToHeight; // e.g. 0.05


    PaintLayerParams Layers[MAX_PAINT_LAYERS];
};