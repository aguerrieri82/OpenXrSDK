#include "Shared/depth_sampler.glsl"    
#include "Shared/uniforms.glsl"    

in vec2 fUv;

layout(location=0) out vec4 FragColor;

#ifdef LINEARIZE 

float linearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * uCamera.nearPlane * uCamera.farPlane) / (uCamera.farPlane + uCamera.nearPlane - z * (uCamera.farPlane - uCamera.nearPlane));
}

#endif  

void main()
{   
    float depthValue = getDepth(fUv);

    #ifdef LINEARIZE
        FragColor = vec4(vec3(linearizeDepth(depthValue) / uCamera.farPlane), 1.0);
    #else
        FragColor = vec4(vec3(depthValue), 1.0);
    #endif
}  