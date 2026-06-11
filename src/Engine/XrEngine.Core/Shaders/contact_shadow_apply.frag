#ifdef MULTI_VIEW
    layout(binding = 0) uniform highp sampler2DArray uContactShadow;
#else
    layout(binding = 0) uniform sampler2D uContactShadow;
#endif

uniform float uApplyStrength;

layout(location = 0) in vec2 fUv;
layout(location = 0) out vec4 outColor;

float sampleContact(vec2 uv)
{
#ifdef MULTI_VIEW
    return texture(uContactShadow, vec3(uv, float(gl_ViewID_OVR))).r;
#else
    return texture(uContactShadow, uv).r;
#endif
}

void main()
{
    float contact = sampleContact(fUv) * uApplyStrength;

    outColor = vec4(0.0, 0.0, 0.0, contact);
}