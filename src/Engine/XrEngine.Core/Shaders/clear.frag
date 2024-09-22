out vec4 FragColor;


void main()
{    
   FragColor = vec4(vec3(0.0, 0.0, 0.0), 1.0);
   gl_FragDepth = 1.0;
}