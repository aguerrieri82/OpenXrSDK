uniform highp sampler3D uLightField[12];

uniform vec3 uLightFieldOrigin;
uniform float uVoxelSize;
uniform ivec3 uLightFieldSize;
uniform float uLightFieldStrength;
uniform float uLightFieldOfs;

const float LightEpsilon = 0.00001;

vec3 evaluateLightFieldFace(
	int face,
	vec3 uvw,
	vec3 albedo,
	float metalness,
	float roughness,
	vec3 normal,
	vec3 viewDir)
{
	vec3 radiance = texture(uLightField[face * 2 + 0], uvw).rgb * uLightFieldStrength;

	if (dot(radiance, radiance) <= LightEpsilon)
		return vec3(0.0);

	vec3 emittedDir = texture(uLightField[face * 2 + 1], uvw).rgb;

	// Stored direction is emitted/outgoing light direction.
	// BRDF wants direction from shaded point toward the light.
	vec3 L = -emittedDir;

	if (dot(normal, L) <= 0.0)
		return vec3(0.0);

	return evaluateDirectLight(
		albedo,
		metalness,
		roughness,
		normal,
		viewDir,
		L,
		radiance);
}

vec3 evaluateLightField(
	vec3 position,
	vec3 albedo,
	float metalness,
	float roughness,
	vec3 normal,
	vec3 viewDir)
{
	vec3 fieldSize = vec3(uLightFieldSize) * uVoxelSize;
	vec3 uvw = ((position + normal * (uVoxelSize * uLightFieldOfs)) - uLightFieldOrigin) / fieldSize;

	if (any(lessThan(uvw, vec3(0.0))) || any(greaterThan(uvw, vec3(1.0))))
		return vec3(0.0);

	vec3 result = vec3(0.0);

#ifdef USE_LIGHT_FIELD_ALL_FACES

	result += evaluateLightFieldFace(0, uvw, albedo, metalness, roughness, normal, viewDir);
	result += evaluateLightFieldFace(1, uvw, albedo, metalness, roughness, normal, viewDir);
	result += evaluateLightFieldFace(2, uvw, albedo, metalness, roughness, normal, viewDir);
	result += evaluateLightFieldFace(3, uvw, albedo, metalness, roughness, normal, viewDir);
	result += evaluateLightFieldFace(4, uvw, albedo, metalness, roughness, normal, viewDir);
	result += evaluateLightFieldFace(5, uvw, albedo, metalness, roughness, normal, viewDir);

#else

	if (normal.x > 0.0)
		result += evaluateLightFieldFace(0, uvw, albedo, metalness, roughness, normal, viewDir);
	else
		result += evaluateLightFieldFace(1, uvw, albedo, metalness, roughness, normal, viewDir);

	if (normal.y > 0.0)
		result += evaluateLightFieldFace(2, uvw, albedo, metalness, roughness, normal, viewDir);
	else
		result += evaluateLightFieldFace(3, uvw, albedo, metalness, roughness, normal, viewDir);

	if (normal.z > 0.0)
		result += evaluateLightFieldFace(4, uvw, albedo, metalness, roughness, normal, viewDir);
	else
		result += evaluateLightFieldFace(5, uvw, albedo, metalness, roughness, normal, viewDir);

#endif

	return result;
}