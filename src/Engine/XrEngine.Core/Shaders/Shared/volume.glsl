#include "consts.glsl"
#include "blur_mip.glsl"

struct VolumeData
{
    float ior;
    float thickness;
    float attenuationDistance;
    vec3 attenuationColor;
    mat3 backgroundUvTransform[2];
};

layout(std140, binding = 5) uniform VolumeUniforms
{
    VolumeData uVolume;
};

#ifdef MULTIVIEW
    layout(binding=VOLUMEFOREGROUND_SLOT) uniform sampler2DArray volumeForeground;

    #ifdef VOLUME_BACKGROUND
        layout(binding=VOLUMEBACKGROUND_SLOT) uniform sampler2DArray volumeBackground;
    #endif
#else

    layout(binding=VOLUMEFOREGROUND_SLOT) uniform sampler2D volumeForeground;

    #ifdef VOLUME_BACKGROUND
        layout(binding=VOLUMEBACKGROUND_SLOT) uniform sampler2D volumeBackground;
    #endif

#endif

#ifdef USE_THICKNESS_MAP
    layout(binding=THICKNESS_SLOT) uniform sampler2D thicknessTexture;
#endif

float applyVolumeIorToRoughness(float roughness)
{
    return roughness * (uVolume.ior * 2.0 - 2.0);
}


#ifdef VOLUME_BACKGROUND

float volumeBackgroundLod(float roughness)
{
    return float(textureQueryLevels(volumeBackground) - 1) * applyVolumeIorToRoughness(roughness);
}

vec2 transformVolumeBackgroundUv(vec2 uv)
{
#ifdef MULTIVIEW
    return (uVolume.backgroundUvTransform[ACTIVE_EYE] * vec3(uv, 1.0)).xy;
#else
    return (uVolume.backgroundUvTransform[0] * vec3(uv, 1.0)).xy;
#endif
}

vec4 sampleVolumeBackground(vec2 uv, float lod)
{
    vec2 texUv = transformVolumeBackgroundUv(uv);

#ifdef MULTIVIEW
    return textureLod(volumeBackground, vec3(texUv, float(ACTIVE_EYE)), lod);
#else
    return textureLod(volumeBackground, texUv, lod);
#endif
}

#endif

vec4 sampleVolumeSource(vec2 uv, float roughness)
{
    vec4 color = sampleBlurMip(volumeForeground, uv, 0, roughness);

#ifdef VOLUME_BACKGROUND
    if (color.a < 1.0)
    {
        vec4 background = sampleVolumeBackground(uv, volumeBackgroundLod(roughness));

        color.rgb = mix(background.rgb, color.rgb, color.a);
        color.a = color.a + background.a * (1.0 - color.a);
    }
#endif

    return color;
}

vec3 computeVolumeRay(vec3 normal, vec3 viewDir, vec2 uv)
{
    float thickness = uVolume.thickness;

#ifdef USE_THICKNESS_MAP
    thickness *= texture(thicknessTexture, uv).g;
#endif

    return refract(-viewDir, normal, 1.0 / uVolume.ior) * thickness;
}

vec2 computeVolumeUv(vec3 position, vec3 ray, mat4 viewProj)
{
    vec4 clip = viewProj * vec4(position + ray, 1.0);
    return clip.xy / clip.w * 0.5 + 0.5;
}

vec3 applyVolumeAttenuation(vec3 color, float dist)
{
    return color * pow(uVolume.attenuationColor, vec3(dist / uVolume.attenuationDistance));
}

vec4 sampleVolume(vec3 position, vec3 normal, vec3 viewDir, mat4 viewProj, vec2 uv, float roughness)
{
    vec3 ray = computeVolumeRay(normal, viewDir, uv);
    vec2 volumeUv = computeVolumeUv(position, ray, viewProj);
    vec4 color = sampleVolumeSource(volumeUv, roughness);

    color.rgb = applyVolumeAttenuation(color.rgb, length(ray));

    return color;
}