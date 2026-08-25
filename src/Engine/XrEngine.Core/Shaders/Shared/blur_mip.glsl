
#define BLUR_MIP_MAX_LEVELS 6
#define BLUR_MIP_MAX_LAYOUTS 8

struct BlurMipLayout
{
	vec4 levels[BLUR_MIP_MAX_LEVELS];
	vec4 info;
};

layout(std140, binding=9) uniform BlurMipBuffer
{
	BlurMipLayout layouts[BLUR_MIP_MAX_LAYOUTS];
} uBlurMip;

int blurMipLevel(int layoutIndex, float roughness)
{
	return int(round(clamp(roughness, 0.0, 1.0) * uBlurMip.layouts[layoutIndex].info.x));
}

vec2 blurMipUv(vec2 uv, int layoutIndex, int level)
{
	vec4 transform = uBlurMip.layouts[layoutIndex].levels[level];
	return uv * transform.xy + transform.zw;
}

#ifdef MULTI_VIEW

vec4 sampleBlurMip(sampler2DArray textureSampler, vec2 uv, int layoutIndex, int level)
{
	return texture(textureSampler, vec3(blurMipUv(uv, layoutIndex, level), float(gl_ViewID_OVR)));
}

vec4 sampleBlurMip(sampler2DArray textureSampler, vec2 uv, int layoutIndex, float roughness)
{
	return sampleBlurMip(textureSampler, uv, layoutIndex, blurMipLevel(layoutIndex, roughness));
}

#else

vec4 sampleBlurMip(sampler2D textureSampler, vec2 uv, int layoutIndex, int level)
{
	return texture(textureSampler, blurMipUv(uv, layoutIndex, level));
}

vec4 sampleBlurMip(sampler2D textureSampler, vec2 uv, int layoutIndex, float roughness)
{
	return sampleBlurMip(textureSampler, uv, layoutIndex, blurMipLevel(layoutIndex, roughness));
}

#endif
