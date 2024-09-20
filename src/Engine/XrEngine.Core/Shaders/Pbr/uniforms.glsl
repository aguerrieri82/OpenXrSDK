
layout(std140) uniform Material {

    vec4 BaseColorFactor;
    int BaseColorUVSet;

    float MetallicFactor;
    float RoughnessFactor;
    int MetallicRoughnessUVSet;

    float NormalScale;
    int NormalUVSet;

    float OcclusionStrength;
    int OcclusionUVSet;

    float AlphaCutoff;

    mat3 NormalUVTransform;
    mat3 EmissiveUVTransform;
    mat3 OcclusionUVTransform;
    mat3 BaseColorUVTransform;
    mat3 MetallicRoughnessUVTransform;

    // Sheen Material

    vec3 SheenColorFactor;
    int SheenColorUVSet;
    mat3 SheenColorUVTransform;

    float SheenRoughnessFactor;
    int SheenRoughnessUVSet;
    mat3 SheenRoughnessUVTransform;


    //Specular Glossiness

    vec4 DiffuseFactor;
    int DiffuseUVSet;

    vec3 SpecularFactor;
    float GlossinessFactor;
    int SpecularGlossinessUVSet;

    mat3 DiffuseUVTransform;
    mat3 SpecularGlossinessUVTransform;
    
    //Shadow
    vec4 ShadowColor;

    // Specular Dieletrics
    vec3 KHR_materials_specular_specularColorFactor;
    float KHR_materials_specular_specularFactor;

    // Emissive

    vec3 EmissiveFactor;
    float EmissiveStrength;
    int EmissiveUVSet;

    // Clearcoat Material

    float ClearcoatFactor;
    int ClearcoatUVSet;
    mat3 ClearcoatUVTransform;

    float ClearcoatRoughnessFactor;
    int ClearcoatRoughnessUVSet;
    mat3 ClearcoatRoughnessUVTransform;

    float ClearcoatNormalScale;
    int ClearcoatNormalUVSet;
    mat3 ClearcoatNormalUVTransform;

    // Specular Material
    int SpecularUVSet;
    mat3 SpecularUVTransform;

    int SpecularColorUVSet;
    mat3 SpecularColorUVTransform;

    // Transmission Material
    float TransmissionFactor;
    int TransmissionUVSet;
    mat3 TransmissionUVTransform;
    ivec2 TransmissionFramebufferSize;

    // Volume Material
    vec3 AttenuationColor;
    float AttenuationDistance;

    float ThicknessFactor;
    int ThicknessUVSet;
    mat3 ThicknessUVTransform;

    // Iridescence
    float IridescenceIor;

    float IridescenceFactor;
    int IridescenceUVSet;
    mat3 IridescenceUVTransform;

    float IridescenceThicknessMinimum;
    float IridescenceThicknessMaximum;
    int IridescenceThicknessUVSet;
    mat3 IridescenceThicknessUVTransform;

    // Anisotropy
    vec3 Anisotropy;
    int AnisotropyUVSet;
    mat3 AnisotropyUVTransform;

    //Transmission
    int DiffuseTransmissionUVSet;
    mat3 DiffuseTransmissionUVTransform;

    int DiffuseTransmissionColorUVSet;
    mat3 DiffuseTransmissionColorUVTransform;


} uMaterial;


layout(std140) uniform Camera {
    mat4 ViewMatrix;
    mat4 ProjectionMatrix;
    mat4 ViewProjectionMatrix;
    vec3 Position;
    float Exposure;
    float FarPlane;
} uCamera;


uniform mat4 uModelMatrix;

uniform sampler2D uNormalSampler;
uniform sampler2D uEmissiveSampler;
uniform sampler2D uOcclusionSampler;
uniform sampler2D uBaseColorSampler;
uniform sampler2D uMetallicRoughnessSampler;
uniform sampler2D uDiffuseSampler;
uniform sampler2D uSpecularGlossinessSampler;
uniform sampler2D uClearcoatSampler;
uniform sampler2D uClearcoatRoughnessSampler;
uniform sampler2D uClearcoatNormalSampler;
uniform sampler2D uSheenColorSampler;
uniform sampler2D uSheenRoughnessSampler;
uniform sampler2D uSpecularSampler;
uniform sampler2D uSpecularColorSampler;
uniform sampler2D uTransmissionSampler;
uniform sampler2D uTransmissionFramebufferSampler;
uniform sampler2D uThicknessSampler;
uniform sampler2D uIridescenceSampler;
uniform sampler2D uIridescenceThicknessSampler;
uniform sampler2D uAnisotropySampler;
uniform sampler2D uDiffuseTransmissionSampler;
uniform sampler2D uDiffuseTransmissionColorSampler;

