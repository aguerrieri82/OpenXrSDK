
in vec2 inUV;
out vec4 FragColor;

uniform float uSize;
uniform vec4 uColor;
uniform vec2 uTexSize;
uniform sampler2D uDepth;


void main()
{    

    float value = texture(uDepth, inUV).r;
    if (value < 1.0)
        discard;

    bool found = false;

    for (float x = -uSize; x <= uSize; x++)
    {
        for (float y = -uSize; y <= uSize; y++)
        {
            vec2 offset = vec2(x, y) * uTexSize;
            value = texture(uDepth, inUV + offset).r;
            if (value < 1.0)
            {
                found = true;
                break;
            }
        }
        if (found)
            break;
    }

    if (!found)
        discard;

    FragColor = uColor;
}