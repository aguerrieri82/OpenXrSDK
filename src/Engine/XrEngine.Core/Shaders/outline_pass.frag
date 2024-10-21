in vec2 inUV;

out vec4 FragColor;

uniform float uSize;
uniform vec4 uColor;
uniform vec2 uTexSize;

#include "Shared/depth_sampler.glsl"    


void main()
{    

    float value = getDepth(inUV);
    if (value < 1.0)
        discard;

    bool found = false;

    for (float x = -uSize; x <= uSize; x++)
    {
        for (float y = -uSize; y <= uSize; y++)
        {
            vec2 offset = vec2(x, y) * uTexSize;
            value = getDepth(inUV + offset);
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