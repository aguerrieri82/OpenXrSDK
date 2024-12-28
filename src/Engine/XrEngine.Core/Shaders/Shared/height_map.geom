layout(triangles, invocations = 1) in;
layout(triangle_strip, max_vertices = 3) out;

layout(location = 0) in vec2 tcUv[];     
layout(location = 1) in vec3 tcPos[];      
layout(location = 2) in vec3 tcCameraPos[]; 
layout(location = 3) in vec3 tcNormal[]; 


out vec2 fUv;     
out vec3 fPos;      
out vec3 fCameraPos; 
out vec3 fNormal; 


vec3 calcNormal(vec3 v0, vec3 v1, vec3 v2)
{
    vec3 edge0 = v1 - v0;
    vec3 edge1 = v2 - v0;
    return normalize(cross(edge1, edge0));
}

void main()
{
    vec3 v0 = tcPos[0];
    vec3 v1 = tcPos[1];
    vec3 v2 = tcPos[2];

    #ifdef NORMAL_GEO
        fNormal = -calcNormal(v0.xyz, v1.xyz, v2.xyz);
    #endif

    gl_Position = gl_in[0].gl_Position;
    fUv = tcUv[0];
    fPos = v0;
    fCameraPos = tcCameraPos[0];
    #ifndef NORMAL_GEO
        fNormal = tcNormal[0];
    #endif
    EmitVertex();

    gl_Position = gl_in[1].gl_Position;
    fUv = tcUv[1];
    fPos = v1;
    fCameraPos = tcCameraPos[1];
    #ifndef NORMAL_GEO
        fNormal = tcNormal[1];
    #endif
    EmitVertex();

    gl_Position = gl_in[2].gl_Position;
    fUv = tcUv[2];
    fPos = v2;
    fCameraPos = tcCameraPos[2];
    #ifndef NORMAL_GEO
        fNormal = tcNormal[2];
    #endif
    EmitVertex();

    EndPrimitive();
}