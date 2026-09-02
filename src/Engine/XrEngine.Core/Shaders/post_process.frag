
#ifdef MULTI_VIEW
    layout(binding = 0) uniform sampler2DArray uSource;
#else
    layout(binding = 0) uniform sampler2D uSource;
#endif

in vec2 fUv;

layout(location = 0) out vec4 oColor;

vec4 SampleSource(vec2 uv)
{
#ifdef MULTI_VIEW
    return texture(uSource, vec3(uv, float(gl_ViewID_OVR)));
#else
    return texture(uSource, uv);
#endif
}

#ifdef USE_FXAA

    vec4 Fxaa()
    {
        vec2 texel = 1.0 / vec2(textureSize(uSource, 0).xy);

        vec3 rgbNW = SampleSource(fUv + vec2(-texel.x, -texel.y)).rgb;
        vec3 rgbNE = SampleSource(fUv + vec2( texel.x, -texel.y)).rgb;
        vec3 rgbSW = SampleSource(fUv + vec2(-texel.x,  texel.y)).rgb;
        vec3 rgbSE = SampleSource(fUv + vec2( texel.x,  texel.y)).rgb;
        vec4 center = SampleSource(fUv);
        vec3 rgbM = center.rgb;

        const vec3 luma = vec3(0.299, 0.587, 0.114);

        float lumaNW = dot(rgbNW, luma);
        float lumaNE = dot(rgbNE, luma);
        float lumaSW = dot(rgbSW, luma);
        float lumaSE = dot(rgbSE, luma);
        float lumaM = dot(rgbM, luma);

        float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
        float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));

        vec2 dir;
        dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
        dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

        float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * (0.25 / 8.0), 1.0 / 128.0);
        float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);

        dir = clamp(dir * rcpDirMin, vec2(-8.0), vec2(8.0)) * texel;

        vec3 rgbA = 0.5 * (
            SampleSource(fUv + dir * (1.0 / 3.0 - 0.5)).rgb +
            SampleSource(fUv + dir * (2.0 / 3.0 - 0.5)).rgb
        );

        vec3 rgbB = rgbA * 0.5 + 0.25 * (
            SampleSource(fUv + dir * -0.5).rgb +
            SampleSource(fUv + dir *  0.5).rgb
        );

        float lumaB = dot(rgbB, luma);

        return vec4((lumaB < lumaMin || lumaB > lumaMax) ? rgbA : rgbB, center.a);
    }

#endif

void main()
{
    #ifdef USE_FXAA
        oColor = Fxaa();
    #else
        oColor = SampleSource(fUv);
    #endif
}