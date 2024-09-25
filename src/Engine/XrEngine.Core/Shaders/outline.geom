
layout (triangles) in;               
layout (line_strip, max_vertices = 4) out; 

in vec3 fNormal[3]; 
in vec3 fPos[3];    

uniform mat4 uViewProj;
uniform vec3 uViewPos;
uniform float uSize; 
uniform float uThreshold; 

void main()
{
    vec3 edge1 = fPos[1] - fPos[0];
    vec3 edge2 = fPos[2] - fPos[0];
    vec3 faceNormal = normalize(cross(edge1, edge2));

    vec3 triangleCenter = (fPos[0] + fPos[1] + fPos[2]) / 3.0;

    vec3 viewVec = normalize(triangleCenter - uViewPos);

    float dotProduct = dot(faceNormal, viewVec);

    if (dotProduct < uThreshold)
    {
        gl_Position = uViewProj * vec4(fPos[0], 1.0);
        EmitVertex();
        gl_Position = uViewProj * vec4(fPos[1], 1.0);
        EmitVertex();
        gl_Position = uViewProj * vec4(fPos[2], 1.0);
        EmitVertex();
        gl_Position = uViewProj * vec4(fPos[0], 1.0);
        EmitVertex();
        EndPrimitive();
    }
}