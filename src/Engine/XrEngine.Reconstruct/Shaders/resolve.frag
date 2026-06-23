
layout(location = 0) out vec4 color;

layout(binding = 0) uniform sampler2D uAccumTexture;

in vec2 fUv;

void main()
{
    vec4 a = texture(uAccumTexture, fUv);

    if (a.a > 0.00001)
    {
        color.rgb = a.rgb / a.a;
        color.a = 1.0;
    }
    else
        color = vec4(0.0);

}