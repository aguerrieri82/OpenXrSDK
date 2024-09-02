in vec3 fPos;
in vec3 fNormal;
in vec2 fUv;

uniform sampler2D uTexture;
uniform vec3 uCenter;
uniform vec3 uViewPos;
uniform float uRadius;
uniform mat3 uRotation;
uniform vec2 uTexCenter;
uniform vec2 uTexRadius;
uniform float uFov;

uniform vec2 uSurfaceSize;
uniform float uBorder;

out vec4 FragColor;

const float PI = 3.14159265358979323846;



bool raySphereIntersect(vec3 rayOrigin, vec3 rayDirection, vec3 sphereCenter, float sphereRadius, out vec2 polarCoordinates) 
{
    vec3 oc = rayOrigin - sphereCenter;
    

    float a = dot(rayDirection, rayDirection);
    float b = 2.0 * dot(oc, rayDirection);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    
    float discriminant = b*b - 4.0*a*c;
    
    if (discriminant < 0.0) {
        return false;
    }
    
    float sqrtDiscriminant = sqrt(discriminant);
    float t1 = (-b - sqrtDiscriminant) / (2.0 * a);
    float t2 = (-b + sqrtDiscriminant) / (2.0 * a);
    
    float t = min(t1, t2); 
    vec3 intersectionPoint = (rayOrigin + t * rayDirection) - sphereCenter;
    
    intersectionPoint = uRotation * intersectionPoint;

    float lat = acos(intersectionPoint.z / sphereRadius);
    float lng = atan(-intersectionPoint.y, -intersectionPoint.x);

    polarCoordinates = vec2(lat, lng);
    return true;
}

vec2 sampleFish(vec2 polar, float fov)
{
    float r = clamp(polar.x / fov, -0.5, 0.5); 
	vec2 result;
	result.x = r * cos(polar.y);
	result.y = r * sin(polar.y);
    return uTexCenter + result * uTexRadius;
}

void main()
{

	vec3 viewDir = normalize(uViewPos - fPos);

    vec2 polar;

    if (raySphereIntersect(uViewPos, viewDir, uCenter, uRadius, polar))
    {
    	vec2 pfish = sampleFish(polar, uFov);
	    FragColor = vec4(texture(uTexture, pfish).rgb, 1.0);
    }

    if (uBorder > 0.0)
    {
        vec2 uvBorder = uBorder / uSurfaceSize;
        float left = mix(0.0, 1.0, min(fUv.x / uvBorder.x, 1.0));
        float right = mix(1.0, 0.0, max((fUv.x - (1.0 - uvBorder.x)) / uvBorder.x, 0.0));

        float top = mix(0.0, 1.0, min(fUv.y / uvBorder.y, 1.0));
        float bottom = mix(1.0, 0.0, max((fUv.y - (1.0 - uvBorder.y)) / uvBorder.y, 0.0));

        FragColor.a = min(min(left, right), min(top, bottom));
    }

}