
#ifdef USE_SHADOW_MAP 

in vec4 v_PosLightSpace;

#ifdef USE_SHADOW_SAMPLER

uniform sampler2DShadow uShadowMap;

#else

uniform sampler2D uShadowMap;

#endif


float calculateShadow(vec3 normal, vec3 lightDir)
{
    vec3 projCoords = v_PosLightSpace.xyz / v_PosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0)
        return 0.0;

    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);  

    float currentDepth = projCoords.z - bias;

#ifdef USE_SHADOW_SAMPLER

    float shadow  = 1.0 - texture(uShadowMap, vec3(projCoords.xy, currentDepth));

#else

    #ifdef SMOOTH_SHADOW_MAP

        float shadow = 0.0;
        vec2 texelSize = 1.0 / vec2(textureSize(uShadowMap, 0));
        for (float x = -1.0; x <= 1.0; ++x)
        {
            for (float y = -1.0; y <= 1.0; ++y)
            {
                float pcfDepth = texture(uShadowMap, projCoords.xy + vec2(x, y) * texelSize).r; 
                shadow += currentDepth > pcfDepth ? 1.0 : 0.0;        
            }    
        }

        shadow /= 9.0;

    #else
            
        float closestDepth = texture(uShadowMap, projCoords.xy).r; 

        float shadow = currentDepth > closestDepth  ? 1.0 : 0.0;  

    #endif

#endif
             
    return shadow;
}  

#endif
