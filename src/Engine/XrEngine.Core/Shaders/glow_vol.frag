
uniform vec3 sphereCenter; // Center of the sphere in world space
uniform float sphereRadius; // Radius of the sphere (r)
uniform float haloWidth; // Width of the halo (d)
uniform vec3 glowColor; // Color of the glow
uniform vec4 haloColor; // Color of the halo
uniform float stepSize;

uniform vec3 uViewPos; 
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

void main() {

    vec3 rayDirection = normalize(fPos - uViewPos);

    if (!intersectSphere(uViewPos, rayDirection, sphereCenter, sphereRadius + haloWidth)) 
        discard;
   

    // Step along the ray
    vec3 rayOrigin = uViewPos + rayDirection * t0; // Start point of ray inside the volume

    vec3 step = rayDirection * stepSize;


    // Order t0, t1
    if(t0 > t1) {
        float tmp = t0;
        t0 = t1;
        t1 = tmp;
    }

    // If both intersections are behind camera, discard
    if(t1 < 0.0) {
        discard;
    }

    // If the near intersection is behind camera, clamp it
    if(t0 < 0.0) {
        t0 = 0.0;
    }

    vec4 accumulatedColor = vec4(0.0); // Accumulated color and alpha

    vec3 samplePos = uViewPos + rayDirection * t0; 


    for (float t = t0; t < t1; t += stepSize) {

 
        float dist = length(samplePos - sphereCenter);

        if (dist <= sphereRadius)
            break;
       
        if (dist > sphereRadius && dist <= sphereRadius + haloWidth) {
            float alpha = 1.0 - ((dist - sphereRadius) / haloWidth); // Fade alpha
            accumulatedColor.a += (1.0 - accumulatedColor.a) * alpha * haloColor.a; // Blend alpha
        }

        if (accumulatedColor.a >= 1.0) 
            break;
               samplePos += step;

    }

    // Output the accumulated color

    fragColor.rgb = haloColor.rgb;
    fragColor.a = accumulatedColor.a;
}