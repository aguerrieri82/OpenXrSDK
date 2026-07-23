const float pureWhite  = 1.0;

#ifdef HIGH_QUALITY_SRGB

vec3 sRGBToLinear(vec3 c)
{
    bvec3 cutoff = lessThanEqual(c, vec3(0.04045));

    vec3 low = c / 12.92;
    vec3 high = pow((c + 0.055) / 1.055, vec3(2.4));

    return mix(high, low, cutoff);
}

vec3 linearTosRGB(vec3 c)
{
    c = max(c, vec3(0.0));

    bvec3 cutoff = lessThanEqual(c, vec3(0.0031308));

    vec3 low = c * 12.92;
    vec3 high = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;

    return mix(high, low, cutoff);
}

#else

const float gamma      = 2.2;
const float inv_gamma  = 1.0 / gamma;

vec3 sRGBToLinear(vec3 srgbIn)
{
    return vec3(pow(srgbIn.xyz, vec3(gamma)));
}

vec3 linearTosRGB(vec3 color)
{
    return pow(color, vec3(inv_gamma));
}

#endif


vec3 toneMap(vec3 color)
{
	float luminance = dot(color, vec3(0.2126, 0.7152, 0.0722));
	float mappedLuminance = (luminance * (1.0 + luminance/(pureWhite*pureWhite))) / (1.0 + luminance);

	// Scale color by ratio of average luminances.
	return (mappedLuminance / luminance) * color;
}

vec3 toneMapNeutral( vec3 color )
{
    const float startCompression = 0.8 - 0.04;
    const float desaturation = 0.15;

    float x = min(color.r, min(color.g, color.b));
    float offset = x < 0.08 ? x - 6.25 * x * x : 0.04;
    color -= offset;

    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;

    const float d = 1. - startCompression;
    float newPeak = 1. - d * d / (peak + d - startCompression);
    color *= newPeak / peak;

    float g = 1. - 1. / (desaturation * (peak - newPeak) + 1.);
    return mix(color, newPeak * vec3(1, 1, 1), g);
}

vec3 toneMapACES(vec3 x)
{
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;

    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}


void toneMapColor(inout vec4 color)
{
	#if defined(COLOR_IS_SRGB) && !defined(SRGB_ENCODE)
		color.rgb = sRGBToLinear(color.rgb);
	#endif

	#if !defined(COLOR_IS_SRGB) && defined(SRGB_ENCODE)
		color.rgb = linearTosRGB(color.rgb);
	#endif
}

void toneMapTex(inout vec4 color)
{
	#if !defined(TEXTURE_IS_SRGB) && defined(SRGB_ENCODE)
		color.rgb = linearTosRGB(color.rgb);
	#endif
}