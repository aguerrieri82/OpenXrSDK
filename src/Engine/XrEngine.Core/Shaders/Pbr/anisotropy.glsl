vec2 getAnisotropyDirection(vec2 direction, float rotation)
{
	float c = cos(rotation);
	float s = sin(rotation);
	return mat2(c, s, -s, c) * normalize(direction);
}

float distributionGGXAnisotropic(float NoH, float ToH, float BoH, float at, float ab)
{
	float a2 = at * ab;
	vec3 f = vec3(ab * ToH, at * BoH, a2 * NoH);
	float w2 = a2 / dot(f, f);
	return a2 * w2 * w2 * 0.31830988618;
}

float visibilityGGXAnisotropic(float NoL, float NoV, float BoV, float ToV, float ToL, float BoL, float at, float ab)
{
	float GGXV = NoL * length(vec3(at * ToV, ab * BoV, NoV));
	float GGXL = NoV * length(vec3(at * ToL, ab * BoL, NoL));
	return clamp(0.5 / (GGXV + GGXL), 0.0, 1.0);
}

vec3 getAnisotropicReflection(vec3 normal, vec3 anisotropicB, vec3 viewDir, float roughness, float anisotropy)
{
	vec3 bentNormal = cross(anisotropicB, viewDir);
	bentNormal = normalize(cross(bentNormal, anisotropicB));

	float x = 1.0 - anisotropy * (1.0 - roughness);
	float x2 = x * x;
	float a = x2 * x2;

	bentNormal = normalize(mix(bentNormal, normal, a));

	vec3 reflectDir = reflect(-viewDir, bentNormal);
	return normalize(mix(reflectDir, bentNormal, roughness * roughness));
}