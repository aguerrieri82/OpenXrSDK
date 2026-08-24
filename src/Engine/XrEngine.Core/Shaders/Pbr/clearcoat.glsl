const float ClearCoatPI = 3.14159265358979323846;
const float ClearCoatEpsilon = 0.00001;
const float ClearCoatF0 = 0.04;

float clearCoatFresnel(float NoV)
{
	float x = 1.0 - NoV;
	float x2 = x * x;

	return ClearCoatF0 + (1.0 - ClearCoatF0) * x * x2 * x2;
}

float clearCoatWeight(float factor, vec3 N, vec3 V)
{
	float NoV = abs(dot(N, V));

	return factor * clearCoatFresnel(NoV);
}

float ndfClearCoatGGX(float NoH, float roughness)
{
	float alpha = max(roughness * roughness, ClearCoatEpsilon);
	float alpha2 = alpha * alpha;
	float f = NoH * NoH * (alpha2 - 1.0) + 1.0;

	return alpha2 / (ClearCoatPI * f * f);
}

float visibilityClearCoatGGX(float NoL, float NoV, float roughness)
{
	float alpha = max(roughness * roughness, ClearCoatEpsilon);
	float alpha2 = alpha * alpha;

	float GGXV = NoL * sqrt(NoV * NoV * (1.0 - alpha2) + alpha2);
	float GGXL = NoV * sqrt(NoL * NoL * (1.0 - alpha2) + alpha2);
	float GGX = GGXV + GGXL;

	return GGX > ClearCoatEpsilon ? 0.5 / GGX : 0.0;
}

float brdfClearCoat(float NoH, float NoL, float NoV, float roughness)
{
	return ndfClearCoatGGX(NoH, roughness) * visibilityClearCoatGGX(NoL, NoV, roughness);
}

vec3 evaluateClearCoatDirect(
	float clearCoatRoughness,
	vec3 N,
	vec3 V,
	vec3 L,
	vec3 radiance)
{
	float NoV = dot(N, V);
	float NoL = dot(N, L);

	if (NoV <= 0.0 || NoL <= 0.0)
		return vec3(0.0);

	vec3 H = normalize(L + V);
	float NoH = dot(N, H);
	float clearCoatBRDF = brdfClearCoat(NoH, NoL, NoV, clearCoatRoughness);

	return vec3(clearCoatBRDF) * radiance * NoL;
}