﻿#include "Shared/uniforms.glsl"

in vec3 fPos;
in vec3 fNormal;
in vec2 fUv;

#ifdef EXTERNAL
    uniform samplerExternalOES uTexture;
#else
    uniform sampler2D uTexture;
#endif

uniform vec3  uSphereCenter;
uniform float uSphereRadius;

uniform mat3 uRotation;


uniform vec2 uTexCenter[2];
uniform vec2 uTexRadius[2];
uniform float uFov;

uniform vec2 uSurfaceSize;
uniform float uBorder;

uniform uint uActiveEye;
uniform uint uDebug;

uint activeEye;

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
    return uTexCenter[activeEye] + result * uTexRadius[activeEye];
}

vec2 mapCircularUV(vec2 uv, vec2 center, vec2 radius) {

    return center - radius / 2.0 + uv * (radius);
}

void main()
{
    #ifdef MULTI_VIEW
        activeEye = gl_ViewID_OVR;
    #else
        activeEye = uActiveEye;
    #endif

    vec3 cameraPos = uCamera.pos;

	vec3 viewDir = normalize(cameraPos - fPos);

    vec2 polar;

    if (raySphereIntersect(cameraPos, viewDir, uSphereCenter, uSphereRadius, polar))
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

    #ifdef DEBUG


    vec2 tex = mapCircularUV(fUv, uTexCenter[activeEye], uTexRadius[activeEye]);
    
    FragColor = vec4(texture(uTexture, tex).rgb, 1.0);

    #endif
}