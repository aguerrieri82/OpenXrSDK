layout(binding = 0) uniform highp sampler2D uAccumTexture;

uniform float uMinWeight;

layout(location = 0) out vec4 outColor;

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    vec4 a = texelFetch(uAccumTexture, p, 0);

    if (a.a <= uMinWeight)
    {
        outColor = vec4(0.0);
        return;
    }

    outColor = vec4(a.rgb / a.a, 1.0);
}