layout(location = 0) out float fragColor;

layout(binding=0) uniform sampler2D uImage;

in vec2 fUv;

void main()
{    
   fragColor = texture(uImage, fUv).r;
}