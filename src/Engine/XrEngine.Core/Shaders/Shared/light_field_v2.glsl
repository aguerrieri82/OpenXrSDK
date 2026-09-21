layout(binding = LIGHTFIELDBASE_SLOT) uniform highp usampler3D uLightFieldLookup;

struct LightFieldContribution
{
	vec4 Direction;
	vec4 Color;
};

layout(std430, binding = 21) readonly buffer LightFieldBuffer
{
	LightFieldContribution uLightFieldContributions[];
};

uniform vec3 uLightFieldOrigin;
uniform float uVoxelSize;
uniform ivec3 uLightFieldSize;
uniform float uLightFieldDifStrength;
uniform float uLightFieldSpecStrength;
uniform float uLightFieldOfs;

const float LightEpsilon = 0.00001;


bool getLightFieldInterpolation(vec3 position, vec3 normal, out ivec3 baseCell, out vec3 factor)
{
	vec3 samplePosition = position + normal * (uVoxelSize * uLightFieldOfs);
	vec3 gridPos = (samplePosition - uLightFieldOrigin) / uVoxelSize;

	if (any(lessThan(gridPos, vec3(0.0))) || any(greaterThan(gridPos, vec3(uLightFieldSize))))
		return false;

	vec3 voxelPos = gridPos - vec3(0.5);

	baseCell = ivec3(floor(voxelPos));
	factor = fract(voxelPos);

	return true;
}


uvec2 getLightFieldRange(ivec3 cell)
{
	cell = clamp(cell, ivec3(0), uLightFieldSize - ivec3(1));
	return texelFetch(uLightFieldLookup, cell, 0).rg;
}


float getLightFieldWeight(ivec3 offset, vec3 factor)
{
	vec3 weight = mix(vec3(1.0) - factor, factor, vec3(offset));
	return weight.x * weight.y * weight.z;
}


vec3 desaturatePreserveEnergy(vec3 c, float amount)
{
	float luma = dot(c, vec3(0.2126, 0.7152, 0.0722));
	float peak = max(max(c.r, c.g), c.b);
	float gray = mix(luma, peak, 0.65);

	return mix(c, vec3(gray), amount);
}


vec3 evaluateLightFieldContribution(LightFieldContribution contribution, vec3 albedo, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	vec3 emittedDir = contribution.Direction.xyz;

	if (dot(emittedDir, emittedDir) <= LightEpsilon)
		return vec3(0.0);

	vec3 L = normalize(-emittedDir);

	float NoL = saturate(dot(normal, L));
	float NoV = saturate(dot(normal, viewDir));

	if (NoL <= 0.0 || NoV <= 0.0)
		return vec3(0.0);

	vec3 radiance = contribution.Color.rgb;

	if (dot(radiance, radiance) <= LightEpsilon)
		return vec3(0.0);

	vec3 diffuseRadiance = radiance * uLightFieldDifStrength;
	vec3 specularRadiance = radiance * uLightFieldSpecStrength;

#ifdef SIMPLIFIED
	vec3 diffuse = albedo * diffuseRadiance * NoL;
	vec3 specular = specularRadiance * metalness * NoL;

	return diffuse * (1.0 - metalness) + specular;
#else
	vec3 H = normalize(L + viewDir);

	float NoH = saturate(dot(normal, H));
	float VoH = saturate(dot(viewDir, H));

	vec3 F0 = mix(Fdielectric, albedo, metalness);
	vec3 F = fresnelSchlick(F0, VoH);

	float D = distributionGGX(NoH, roughness);
	float G = geometrySmith(NoL, NoV, roughness);

	vec3 kd = (vec3(1.0) - F) * (1.0 - metalness);

	#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
		vec3 diffuseBRDF = kd * albedo * (1.0 / PI);
	#else
		vec3 diffuseBRDF = kd * albedo;
	#endif

	vec3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * NoL * NoV);

	vec3 diffuse = diffuseBRDF * diffuseRadiance;
	vec3 specular = specularBRDF * specularRadiance;

	return (diffuse + specular) * NoL;
#endif
}


vec3 evaluateLightFieldVoxel(ivec3 cell, vec3 albedo, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	uvec2 range = getLightFieldRange(cell);
	vec3 result = vec3(0.0);
	uint end = range.x + range.y;

	for (uint i = range.x; i < end; ++i)
		result += evaluateLightFieldContribution(uLightFieldContributions[i], albedo, metalness, roughness, normal, viewDir);

	return result;
}


vec3 evaluateLightField(vec3 position, vec3 albedo, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	ivec3 baseCell;
	vec3 factor;

	if (!getLightFieldInterpolation(position, normal, baseCell, factor))
		return vec3(0.0);

	vec3 result = vec3(0.0);

	for (int z = 0; z < 2; ++z)
	{
		for (int y = 0; y < 2; ++y)
		{
			for (int x = 0; x < 2; ++x)
			{
				ivec3 offset = ivec3(x, y, z);
				float weight = getLightFieldWeight(offset, factor);

				if (weight <= 0.0)
					continue;

				result += evaluateLightFieldVoxel(baseCell + offset, albedo, metalness, roughness, normal, viewDir) * weight;
			}
		}
	}

	return result;
}


vec3 evaluateLightFieldDirectionVoxel(ivec3 cell)
{
	uvec2 range = getLightFieldRange(cell);
	vec3 emittedMoment = vec3(0.0);
	uint end = range.x + range.y;

	for (uint i = range.x; i < end; ++i)
	{
		LightFieldContribution contribution = uLightFieldContributions[i];
		vec3 radiance = contribution.Color.rgb;
		float weight = radiance.r + radiance.g + radiance.b;

		emittedMoment += contribution.Direction.xyz * weight;
	}

	return emittedMoment;
}


