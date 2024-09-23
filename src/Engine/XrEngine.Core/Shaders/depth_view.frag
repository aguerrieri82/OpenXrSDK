in vec2 fUv;

out vec4 FragColor;

uniform sampler2D uTexture0;

#ifdef SAMPLES
uniform sampler2DMS uTexture0MS;
#endif


uniform float uNearPlane;
uniform float uFarPlane;


float linearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * uNearPlane * uFarPlane) / (uFarPlane + uNearPlane - z * (uFarPlane - uNearPlane));
}

void main()
{   
    float depthValue;

    #ifdef SAMPLES
        vec2 texSize = vec2(textureSize(uTexture0MS));

        depthValue = 0.0;

        for (int i = 0; i < SAMPLES; i++)
            depthValue += texelFetch(uTexture0MS, ivec2(fUv * texSize), i).r;

        depthValue /= float(SAMPLES);
    #else
        depthValue = texture(uTexture0, fUv).r;
    #endif

    #ifdef LINEARIZE
        FragColor = vec4(vec3(linearizeDepth(depthValue) / uFarPlane), 1.0);
    #else
        FragColor = vec4(vec3(depthValue), 1.0);
    #endif
}  