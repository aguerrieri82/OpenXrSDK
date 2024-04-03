in vec2 fUv;

uniform sampler2D uTexture;


out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture, fUv);

}