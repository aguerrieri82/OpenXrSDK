const float PI = 3.141592;
const float TwoPI = 2.0 * PI;
const float Epsilon = 0.00001;


float radicalInverseVdC(uint bits)
{
	return float(bitfieldReverse(bits)) * 2.3283064365386963e-10;
}

vec2 sampleHammersley(uint i, uint count)
{
	return vec2(float(i) / float(count), radicalInverseVdC(i));
}

vec3 sampleHemisphere(float u1, float u2)
{
	float u1p = sqrt(max(0.0, 1.0 - u1*u1));
	return vec3(cos(TwoPI*u2) * u1p, sin(TwoPI*u2) * u1p, u1);
}

vec3 sampleGGX(float u1, float u2, float roughness)
{
	float alpha = roughness * roughness;
	float cosTheta = sqrt((1.0 - u2) / (1.0 + (alpha*alpha - 1.0) * u2));
	float sinTheta = sqrt(max(0.0, 1.0 - cosTheta*cosTheta));
	float phi = TwoPI * u1;

	return vec3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
}

float ndfGGX(float cosLh, float roughness)
{
	float alpha = roughness * roughness;
	float alphaSq = alpha * alpha;
	float denom = (cosLh * cosLh) * (alphaSq - 1.0) + 1.0;

	return alphaSq / (PI * denom * denom);
}

vec3 sampleCharlie(float u1, float u2, float roughness)
{
	float alpha = max(roughness * roughness, 0.000001);
	float sinTheta = pow(u2, alpha / (2.0 * alpha + 1.0));
	float cosTheta = sqrt(max(0.0, 1.0 - sinTheta*sinTheta));
	float phi = TwoPI * u1;

	return vec3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
}

float ndfCharlie(float cosLh, float roughness)
{
	float alpha = max(roughness * roughness, 0.000001);
	float invAlpha = 1.0 / alpha;
	float cos2h = cosLh * cosLh;
	float sin2h = max(0.0, 1.0 - cos2h);

	return (2.0 + invAlpha) * pow(sin2h, invAlpha * 0.5) / (2.0 * PI);
}

float gaSchlickG1(float cosTheta, float k)
{
	return cosTheta / (cosTheta * (1.0 - k) + k);
}

float gaSchlickGGX_IBL(float cosLi, float cosLo, float roughness)
{
	float k = (roughness * roughness) / 2.0;
	return gaSchlickG1(cosLi, k) * gaSchlickG1(cosLo, k);
}

float visibilityAshikhmin(float cosLi, float cosLo)
{
	return clamp(1.0 / (4.0 * (cosLi + cosLo - cosLi * cosLo)), 0.0, 1.0);
}

vec3 getSamplingVector(ivec2 size, uint face)
{
	vec2 st = vec2(gl_GlobalInvocationID.xy) / vec2(size);
	vec2 uv = 2.0 * vec2(st.x, 1.0-st.y) - vec2(1.0);
	vec3 ret;

	if(face == 0u)      ret = vec3(1.0,  uv.y, -uv.x);
	else if(face == 1u) ret = vec3(-1.0, uv.y,  uv.x);
	else if(face == 2u) ret = vec3(uv.x, 1.0, -uv.y);
	else if(face == 3u) ret = vec3(uv.x, -1.0, uv.y);
	else if(face == 4u) ret = vec3(uv.x, uv.y, 1.0);
	else                ret = vec3(-uv.x, uv.y, -1.0);

	return normalize(ret);
}

void computeBasisVectors(const vec3 N, out vec3 S, out vec3 T)
{
	T = cross(N, vec3(0.0, 1.0, 0.0));
	T = mix(cross(N, vec3(1.0, 0.0, 0.0)), T, step(Epsilon, dot(T, T)));
	T = normalize(T);
	S = normalize(cross(N, T));
}

vec3 tangentToWorld(const vec3 v, const vec3 N, const vec3 S, const vec3 T)
{
	return S * v.x + T * v.y + N * v.z;
}
