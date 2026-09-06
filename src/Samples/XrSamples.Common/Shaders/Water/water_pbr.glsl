#include "[XrEngine.Core]Pbr/pbr_defaults.glsl"

layout(binding = WATERSTATE_SLOT) uniform sampler2DArray uWaterState;

uniform int uWaterLayer;
uniform vec2 uWaterSize;
uniform vec2 uWaterTexelSize;
uniform float uWaterDepth;
uniform float uWaterHeightScale;
uniform float uWakeDetailStrength;

vec2 loadWakeDetailGradient(vec2 uv)
{
    float left = textureLod(uWaterState, vec3(uv - vec2(uWaterTexelSize.x, 0.0), float(uWaterLayer)), 0.0).b;
    float right = textureLod(uWaterState, vec3(uv + vec2(uWaterTexelSize.x, 0.0), float(uWaterLayer)), 0.0).b;
    float down = textureLod(uWaterState, vec3(uv - vec2(0.0, uWaterTexelSize.y), float(uWaterLayer)), 0.0).b;
    float up = textureLod(uWaterState, vec3(uv + vec2(0.0, uWaterTexelSize.y), float(uWaterLayer)), 0.0).b;
    float dx = max(2.0 * uWaterTexelSize.x * uWaterSize.x, 0.0001);
    float dy = max(2.0 * uWaterTexelSize.y * uWaterSize.y, 0.0001);

    return vec2((right - left) / dx, (up - down) / dy) * uWaterHeightScale * uWakeDetailStrength;
}

vec3 applyWaterNormalDetail(vec3 normal, vec2 uv)
{
    normal = normalize(normal);

    vec3 tangent = normalize(fTangentBasis[0]);
    tangent = normalize(tangent - normal * dot(tangent, normal));

    vec3 bitangent = normalize(cross(normal, tangent));

    if (dot(bitangent, fTangentBasis[1]) < 0.0)
        bitangent = -bitangent;

    vec2 gradient = loadWakeDetailGradient(uv);

    return normalize(normal - tangent * gradient.x - bitangent * gradient.y);
}

FragmentProperties loadWaterFragmentProperties()
{
    FragmentProperties frag = loadFragmentProperties();
    vec4 state = texture(uWaterState, vec3(fUv, float(uWaterLayer)));

    float foam = smoothstep(0.35, 0.9, max(abs(state.g), abs(state.a)));
    vec3 foamColor = vec3(0.72, 0.88, 0.91);

    frag.normal = applyWaterNormalDetail(normalize(frag.normal), fUv);

    float localDepth = max(uWaterDepth + state.r * uWaterHeightScale, 0.0);
    frag.thickness = localDepth;

    frag.albedo = mix(frag.albedo, foamColor, foam * 0.22);
    frag.baseColor.rgb = frag.albedo;
    frag.baseColor.a = mix(frag.baseColor.a, 0.68, foam);
    frag.roughness = mix(frag.roughness, 0.16, foam);
    frag.metalness = 0.0;

    return frag;
}
