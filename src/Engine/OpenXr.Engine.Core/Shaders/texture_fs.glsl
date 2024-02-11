in vec2 fUv;

uniform sampler2D uTexture0;
uniform vec3 viewPos;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture0, fUv);
}