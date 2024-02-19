in vec2 fUv;

out vec4 FragColor;

uniform sampler2D uTexture0;
uniform sampler2DMS uTexture0MS;

uniform float uNearPlane;
uniform float uFarPlane;
uniform int uSamples;


float LinearizeDepth(float depth)
{
    float z = depth * 2.0 - 1.0;
    return (2.0 * uNearPlane * uFarPlane) / (uFarPlane + uNearPlane - z * (uFarPlane - uNearPlane));
}

void main()
{   
    float depthValue;

    if (uSamples > 1)
    {
        depthValue = 0.0;

        for (int i = 0; i < uSamples; i++)
            depthValue += texelFetch(uTexture0MS, ivec2(fUv * textureSize(uTexture0MS)), i).r;

        depthValue /= float(uSamples);
    }
    else 
    {
        depthValue = texture(uTexture0, fUv).r;
    }

    FragColor = vec4(vec3(LinearizeDepth(depthValue) / uFarPlane), 1.0);
}  