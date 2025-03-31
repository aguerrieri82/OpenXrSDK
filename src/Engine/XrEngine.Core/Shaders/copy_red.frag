layout(location = 0) out float fragColor;

layout(binding=0) uniform sampler2D uImage;

in vec2 inUV;

void main()
{    
   fragColor = texture(uImage, inUV).r;
}