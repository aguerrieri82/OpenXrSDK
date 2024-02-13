in vec2 fUv;

uniform sampler2DArray uTexture0;
uniform vec3 viewPos;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture0, vec3(fUv, 1));
}