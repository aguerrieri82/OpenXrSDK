#include "../Shared/blur_mip.glsl"

#ifdef VOLUME_BACKGROUND
	uniform mat3 uBackgroundUvTransform[2];

	#ifdef VOLUME_BACKGROUND_EXTERNAL
		#define VOLUME_BACKGROUND_SAMPLER samplerExternalOES
	#else
		#define VOLUME_BACKGROUND_SAMPLER sampler2D
	#endif

	layout(binding=VOLUMEBACKGROUND_SLOT) uniform VOLUME_BACKGROUND_SAMPLER volumeBackground;

	#ifdef VOLUME_BACKGROUND_STEREO
		layout(binding=VOLUMEBACKGROUNDRIGHT_SLOT) uniform VOLUME_BACKGROUND_SAMPLER volumeBackgroundRight;
	#endif
#endif

#ifdef VOLUME_FOREGROUND
	#ifdef MULTI_VIEW
		layout(binding=VOLUMEFOREGROUND_SLOT) uniform sampler2DArray volumeForeground;
	#else
		layout(binding=VOLUMEFOREGROUND_SLOT) uniform sampler2D volumeForeground;
	#endif
#endif


#ifdef VOLUME_BACKGROUND

	#ifdef VOLUME_BACKGROUND_EXTERNAL

	vec4 sampleVolumeBackground(vec2 uv, float roughness)
	{
		vec3 tuv = vec3(uv, 1.0) * uBackgroundUvTransform[ACTIVE_EYE];
		vec2 texUv = tuv.xy / tuv.z;

		#ifdef VOLUME_BACKGROUND_STEREO
			if (ACTIVE_EYE == 1u)
				return texture(volumeBackgroundRight, texUv);
		#endif

		return texture(volumeBackground, texUv);
	}

	#else

	vec4 sampleVolumeBackground(vec2 uv, float roughness)
	{
		vec3 tuv = vec3(uv, 1.0) * uBackgroundUvTransform[ACTIVE_EYE];
		vec2 texUv = tuv.xy / tuv.z;

		#ifdef VOLUME_BACKGROUND_STEREO
			if (ACTIVE_EYE == 1u)
			{
				float lod = float(textureQueryLevels(volumeBackgroundRight) - 1) * roughness * (uMaterial.ior * 2.0 - 2.0);
				return textureLod(volumeBackgroundRight, texUv, lod);
			}
		#endif

		float lod = float(textureQueryLevels(volumeBackground) - 1) * roughness * (uMaterial.ior * 2.0 - 2.0);
		return textureLod(volumeBackground, texUv, lod);
	}

	#endif

#endif


vec4 sampleVolumeSource(vec2 uv, float roughness)
{
#ifdef VOLUME_FOREGROUND
	vec4 color = sampleBlurMip(volumeForeground, uv, 0, roughness);
#else
	vec4 color = vec4(0.0);
#endif

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
#ifdef USE_DISPERSION
	float halfSpread = (uMaterial.ior - 1.0) * 0.025 * uMaterial.dispersion;
	vec3 iors = vec3(uMaterial.ior - halfSpread, uMaterial.ior, uMaterial.ior + halfSpread);

	vec3 rayR = refract(-viewDir, normal, 1.0 / iors.r) * thickness;
	vec3 rayG = refract(-viewDir, normal, 1.0 / iors.g) * thickness;
	vec3 rayB = refract(-viewDir, normal, 1.0 / iors.b) * thickness;

	vec4 colorR = sampleVolumeSource(computeVolumeUv(position, rayR), roughness);
	vec4 colorG = sampleVolumeSource(computeVolumeUv(position, rayG), roughness);
	vec4 colorB = sampleVolumeSource(computeVolumeUv(position, rayB), roughness);

	vec4 color = vec4(colorR.r, colorG.g, colorB.b, colorG.a);

	color.rgb *= pow(uMaterial.attenuationColor, vec3(length(rayG) / uMaterial.attenuationDistance));
#else
	vec3 ray = refract(-viewDir, normal, 1.0 / uMaterial.ior) * thickness;

	vec4 color = sampleVolumeSource(computeVolumeUv(position, ray), roughness);

	color.rgb *= pow(uMaterial.attenuationColor, vec3(length(ray) / uMaterial.attenuationDistance));
#endif

	return color;
}