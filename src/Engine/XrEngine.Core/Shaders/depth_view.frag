in vec2 fUv;

out vec4 FragColor;

#include "Shared/depth_sampler.glsl"    

#ifdef LINEARIZE

uniform float uNearPlane;
uniform float uFarPlane;

float linearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * uNearPlane * uFarPlane) / (uFarPlane + uNearPlane - z * (uFarPlane - uNearPlane));
}

#endif  

void main()
{   
    float depthValue = getDepth(fUv);

    #ifdef LINEARIZE
        FragColor = vec4(vec3(linearizeDepth(depthValue) / uFarPlane), 1.0);
    #else
        FragColor = vec4(vec3(depthValue), 1.0);
    #endif
}  