vec3 evaluateLightFieldDirection(vec3 position, vec3 normal)
{
	ivec3 baseCell;
	vec3 factor;

	if (!getLightFieldInterpolation(position, normal, baseCell, factor))
		return vec3(0.0);

	vec3 emittedMoment = vec3(0.0);

	for (int z = 0; z < 2; ++z)
	{
		for (int y = 0; y < 2; ++y)
		{
			for (int x = 0; x < 2; ++x)
			{
				ivec3 offset = ivec3(x, y, z);
				float weight = getLightFieldWeight(offset, factor);

				emittedMoment += evaluateLightFieldDirectionVoxel(baseCell + offset) * weight;
			}
		}
	}

	if (dot(emittedMoment, emittedMoment) <= LightEpsilon)
		return vec3(0.0);

	return (normalize(-emittedMoment) + 1.0) * 0.5;
}


vec3 evaluateLightFieldRadianceVoxel(ivec3 cell, vec3 normal)
{
	uvec2 range = getLightFieldRange(cell);
	vec3 radiance = vec3(0.0);
	uint end = range.x + range.y;

	for (uint i = range.x; i < end; ++i)
	{
		LightFieldContribution contribution = uLightFieldContributions[i];

#ifdef LIGHT_FIELD_SELF_OMNI
		radiance += contribution.Color.rgb;
#else
		vec3 emittedDir = contribution.Direction.xyz;

		if (dot(emittedDir, emittedDir) <= LightEpsilon)
			continue;

		vec3 L = normalize(-emittedDir);
		float NoL = saturate(dot(normal, L));

		radiance += contribution.Color.rgb * NoL;
#endif
	}

	return radiance;
}


vec3 evaluateLightFieldRadiance(vec3 position, vec3 normal)
{
	ivec3 baseCell;
	vec3 factor;

	if (!getLightFieldInterpolation(position, normal, baseCell, factor))
		return vec3(0.0);

	vec3 radiance = vec3(0.0);

	for (int z = 0; z < 2; ++z)
	{
		for (int y = 0; y < 2; ++y)
		{
			for (int x = 0; x < 2; ++x)
			{
				ivec3 offset = ivec3(x, y, z);
				float weight = getLightFieldWeight(offset, factor);

				radiance += evaluateLightFieldRadianceVoxel(baseCell + offset, normal) * weight;
			}
		}
	}

	return radiance * uLightFieldDifStrength;
}


vec3 evaluateLightFieldSelfSpecular(LightFieldContribution contribution, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	vec3 emittedDir = contribution.Direction.xyz;

	if (dot(emittedDir, emittedDir) <= LightEpsilon)
		return vec3(0.0);

	vec3 L = normalize(-emittedDir);

	float NoL = saturate(dot(normal, L));
	float NoV = saturate(dot(normal, viewDir));

	if (NoL <= 0.0 || NoV <= 0.0)
		return vec3(0.0);

	vec3 radiance = contribution.Color.rgb * uLightFieldSpecStrength;

	if (dot(radiance, radiance) <= LightEpsilon)
		return vec3(0.0);

#ifdef SIMPLIFIED
	return radiance * metalness * NoL;
#else
	vec3 H = normalize(L + viewDir);

	float NoH = saturate(dot(normal, H));
	float VoH = saturate(dot(viewDir, H));

	vec3 F0 = mix(Fdielectric, vec3(1.0), metalness);
	vec3 F = fresnelSchlick(F0, VoH);

	float D = distributionGGX(NoH, roughness);
	float G = geometrySmith(NoL, NoV, roughness);

	vec3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * NoL * NoV);

	return specularBRDF * radiance * NoL;
#endif
}


vec3 evaluateLightFieldSelfVoxel(ivec3 cell, vec3 albedo, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	uvec2 range = getLightFieldRange(cell);
	vec3 radiance = vec3(0.0);
	vec3 specular = vec3(0.0);
	uint end = range.x + range.y;

	for (uint i = range.x; i < end; ++i)
	{
		LightFieldContribution contribution = uLightFieldContributions[i];

#ifdef LIGHT_FIELD_SELF_OMNI
		radiance += contribution.Color.rgb;
#else
		vec3 emittedDir = contribution.Direction.xyz;

		if (dot(emittedDir, emittedDir) > LightEpsilon)
		{
			vec3 L = normalize(-emittedDir);
			float NoL = saturate(dot(normal, L));

			radiance += contribution.Color.rgb * NoL;
		}
#endif

		specular += evaluateLightFieldSelfSpecular(contribution, metalness, roughness, normal, viewDir);
	}

	radiance *= uLightFieldDifStrength;

	vec3 diffuse = albedo * radiance * (1.0 - metalness);

	return diffuse + specular;
}


vec3 evaluateLightFieldSelf(vec3 position, vec3 albedo, float metalness, float roughness, vec3 normal, vec3 viewDir)
{
	ivec3 baseCell;
	vec3 factor;

	if (!getLightFieldInterpolation(position, normal, baseCell, factor))
		return vec3(0.0);

	vec3 result = vec3(0.0);

	for (int z = 0; z < 2; ++z)
	{
		for (int y = 0; y < 2; ++y)
		{
			for (int x = 0; x < 2; ++x)
			{
				ivec3 offset = ivec3(x, y, z);
				float weight = getLightFieldWeight(offset, factor);

				if (weight <= 0.0)
					continue;

				result += evaluateLightFieldSelfVoxel(baseCell + offset, albedo, metalness, roughness, normal, viewDir) * weight;
			}
		}
	}

	return result;
}