layout(binding = WATERSTATE_SLOT) uniform sampler2DArray uWaterState;

uniform int uWaterLayer;
uniform vec2 uWaterSize;
uniform vec2 uWaterTexelSize;
uniform float uWaterHeightScale;

float sampleWaterHeight(vec2 uv)
{
    return textureLod(
        uWaterState,
        vec3(clamp(uv, vec2(0.0), vec2(1.0)), float(uWaterLayer)),
        0.0).r;
}

void applyWaterVertex(inout vec3 position, inout vec3 normal, vec2 uv)
{
    float height = sampleWaterHeight(uv) * uWaterHeightScale;

    float left = sampleWaterHeight(uv - vec2(uWaterTexelSize.x, 0.0));
    float right = sampleWaterHeight(uv + vec2(uWaterTexelSize.x, 0.0));
    float down = sampleWaterHeight(uv - vec2(0.0, uWaterTexelSize.y));
    float up = sampleWaterHeight(uv + vec2(0.0, uWaterTexelSize.y));

    float dx = max(2.0 * uWaterTexelSize.x * uWaterSize.x, 0.0001);
    float dy = max(2.0 * uWaterTexelSize.y * uWaterSize.y, 0.0001);

    position.xy = (position.xy - vec2(0.5)) * uWaterSize;
    position.z += height;

    normal = normalize(vec3(
        -(right - left) * uWaterHeightScale / dx,
        -(up - down) * uWaterHeightScale / dy,
        1.0));
}
