in vec3 fNormal;
uniform vec4 uColor;

layout(location=0) out vec4 FragColor;
layout(location=1) out vec3 Normal;

void main()
{    
   FragColor = uColor;
   Normal = normalize(fNormal);
}