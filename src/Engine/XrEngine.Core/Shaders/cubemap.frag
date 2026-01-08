precision highp float;


layout(binding = 0) uniform samplerCube uCube;

out vec4 FragColor;
in vec3 v_TexCoords;


void main()
{
    vec4 color = texture(uCube, v_TexCoords);
    //color.r = 1.0;
    //color.a = 1.0;
    FragColor = color.rgba;
}
