in vec2 fUv;

uniform sampler2D uTexture;
uniform vec2 uOffset;
uniform vec2 uScale;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture, fUv);

}