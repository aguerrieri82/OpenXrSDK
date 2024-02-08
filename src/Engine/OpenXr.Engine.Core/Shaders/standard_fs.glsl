#version 330 core

out vec4 FragColor;

struct DirectionalLight {
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform vec3 viewPos;
uniform vec3 objectColor;
uniform DirectionalLight light;

void main()
{
    // Normalize normal vector
    vec3 normal = normalize(Normal);

    // Calculate ambient light
    vec3 ambient = light.ambient * objectColor;

    // Calculate diffuse light
    float diff = max(dot(normal, -light.direction), 0.0);
    vec3 diffuse = light.diffuse * (diff * objectColor);

    // Calculate specular light
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-light.direction, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = light.specular * (spec * objectColor);

    // Final light intensity
    vec3 result = ambient + diffuse + specular;

    // Output final color
    FragColor = vec4(result, 1.0);
}