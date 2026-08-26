#include "../Shared/blur_mip.glsl"

#ifdef VOLUME_BACKGROUND
uniform mat3 uBackgroundUvTransform[2];
#endif

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


#ifdef VOLUME_BACKGROUND

vec4 sampleVolumeBackground(vec2 uv, float roughness)
{
	float lod = float(textureQueryLevels(volumeBackground) - 1) * roughness * (uMaterial.ior * 2.0 - 2.0);

#ifdef MULTIVIEW
	vec2 texUv = (uBackgroundUvTransform[ACTIVE_EYE] * vec3(uv, 1.0)).xy;
	return textureLod(volumeBackground, vec3(texUv, float(ACTIVE_EYE)), lod);
#else
	vec2 texUv = (uBackgroundUvTransform[0] * vec3(uv, 1.0)).xy;
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
		vec4 background = sampleVolumeBackground(uv, roughness);

		color.rgb = mix(background.rgb, color.rgb, color.a);
		color.a = color.a + background.a * (1.0 - color.a);
	}
#endif

	return color;
}

vec2 computeVolumeUv(vec3 position, vec3 ray)
{
	vec4 clip = getViewProj() * vec4(position + ray, 1.0);
	return clip.xy / clip.w * 0.5 + 0.5;
}

vec4 sampleVolume(vec3 position, float roughness)
{
	return sampleVolumeSource(computeVolumeUv(position, vec3(0.0)), roughness);
}

vec4 sampleVolume(vec3 position, vec3 normal, vec3 viewDir, float thickness, float roughness)
{
	vec3 ray = refract(-viewDir, normal, 1.0 / uMaterial.ior) * thickness;

	vec4 color = sampleVolumeSource(computeVolumeUv(position, ray), roughness);

	color.rgb *= pow(uMaterial.attenuationColor, vec3(length(ray) / uMaterial.attenuationDistance));

	return color;
}