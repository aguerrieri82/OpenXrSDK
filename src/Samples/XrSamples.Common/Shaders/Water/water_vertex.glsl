uniform vec2 uWaterSize;
out float waterSurfaceHeight;
layout(binding = WATERSTATE_SLOT) uniform sampler2DArray uWaterState;

uniform int uWaterLayer;
uniform vec2 uWaterTexelSize;
uniform float uSurfaceHeightScale;

float sampleSimulationHeight(vec2 uv)
{
    vec4 state = textureLod(uWaterState, vec3(clamp(uv, vec2(0.0), vec2(1.0)), float(uWaterLayer)), 0.0);
    return state.r;
}

void applyWaterVertex(inout vec3 position, inout vec3 normal, inout vec4 tangent, vec2 uv)
{
    float height = sampleSimulationHeight(uv) * uSurfaceHeightScale;
    waterSurfaceHeight = height;

    float left = sampleSimulationHeight(uv - vec2(uWaterTexelSize.x, 0.0));
    float right = sampleSimulationHeight(uv + vec2(uWaterTexelSize.x, 0.0));
    float down = sampleSimulationHeight(uv - vec2(0.0, uWaterTexelSize.y));
    float up = sampleSimulationHeight(uv + vec2(0.0, uWaterTexelSize.y));

    float dx = max(2.0 * uWaterTexelSize.x * uWaterSize.x, 0.0001);
    float dy = max(2.0 * uWaterTexelSize.y * uWaterSize.y, 0.0001);
    float slopeX = (right - left) * uSurfaceHeightScale / dx;
    float slopeY = (up - down) * uSurfaceHeightScale / dy;

    position.xy = (position.xy - vec2(0.5)) * uWaterSize;
    position.z += height;

    normal = normalize(vec3(-slopeX, -slopeY, 1.0));
    tangent.xyz = normalize(vec3(1.0, 0.0, slopeX));
}
