﻿#include "Shared/uniforms.glsl"

in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;


#ifdef TEXTURE

layout(binding=0) uniform sampler2D uTexture;

#endif

struct Material {
    vec3 ambient;
    vec4 diffuse;
    vec3 specular;
    float shininess;
};

struct Light {
    vec3 position;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform Material material;
uniform Light light;

layout(location=0) out vec4 FragColor;

void main()
{
    vec3 ambient = light.ambient * material.ambient;

    vec3 norm = normalize(fNormal);

	if (!gl_FrontFacing)
		norm = -norm;

    vec3 lightDirection = normalize(light.position - fPos);
    float diff = max(dot(norm, lightDirection), 0.0);
    vec3 diffuse = light.diffuse * (diff * material.diffuse.rgb);

    #ifdef TEXTURE
        diffuse = diffuse * texture(uTexture, fUv).rgb;
    #endif

    vec3 viewDirection = normalize(uCamera.pos - fPos);
    vec3 reflectDirection = reflect(-lightDirection, norm);
    float spec = pow(max(dot(viewDirection, reflectDirection), 0.0), material.shininess);
    vec3 specular = light.specular * (spec * material.specular);

    vec3 result = ambient + diffuse + specular;

    FragColor = vec4(result, material.diffuse.a);
}