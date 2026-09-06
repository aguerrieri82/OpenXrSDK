#include "[XrEngine.Core]Pbr/pbr_defaults.glsl"

layout(binding = WATERSTATE_SLOT) uniform sampler2DArray uWaterState;

uniform int uWaterLayer;
uniform float uWaterDepth;
uniform float uWaterHeightScale;
uniform float uMicroWaveStrength;
uniform float uWaterTime;

vec2 loadMicroWaveGradient(vec2 uv)
{
    vec2 dir0 = normalize(vec2(1.0, 0.37));
    vec2 dir1 = normalize(vec2(-0.31, 1.0));
    vec2 dir2 = normalize(vec2(0.73, -1.0));

    float phase0 = dot(uv, dir0) * 54.0 + uWaterTime * 1.7;
    float phase1 = dot(uv, dir1) * 79.0 + uWaterTime * 2.2;
    float phase2 = dot(uv, dir2) * 113.0 + uWaterTime * 2.8;

    return (cos(phase0) * dir0 * 0.030 + cos(phase1) * dir1 * 0.018 + cos(phase2) * dir2 * 0.010) * uMicroWaveStrength;
}

vec3 applyMicroWaveNormal(vec3 normal, vec2 uv)
{
    normal = normalize(normal);

    vec3 tangent = normalize(fTangentBasis[0]);
    tangent = normalize(tangent - normal * dot(tangent, normal));

    vec3 bitangent = normalize(cross(normal, tangent));

    if (dot(bitangent, fTangentBasis[1]) < 0.0)
        bitangent = -bitangent;

    vec2 gradient = loadMicroWaveGradient(uv);

    return normalize(normal - tangent * gradient.x - bitangent * gradient.y);
}

FragmentProperties loadWaterFragmentProperties()
{
    FragmentProperties frag = loadFragmentProperties();
    vec4 state = texture(uWaterState, vec3(fUv, float(uWaterLayer)));

    float foam = smoothstep(0.35, 0.9, max(abs(state.g), abs(state.a)));
    vec3 foamColor = vec3(0.72, 0.88, 0.91);

    frag.normal = applyMicroWaveNormal(normalize(frag.normal), fUv);

    float localDepth = max(uWaterDepth + (state.r + state.b) * uWaterHeightScale, 0.0);
    frag.thickness = localDepth;

    frag.albedo = mix(frag.albedo, foamColor, foam * 0.22);
    frag.baseColor.rgb = frag.albedo;
    frag.baseColor.a = mix(frag.baseColor.a, 0.68, foam);
    frag.roughness = mix(frag.roughness, 0.16, foam);
    frag.metalness = 0.0;

    return frag;
}
