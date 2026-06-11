
#define MODE_NONE 0
#define MODE_HARD 1
#define MODE_PCF  2
#define MODE_VCF  3

#define LIGHT_SIZE 10.0

uniform float uShadowBias;
uniform float uLightBleed;

const vec2 PCF_OFFSETS[8] = vec2[8](
    vec2(-0.707, -0.707),
    vec2( 0.707, -0.707),
    vec2(-0.707,  0.707),
    vec2( 0.707,  0.707),
    vec2(-1.0,  0.0),
    vec2( 1.0,  0.0),
    vec2( 0.0, -1.0),
    vec2( 0.0,  1.0));

#ifdef USE_SHADOW_MAP 

    #ifdef USE_SHADOW_SAMPLER
        layout(binding=14) uniform highp sampler2DShadow uShadowMap;
    #else
        layout(binding=14) uniform sampler2D uShadowMap;
    #endif

    float getShadowBias(vec3 normal, vec3  lightDir) 
    {
        #if SHADOW_BIAS == 1
            return max(0.002 * (1.0 - dot(normal, lightDir)), 0.005);  
        #elif SHADOW_BIAS == 2
            return uShadowBias;
        #else
            return 0.0;
        #endif 
    }
   

    #if SHADOW_MAP_MODE == MODE_VCF
         
         const float MIN_VARIANCE = 0.00002;

         float linstep(float min, float max, float v)
         {
            return clamp((v - min) / (max - min), 0.0, 1.0);    
         }

          float calculateShadow(vec4 posLightSpace, vec3 normal, vec3 lightDir)
          {
                vec3 projCoords = posLightSpace.xyz / posLightSpace.w;

                projCoords = projCoords * 0.5 + 0.5;

                float bias = getShadowBias(normal, lightDir);

                float currentDepth = projCoords.z - bias;

                vec2 moments = texture(uShadowMap, projCoords.xy).rg;

                float mean = moments.x;
                float meanSq = moments.y;
                float variance = meanSq - (mean * mean);
                variance = max(variance, MIN_VARIANCE);  

                float d = currentDepth - mean;
                float pMax = variance / (variance + d * d);
                float p = (currentDepth <= mean) ? 1.0 : 0.0;

                if (uLightBleed > 0.0)
                    pMax = linstep(uLightBleed, 1.0, pMax);

                return 1.0 - max(p, pMax);

          }
    #else

        float calculateShadow(vec4 postLightSpace, vec3 normal, vec3 lightDir)
        {
            vec3 projCoords = postLightSpace.xyz / postLightSpace.w;

            projCoords = projCoords * 0.5 + 0.5;

            float bias = getShadowBias(normal, lightDir);

            float currentDepth = projCoords.z - bias;

            if (currentDepth > 1.0)
                return 0.0;

            #if SHADOW_MAP_MODE == MODE_PCF    

                float shadow = 0.0;
                vec2 texelSize = 1.0 / vec2(textureSize(uShadowMap, 0));

                for (int i = 0; i < 8; ++i)
                {
                    #ifdef USE_SHADOW_SAMPLER
                        float lit = texture(
                            uShadowMap,
                            vec3(projCoords.xy + PCF_OFFSETS[i] * texelSize, currentDepth)
                        );

                        shadow += 1.0 - lit;
                    #else
                        float pcfDepth = texture(uShadowMap, projCoords.xy + PCF_OFFSETS[i] * texelSize).r;
                        shadow += currentDepth > pcfDepth ? 1.0 : 0.0;
                    #endif

                }

                shadow *= 1.0 / 8.0;

            #else
            
                #ifdef USE_SHADOW_SAMPLER

                    float shadow  = 1.0 - texture(uShadowMap, vec3(projCoords.xy, currentDepth));
                        
                #else

                    float closestDepth = texture(uShadowMap, projCoords.xy).r; 

                    float shadow = currentDepth > closestDepth  ? 1.0 : 0.0;  
                #endif

            #endif
             
            return shadow;
        }  

    #endif

#endif
