precision highp float;

#ifdef MULTI_VIEW

    layout(num_views=2) in;

#endif  

out vec2 fUv;

void main()
{
   float x = -1.0 + float((gl_VertexID & 1) << 2);
   float y = -1.0 + float((gl_VertexID & 2) << 1);
   fUv.x = (x+1.0)*0.5;
   fUv.y = (y+1.0)*0.5;    
   
   #ifdef FLIP_Y
     fUv.y = 1.0 - fUv.y;
   #endif

   gl_Position = vec4(x, y, 0.0, 1.0);
}