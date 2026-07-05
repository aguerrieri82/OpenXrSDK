
in vec3 vVoxelUv;
flat in int vFace;

out vec4 outColor;

uniform highp sampler3D uLightField[12];

uniform float uMaxIntensity;


const int MODE_COLOR = 0;
const int MODE_DIRECTION = 1;

vec3 SampleColor(int face)
{
	return texture(uLightField[face * 2 + 0], vVoxelUv).rgb;
}

vec3 SampleDirection(int face)
{
	return texture(uLightField[face * 2 + 1], vVoxelUv).rgb;
}

vec3 DirectionToColor(vec3 dir)
{
	float len = length(dir);

	if (len <= 0.00001)
		return vec3(0.0);
	return normalize(dir) * 0.5 + 0.5;
}

float Intensity(vec3 color)
{
	return max(color.r, max(color.g, color.b));
}

void main()
{
	int face = clamp(vFace, 0, 5);

	vec3 color = SampleColor(face);
	vec3 dir = SampleDirection(face);

	float intensity = Intensity(color);
	float alpha = clamp(intensity, 0.0, 1.0);

	if (MODE == MODE_DIRECTION)
	{

		outColor.rgb = DirectionToColor(dir);
		outColor.a = 1.0;

		if (dot(outColor.rgb, outColor.rgb) < 0.0001)
			discard;
		
		return;
	}

	if (alpha <= 0.0)
		discard;

	outColor = vec4(color, alpha) * uMaxIntensity;
}