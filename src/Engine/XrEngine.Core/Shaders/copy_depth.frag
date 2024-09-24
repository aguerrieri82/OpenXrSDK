layout(location = 1) out float outDepth;

void main()
{    
   outDepth = gl_FragCoord.z;
}