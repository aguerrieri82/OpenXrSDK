
#ifdef USE_SHADOW_MAP 

    #define MODE_NONE 0
    #define MODE_HARD 1
    #define MODE_PCF  2
    #define MODE_VCF  3


    #ifdef USE_SHADOW_SAMPLER
        uniform sampler2DShadow uShadowMap;
    #else
        uniform sampler2D uShadowMap;
    #endif
    


    #if SHADOW_MAP_MODE == MODE_VCF
         
         const float MIN_VARIANCE = 0.00002;
         const float LIGHT_BLEED = 0.4;

         float linstep(float min, float max, float v)
         {
            return clamp((v - min) / (max - min), 0.0, 1.0);    
         }

          float calculateShadow(vec4 posLightSpace, vec3 normal, vec3 lightDir)
          {
                vec3 projCoords = posLightSpace.xyz / posLightSpace.w;

                projCoords = projCoords * 0.5 + 0.5;

                float currentDepth = projCoords.z;

                if (currentDepth >= 1.0)
                    return 0.0;

                vec2 moments = texture(uShadowMap, projCoords.xy).rg;

                float mean = moments.x;
                float meanSq = moments.y;
                float variance = meanSq - (mean * mean);
                variance = max(variance, MIN_VARIANCE);  

                float d = currentDepth - mean;
                float pMax = variance / (variance + d * d);
                float p = (currentDepth <= mean) ? 1.0 : 0.0;

                pMax = linstep(LIGHT_BLEED, 1.0, pMax);

                return 1.0 - max(p, pMax);
          }
    #else

        float calculateShadow(vec4 postLightSpace, vec3 normal, vec3 lightDir)
        {
            vec3 projCoords = postLightSpace.xyz / postLightSpace.w;

            projCoords = projCoords * 0.5 + 0.5;

            float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);  

            float currentDepth = projCoords.z - bias;

            if (currentDepth > 1.0)
                return 0.0;

            #ifdef USE_SHADOW_SAMPLER

                float shadow  = 1.0 - texture(uShadowMap, vec3(projCoords.xy, currentDepth));

            #else

                #if SHADOW_MAP_MODE == MODE_PCF    

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

#endif
