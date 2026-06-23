
layout(binding = 0) uniform highp sampler2D uSource;

layout(location = 0) out vec4 outColor;

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 size = textureSize(uSource, 0);

    vec4 center = texelFetch(uSource, p, 0);

    if (center.a > 0.0)
    {
        outColor = center;
        return;
    }

    vec3 sum = vec3(0.0);
    float count = 0.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            if (x == 0 && y == 0)
                continue;

            ivec2 q = p + ivec2(x, y);

            if (q.x < 0 || q.y < 0 || q.x >= size.x || q.y >= size.y)
                continue;

            vec4 c = texelFetch(uSource, q, 0);

            if (c.a <= 0.0)
                continue;

            sum += c.rgb;
            count += 1.0;
        }
    }

    if (count > 0.0)
        outColor = vec4(sum / count, 1.0);
    else
        outColor = vec4(0.0);
}