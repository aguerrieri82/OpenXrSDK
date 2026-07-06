
in vec3 vWorldPos;
in vec3 vFaceNormal;
in vec2 vUv;

flat in int vFace;
flat in int vOutIndex;

#ifdef VOXEL_REMAP
flat in vec3 vTriNormal;
flat in vec4 vTriTangent;
#endif

#ifdef VOXEL_REMAP
struct VoxelResolvedFace
{
    vec4 BaseColor;
    vec3 Normal;
    float Roughness;
    float Metallic;
};

layout(std430, binding = 10) buffer VoxelResolvedFaceBuffer
{
    VoxelResolvedFace uResolvedFaces[];
};
#endif

uniform vec4 uBaseColorFactor;
uniform float uMetallicFactor;
uniform float uRoughnessFactor;
uniform vec3 uCameraPosition;

#ifdef HAS_BASE_COLOR_MAP
layout(binding = 0) uniform sampler2D uBaseColorMap;
#endif

#ifdef HAS_METALLIC_ROUGHNESS_MAP
layout(binding = 2) uniform sampler2D uMetallicRoughnessMap;
#endif

layout(location = 0) out vec4 outColor;


void main()
{
    vec2 uv = vUv;

    vec4 baseColor = uBaseColorFactor;
    float metallic = uMetallicFactor;
    float roughness = uRoughnessFactor;

#ifdef HAS_BASE_COLOR_MAP
    baseColor *= texture(uBaseColorMap, uv);
#endif

#ifdef HAS_METALLIC_ROUGHNESS_MAP
    vec4 mr = texture(uMetallicRoughnessMap, uv);

    // glTF convention:
    // G = roughness
    // B = metallic
    roughness *= mr.g;
    metallic *= mr.b;
#endif

#ifdef VOXEL_REMAP

    VoxelResolvedFace resolved;
    resolved.BaseColor = baseColor;
    resolved.Normal = vFaceNormal;
    resolved.Roughness = roughness;
    resolved.Metallic = metallic;

    uResolvedFaces[vOutIndex] = resolved;
#endif

#ifdef VOXEL_PREVIEW
    vec3 materialColor = baseColor.rgb;

#ifdef PREVIEW_MATERIAL
    vec3 color = materialColor;
#else
    vec3 faceColor = normalize(vFaceNormal) * 0.5 + 0.5;
    vec3 color = mix(faceColor, materialColor, 0.35);
#endif

    vec3 viewDir = normalize(uCameraPosition - vWorldPos);
    float light = dot(normalize(vFaceNormal), viewDir) > 0.0 ? 1.0 : 0.35;

    outColor = vec4(color * light, baseColor.a);
#else
    outColor = baseColor;
#endif
}