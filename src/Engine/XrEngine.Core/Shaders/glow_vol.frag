
#include "Shared/uniforms.glsl"

uniform vec3 sphereCenter; 
uniform float sphereRadius;
uniform float haloWidth; 
uniform vec4 haloColor; 
uniform float stepSize;

#ifdef USE_DEPTH_CULL

    uniform mat4 uInvViewProj;
    layout(binding=1) uniform sampler2D depthTexture;

#endif

in vec3 fPos;

out vec4 fragColor; 

float t0,t1;

bool intersectSphere(vec3 rayOrigin, vec3 rayDir, vec3 sphereCenter, float sphereRadius) {
    vec3 oc = rayOrigin - sphereCenter;      // Ray origin offset from sphere center
    float b = dot(oc, rayDir);              // Projection of oc onto rayDir
    float c = dot(oc, oc) - sphereRadius * sphereRadius; // Distance from sphere center squared
    float discriminant = b * b - c;         // Quadratic discriminant

    // Debug output for discriminant and input values
    if (discriminant < 0.0) 
        return false;
    

    float sqrtDiscriminant = sqrt(discriminant);
    t0 = -b - sqrtDiscriminant;             // Closest intersection point
    t1 = -b + sqrtDiscriminant;             // Furthest intersection point

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
        t0 = 0.0;
  
    vec3 step = rayDirection * stepSize;

    vec4 accumulatedColor = vec4(0.0); // Accumulated color and alpha

    vec3 samplePos = cameraPos + rayDirection * t0; 

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
            float alpha = 1.0 - ((dist - sphereRadius) / haloWidth); // Fade alpha
            accumulatedColor.a += (1.0 - accumulatedColor.a) * alpha * haloColor.a; // Blend alpha

            if (accumulatedColor.a >= 1.0) 
                break;
        }
        /*
        else if (accumulatedColor.a > 0.0) 
            break;
         */
        samplePos += step;

    }

    // Output the accumulated color

    fragColor.rgb = haloColor.rgb;
    fragColor.a = accumulatedColor.a;
}