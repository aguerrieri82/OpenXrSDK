const float M_PI = 3.141592653589793;


in vec3 v_Position;
in vec3 v_CameraPos;


#ifdef HAS_NORMAL_VEC3
#ifdef HAS_TANGENT_VEC4
in mat3 v_TBN;
#else
in vec3 v_Normal;
#endif
#endif


#ifdef HAS_COLOR_0_VEC3
in vec3 v_Color;
#endif
#ifdef HAS_COLOR_0_VEC4
in vec4 v_Color;
#endif


vec4 getVertexColor()
{
   vec4 color = vec4(1.0);

#ifdef HAS_COLOR_0_VEC3
    color.rgb = v_Color.rgb;
#endif
#ifdef HAS_COLOR_0_VEC4
    color = v_Color;
#endif

   return color;
}


struct NormalInfo {
    vec3 ng;   // Geometry normal
    vec3 t;    // Geometry tangent
    vec3 b;    // Geometry bitangent
    vec3 n;    // Shading normal
    vec3 ntex; // Normal from texture, scaling is accounted for.
};


float clampedDot(vec3 x, vec3 y)
{
    return clamp(dot(x, y), 0.0, 1.0);
}


float max3(vec3 v)
{
    return max(max(v.x, v.y), v.z);
}


float sq(float t)
{
    return t * t;
}

vec2 sq(vec2 t)
{
    return t * t;
}

vec3 sq(vec3 t)
{
    return t * t;
}

vec4 sq(vec4 t)
{
    return t * t;
}


float applyIorToRoughness(float roughness, float ior)
{
    // Scale roughness with IOR so that an IOR of 1.0 results in no microfacet refraction and
    // an IOR of 1.5 results in the default amount of microfacet refraction.
    return roughness * clamp(ior * 2.0 - 2.0, 0.0, 1.0);
}

vec3 rgb_mix(vec3 base, vec3 layer, vec3 rgb_alpha)
{
    float rgb_alpha_max = max(rgb_alpha.r, max(rgb_alpha.g, rgb_alpha.b));
    return (1.0 - rgb_alpha_max) * base + rgb_alpha * layer;
}

#ifdef USE_SHADOW_MAP

in vec4 v_PosLightSpace;

uniform sampler2D uShadowMap;

float calculateShadow(vec3 normal, vec3 lightDir)
{
    vec3 projCoords = v_PosLightSpace.xyz / v_PosLightSpace.w;

    projCoords = projCoords * 0.5 + 0.5;

    if (projCoords.z > 1.0)
        return 0.0;

    float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);  

    float currentDepth = projCoords.z - bias;

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
             
    return shadow;
}  

#endif