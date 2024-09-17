precision highp float;


#include <tonemapping.glsl>


uniform float uEnvIntensity;
uniform float uEnvBlurNormalized;
uniform int uMipCount;
uniform samplerCube uGGXEnvSampler;

out vec4 FragColor;
in vec3 v_TexCoords;


void main()
{
    vec4 color = textureLod(uGGXEnvSampler, v_TexCoords, uEnvBlurNormalized * float(uMipCount - 1));
    color.rgb *= uEnvIntensity;
    color.a = 1.0;

#ifdef LINEAR_OUTPUT
    FragColor = color.rgba;
#else
    FragColor = vec4(toneMap(color.rgb), color.a);
#endif
}
