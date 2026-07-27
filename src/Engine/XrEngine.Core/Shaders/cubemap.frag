#include "Shared/tonemap.glsl"

precision highp float;

layout(binding = 0) uniform samplerCube uCube;

#ifdef COLOR_CORRECT
    uniform float uIntensity;
#endif

#ifdef MIP_FACTOR
    uniform int uMipCount;
    uniform float uMipFactor;
#endif

out vec4 FragColor;

in vec3 fUv;

void main()
{

#ifdef MIP_FACTOR
    vec4 color = textureLod(uCube, fUv, uMipFactor * float(uMipCount - 1));
#else
    vec4 color = texture(uCube, fUv);
#endif

    #ifdef COLOR_CORRECT
        color *= uIntensity;
    #endif

    FragColor = color.rgba;

    fixSrgbTex(FragColor);
}
