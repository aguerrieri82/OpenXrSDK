layout(binding = WATERSTATE_SLOT) uniform sampler2DArray uWaterState;

uniform int uWaterLayer;
uniform vec2 uWaterSize;
uniform vec2 uWaterTexelSize;
uniform float uWaterHeightScale;
uniform float uAmbientWaveHeight;
uniform float uAmbientWaveScale;
uniform float uWaterTime;

float sampleSimulationHeight(vec2 uv)
{
    vec4 state = textureLod(uWaterState, vec3(clamp(uv, vec2(0.0), vec2(1.0)), float(uWaterLayer)), 0.0);
    return state.r;
}

float sampleAmbientWaveHeight(vec2 uv)
{
    vec2 position = (uv - vec2(0.5)) * uWaterSize;
    float wave0 = sin(dot(position, normalize(vec2(1.0, 0.31))) * 13.1 * uAmbientWaveScale + uWaterTime * 1.15);
    float wave1 = sin(dot(position, normalize(vec2(-0.37, 1.0))) * 20.3 * uAmbientWaveScale + uWaterTime * 1.53 + 1.7);
    float wave2 = sin(dot(position, normalize(vec2(0.71, -1.0))) * 33.1 * uAmbientWaveScale + uWaterTime * 2.05 + 4.1);
    return (wave0 * 0.52 + wave1 * 0.31 + wave2 * 0.17) * uAmbientWaveHeight;
}

float sampleWaterHeight(vec2 uv)
{
    return sampleSimulationHeight(uv) * uWaterHeightScale + sampleAmbientWaveHeight(uv);
}

void applyWaterVertex(inout vec3 position, inout vec3 normal, inout vec4 tangent, vec2 uv)
{
    float height = sampleWaterHeight(uv);

    float left = sampleWaterHeight(uv - vec2(uWaterTexelSize.x, 0.0));
    float right = sampleWaterHeight(uv + vec2(uWaterTexelSize.x, 0.0));
    float down = sampleWaterHeight(uv - vec2(0.0, uWaterTexelSize.y));
    float up = sampleWaterHeight(uv + vec2(0.0, uWaterTexelSize.y));

    float dx = max(2.0 * uWaterTexelSize.x * uWaterSize.x, 0.0001);
    float dy = max(2.0 * uWaterTexelSize.y * uWaterSize.y, 0.0001);
    float slopeX = (right - left) / dx;
    float slopeY = (up - down) / dy;

    position.xy = (position.xy - vec2(0.5)) * uWaterSize;
    position.z += height;

    normal = normalize(vec3(-slopeX, -slopeY, 1.0));
    tangent.xyz = normalize(vec3(1.0, 0.0, slopeX));
}
