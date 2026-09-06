precision highp float;

in vec2 fUv;

layout(location = 0) out vec4 outColor;

#ifdef MULTI_VIEW
layout(binding = 0) uniform sampler2DArray uSource;

vec4 sampleSource(vec2 uv)
{
	return textureLod(uSource, vec3(uv, float(gl_ViewID_OVR)), 0.0);
}

vec2 sourceSize()
{
	return vec2(textureSize(uSource, 0).xy);
}

#else

layout(binding = 0) uniform sampler2D uSource;

vec4 sampleSource(vec2 uv)
{
	return textureLod(uSource, uv, 0.0);
}

vec2 sourceSize()
{
	return vec2(textureSize(uSource, 0));
}

#endif


void main()
{
	vec2 texel = 1.0 / sourceSize();

#if BLUR_LEVEL == 1

	// One bilinear sample at the destination texel center
	// gives the exact 2x2 source box average.
	outColor = sampleSource(fUv);

#elif BLUR_LEVEL == 2

	// Four bilinear samples -> effective 4x4 box.
	vec2 d = texel;

	outColor =
		(sampleSource(fUv + vec2(-d.x, -d.y)) +
		 sampleSource(fUv + vec2( d.x, -d.y)) +
		 sampleSource(fUv + vec2(-d.x,  d.y)) +
		 sampleSource(fUv + vec2( d.x,  d.y))) * 0.25;

#elif BLUR_LEVEL == 3

	// Nine bilinear samples -> effective 6x6 box.
	vec2 d = texel * 2.0;

	outColor =
		(sampleSource(fUv + vec2(-d.x, -d.y)) +
		 sampleSource(fUv + vec2( 0.0, -d.y)) +
		 sampleSource(fUv + vec2( d.x, -d.y)) +

		 sampleSource(fUv + vec2(-d.x,  0.0)) +
		 sampleSource(fUv) +
		 sampleSource(fUv + vec2( d.x,  0.0)) +

		 sampleSource(fUv + vec2(-d.x,  d.y)) +
		 sampleSource(fUv + vec2( 0.0,  d.y)) +
		 sampleSource(fUv + vec2( d.x,  d.y))) * (1.0 / 9.0);

#endif
}