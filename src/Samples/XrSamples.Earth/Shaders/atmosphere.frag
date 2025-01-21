
#include "Shared/uniforms.glsl"


// Existing uniforms
uniform vec3 sphereCenter; 
uniform float sphereRadius;
uniform float haloWidth; 
uniform vec4 haloColor; 
uniform float stepSize;

// New uniforms for sunset and darkness effects
uniform vec3 uSunPosition;    // Position of the sun in world space
uniform vec3 uSunColor;       // Color of the sun, e.g., vec3(1.0, 0.8, 0.6)
uniform float uSunIntensity;  // Intensity of the sun's light

#ifdef USE_DEPTH_CULL
    uniform mat4 uInvViewProj;
    layout(binding=1) uniform sampler2D depthTexture;
#endif

in vec3 fPos;

out vec4 fragColor; 

float t0, t1;

// Ray-sphere intersection
bool intersectSphere(vec3 rayOrigin, vec3 rayDir, vec3 sphereCenter, float sphereRadius) {
    vec3 oc = rayOrigin - sphereCenter;
    float b = dot(oc, rayDir);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    float discriminant = b * b - c;

    if (discriminant < 1e-6) 
        return false;
    
    float sqrtDiscriminant = sqrt(discriminant);
    t0 = -b - sqrtDiscriminant;
    t1 = -b + sqrtDiscriminant;

    return true;
}

#ifdef USE_DEPTH_CULL
vec3 getWorldPosFromDepth(float depth, vec2 uv) {
    vec4 clipPos = vec4(uv * 2.0 - 1.0, depth, 1.0); 
    vec4 worldPos = uInvViewProj * clipPos;
    return worldPos.xyz / worldPos.w;
}
#endif

void main() {
    vec3 cameraPos = uCamera.pos;
    
    #ifdef USE_DEPTH_CULL
        vec2 uv = gl_FragCoord.xy / vec2(uCamera.viewSize);
        float sceneDepth = texture(depthTexture, uv).r;
        vec3 sceneIntersection = getWorldPosFromDepth(sceneDepth, uv);
        float depthIntPoint = length(sceneIntersection - cameraPos);
    #endif

    vec3 rayDirection = normalize(fPos - cameraPos);

    if (!intersectSphere(cameraPos, rayDirection, sphereCenter, sphereRadius + haloWidth)) 
        discard;    

    // If both intersections are behind camera, discard
    if (t1 < 0.0) 
        discard;

    // If the near intersection is behind camera, clamp it
    if (t0 < 0.0) 
        t0 = 1e-3;
  
    vec3 step = rayDirection * stepSize;

    vec4 accumulatedColor = vec4(0.0); // Accumulated color and alpha

    vec3 samplePos = cameraPos + rayDirection * t0; 

    // Calculate sun direction from camera position
    vec3 sunDir = normalize(uSunPosition - cameraPos);

    for (float t = t0; t < t1; t += stepSize) {
        float dist = length(samplePos - sphereCenter);
  
        if (dist <= sphereRadius )
            break;

        #ifdef USE_DEPTH_CULL
            if (length(samplePos - cameraPos) > depthIntPoint)
                break;
        #endif   

        if (dist <= sphereRadius + haloWidth) 
        {
            // Calculate local up vector
            vec3 localUp = normalize(samplePos - sphereCenter);
            
            // Calculate sun elevation angle
            float sunElevation = dot(sunDir, localUp);

            // Define elevation thresholds
            float sunsetStart = 0.0; // Sun at the horizon
            float sunsetEnd = 0.2;   // Sun slightly below the horizon
            
            // Calculate elevation-based sunset factor
            float elevationFactor = smoothstep(sunsetStart, sunsetEnd, sunElevation);
            float sunsetFactor = 1.0 - clamp(elevationFactor, 0.0, 1.0);
            
            // Reduce the maximum sunset effect strength
            float maxSunsetEffect = 1.0; // Adjust as needed (0.0 to 1.0)
            
            // Calculate the reddish tint based on the sunset factor
            vec3 sunsetColor = mix(haloColor.rgb, vec3(1.0, 0.5, 0.3), sunsetFactor * maxSunsetEffect);

            // Blend sunset color with darkness
            vec3 finalColor = sunsetColor * clamp(sunElevation + 0.1, 0.0, 1.0);
            
            // Calculate alpha based on distance
            float a1 = smoothstep(sphereRadius, sphereRadius + haloWidth, dist);
            float a2 = clamp(exp(-(dist - sphereRadius) / haloWidth), 0.0, 1.0);

            #if FADE_MODE == 1
                 float alpha = a1;
            #elif FADE_MODE == 2
                float alpha = a2;
            #else
                 float alpha = a1 * a2;
            #endif

            // Accumulate color with blending
            accumulatedColor.rgb += (1.0 - accumulatedColor.a) * alpha * finalColor * haloColor.a;
            accumulatedColor.a += (1.0 - accumulatedColor.a) * alpha * haloColor.a; // Blend alpha
    
            if (accumulatedColor.a >= 1.0) 
                break;
        }

        samplePos += step;
    }

      // Calculate the angle between the view direction and the sun direction
    float viewSunAngle = dot(rayDirection, sunDir);
    
    // Define halo parameters
    float haloRadius =  0.000002; // Adjust for halo size
    float haloFalloff = 0.0000005; // Adjust for halo softness
    
    // Calculate halo intensity based on angle
    float haloFactor = smoothstep(haloRadius, haloRadius - haloFalloff, viewSunAngle);
    
    // Apply halo color and intensity
    vec3 haloColorFinal = mix(vec3(1.0), accumulatedColor.rgb, haloFactor) * 0.7;



    // Final color assignment with optional blending
    #ifdef BLEND_COLOR
        fragColor.rgb = haloColorFinal.rgb;
    #else
        fragColor.rgb = haloColor.rgb; // Using accumulated color with effects
    #endif

    fragColor.a = accumulatedColor.a;
}