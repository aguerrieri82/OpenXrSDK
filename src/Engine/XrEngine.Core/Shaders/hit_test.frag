in vec3 fNormal;

uniform uint uDrawId;

layout(location=0) out uvec2 Ids;
layout(location=1) out vec3 Normal;

void main()
{    
   Ids.x = uDrawId;
   Ids.y = uint(gl_PrimitiveID);

   Normal = normalize(fNormal);
}