const float SheenPI = 3.14159265358979323846;
const float SheenEpsilon = 0.00001;


float sheenL(float x, float alpha)
{
	float oneMinusAlphaSq = (1.0 - alpha) * (1.0 - alpha);
	float a = mix(21.5473, 25.3245, oneMinusAlphaSq);
	float b = mix(3.82987, 3.32435, oneMinusAlphaSq);
	float c = mix(0.19823, 0.16801, oneMinusAlphaSq);
	float d = mix(-1.97760, -1.27393, oneMinusAlphaSq);
	float e = mix(-4.32054, -4.85967, oneMinusAlphaSq);

	return a / (1.0 + b * pow(x, c)) + d * x + e;
}

float lambdaSheen(float cosTheta, float alpha)
{
	return abs(cosTheta) < 0.5 ?
		exp(sheenL(cosTheta, alpha)) :
		exp(2.0 * sheenL(0.5, alpha) - sheenL(1.0 - cosTheta, alpha));
}

float visibilityCharlie(float cosLi, float cosLo, float roughness)
{
	float alpha = max(roughness * roughness, SheenEpsilon);
	cosLi = max(cosLi, SheenEpsilon);
	cosLo = max(cosLo, SheenEpsilon);

	return 1.0 / ((1.0 + lambdaSheen(cosLo, alpha) + lambdaSheen(cosLi, alpha)) * (4.0 * cosLo * cosLi));
}

#ifndef CHARLIE_LUT

layout(binding=IBLCHARLIELUT_SLOT) uniform sampler2D charlieLUT;

#ifdef USE_IBL
	layout(binding=IBLCHARLIEENV_SLOT) uniform samplerCube charlieEnv;
#endif

float ndfCharlie(float cosLh, float roughness)
{
	float alpha = max(roughness * roughness, SheenEpsilon);
	float invAlpha = 1.0 / alpha;
	float cos2h = cosLh * cosLh;
	float sin2h = max(0.0, 1.0 - cos2h);

	return (2.0 + invAlpha) * pow(sin2h, invAlpha * 0.5) / (2.0 * SheenPI);
}


float brdfSheen(float cosLh, float cosLi, float cosLo, float roughness)
{
	return ndfCharlie(cosLh, roughness) * visibilityCharlie(cosLi, cosLo, roughness);
}

float sheenAlbedoScaling(vec3 sheenColor, float e)
{
	return 1.0 - max(max(sheenColor.r, sheenColor.g), sheenColor.b) * e;
}

float sheenAlbedoScaling(vec3 sheenColor, float eV, float eL)
{
	float maxSheen = max(max(sheenColor.r, sheenColor.g), sheenColor.b);

	return min(1.0 - maxSheen * eV, 1.0 - maxSheen * eL);
}

vec3 evaluateSheenIBL(
	vec3 sheenColor,
	float sheenRoughness,
	float NoV,
	vec3 reflectionDir,
	float mipLevels,
	float intensity,
	out float scaling)
{
	float sheenE = texture(charlieLUT, vec2(NoV, sheenRoughness)).r;
	scaling = sheenAlbedoScaling(sheenColor, sheenE);

	vec3 irradiance = textureLod(
		charlieEnv,
		reflectionDir,
		sheenRoughness * mipLevels).rgb * intensity;

	return sheenColor * sheenE * irradiance;
}

vec3 evaluateSheenDirect(
	vec3 sheenColor,
	float sheenRoughness,
	float NoV,
	float NoL,
	float NoH,
	vec3 radiance,
	out float sheenSpecularStrength,
	out float sheenScaling)
{
	float sheenEV = texture(charlieLUT, vec2(NoV, sheenRoughness)).r;
	float sheenEL = texture(charlieLUT, vec2(NoL, sheenRoughness)).r;
	float sheenBRDF = brdfSheen(NoH, NoL, NoV, sheenRoughness);

	sheenScaling = sheenAlbedoScaling(sheenColor, sheenEV, sheenEL);

	vec3 sheen = sheenColor * sheenBRDF;
	vec3 sheenLighting = sheen * radiance * NoL;

	sheenSpecularStrength = max(sheenLighting.r, max(sheenLighting.g, sheenLighting.b));

	return sheen;
}

#endif