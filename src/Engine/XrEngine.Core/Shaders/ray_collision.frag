
uniform uint uDrawId;

layout(location=0) out uvec2 Ids;

void main()
{    
   Ids.x = uDrawId;
   Ids.y = uint(gl_PrimitiveID);
}