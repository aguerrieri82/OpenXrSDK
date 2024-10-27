in vec3 fPos;
in vec3 fNormal;

uniform sampler2D uTexture;
uniform vec3 uCenter;
uniform vec3 uViewPos;
uniform float uRadius;
uniform mat3 uRotation;
uniform vec2 uTexCenter;
uniform vec2 uTexRadius;
uniform float uShift;

layout(location=0) out vec4 FragColor;

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

    float inc = acos(intersectionPoint.y / sphereRadius);
    float azm = atan(intersectionPoint.z, intersectionPoint.x);

    polarCoordinates = vec2(azm + (azm < 0.0 ? PI : 0.0), (PI / 2.0) - inc);

    return true;
}

vec2 sampleFish(vec2 polar, float fov)
{
	vec2 result;
	result.x = uTexCenter.x + ((polar.x + PI / 2.0) / PI) * uTexRadius.x;  //-PI +PI
	result.y = uTexCenter.y + (polar.y / PI) * uTexRadius.y; //0-PI

    result.y = 1.0 - result.y;
    result.x = 1.0 - result.x;
    return result;
}

void main()
{

	vec3 viewDir = normalize(uViewPos - fPos);

    vec2 polar;

    if (raySphereIntersect(uViewPos, viewDir, uCenter, uRadius, polar))
    {
    	vec2 pfish = sampleFish(polar, PI);    

        if (polar.y > uShift - 0.01 && polar.y < uShift + 0.01)
            FragColor = vec4(polar.y, polar.x, 0.0, 1.0);
        else
            FragColor = vec4(1.0, 1.0, 1.0, 1.0);

        FragColor = vec4(texture(uTexture, pfish).rgb, 1.0);
    }
}