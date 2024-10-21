uniform vec4 uShadowColor;
uniform vec3 uLightDirection;

in vec4 fPosLightSpace;
in vec3 fNormal;

out vec4 FragColor;

#include "Shared/shadow.glsl"	

void main()
{    
	float shadow = calculateShadow(fPosLightSpace, fNormal, -uLightDirection);

	FragColor =  shadow * uShadowColor;
}