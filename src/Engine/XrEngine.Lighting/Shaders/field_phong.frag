
in vec3 fPos;
in vec3 fNormal;

out vec4 outColor;

uniform vec3 uCameraPosition;

uniform sampler3D uLightField[12];

uniform vec3 uLightFieldOrigin;
uniform float uVoxelSize;
uniform ivec3 uGridSize;

const vec3 TestAlbedo = vec3(1.0);

const float IndirectStrength = 1.0;
const float SpecularStrength = 0.45;
const float Shininess = 64.0;

const float Ambient = 0.015;
const float LightEpsilon = 0.00001;

vec3 SampleFaceColor(int face, vec3 uvw)
{
	return texture(uLightField[face * 2 + 0], uvw).rgb;
}

vec3 SampleFaceDirection(int face, vec3 uvw)
{
	return texture(uLightField[face * 2 + 1], uvw).rgb;
}

vec3 SampleLightFieldPhong(vec3 worldPos, vec3 normal, vec3 viewDir)
{
	vec3 fieldSize = vec3(uGridSize) * uVoxelSize;
	vec3 uvw = (worldPos - uLightFieldOrigin) / fieldSize;

	if (any(lessThan(uvw, vec3(0.0))) || any(greaterThan(uvw, vec3(1.0))))
		return vec3(0.0);

	vec3 N = normalize(normal);
	vec3 V = normalize(viewDir);

	vec3 result = vec3(0.0);

	for (int face = 0; face < 6; ++face)
	{
		vec3 energy = SampleFaceColor(face, uvw);
		vec3 dir = SampleFaceDirection(face, uvw);

		float dirLen = length(dir);

		if (dirLen <= LightEpsilon)
			continue;

		vec3 L = -dir / dirLen;

		float NoL = max(dot(N, L), 0.0);

		if (NoL <= 0.0)
			continue;

		vec3 H = normalize(L + V);

		float NoH = max(dot(N, H), 0.0);
		float specular = pow(NoH, Shininess) * SpecularStrength;

		vec3 diffuseTerm = TestAlbedo * NoL;
		vec3 specularTerm = vec3(specular);

		result += energy * (diffuseTerm + specularTerm);
	}

	return result * IndirectStrength;
}

void main()
{
	vec3 N = normalize(fNormal);
	vec3 V = normalize(uCameraPosition - fPos);

	vec3 light = SampleLightFieldPhong(fPos, N, V);

	vec3 color = vec3(Ambient) + light;

	outColor = vec4(color, 1.0);
}