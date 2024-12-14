in vec2 inUV;

layout(location=0) out vec4 FragColor;

void main()
{    
   FragColor =  vec4(inUV.x, inUV.y, 0.0, 1.0);
}