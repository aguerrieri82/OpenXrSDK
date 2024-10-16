﻿in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;


const float GAMMA = 2.2;
const float INV_GAMMA = 1.0 / GAMMA;

#ifdef TEXTURE

uniform sampler2D uTexture0;

#endif

struct Material {
    vec3 ambient;
    vec3 diffuse;
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
uniform vec3 uViewPos;

out vec4 FragColor;

vec3 linearTosRGB(vec3 color)
{
    return pow(color, vec3(INV_GAMMA));
}

vec3 sRGBToLinear(vec3 srgbIn)
{
    return vec3(pow(srgbIn.xyz, vec3(GAMMA)));
}


void main()
{
      vec3 ambient = light.ambient * material.ambient;

      vec3 norm = normalize(fNormal);
      vec3 lightDirection = normalize(light.position - fPos);
      float diff = max(dot(norm, lightDirection), 0.0);
      vec3 diffuse = light.diffuse * (diff * material.diffuse);

      #ifdef TEXTURE
      diffuse =  diffuse * texture(uTexture0, fUv).rgb;
      #endif

      vec3 viewDirection = normalize(uViewPos - fPos);
      vec3 reflectDirection = reflect(-lightDirection, norm);
      float spec = pow(max(dot(viewDirection, reflectDirection), 0.0), material.shininess);
      vec3 specular = light.specular * (spec * material.specular);

       vec3 result = ambient + diffuse + specular;

      FragColor = vec4(result, 1.0);
}