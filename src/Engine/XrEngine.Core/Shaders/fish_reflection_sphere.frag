in vec3 fPos;
in vec3 fNormal;

uniform sampler2D uTexture;
uniform vec3 uCenter;
uniform vec3 uViewPos;
uniform float uRadius;
uniform mat3 uRotation;

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
	result.x = 0.5 + r * cos(polar.y);
	result.y = 0.5 + r * sin(polar.y);
    return result;
}

void main()
{

	vec3 viewDir = normalize(uViewPos - fPos);


    vec2 polar;

    if (raySphereIntersect(uViewPos, viewDir, uCenter, uRadius, polar))
    {
    	vec2 pfish = sampleFish(polar, PI);
	    FragColor = vec4(texture(uTexture, pfish).rgb, 1.0);
    }
}