#include "ibl_shared.frag";


uniform sampler2D uPanorama;


vec2 dirToUV(vec3 dir)
{
    return vec2(
            0.5f + 0.5f * atan(dir.z, dir.x) / UX3D_MATH_PI,
            1.f - acos(dir.y) / UX3D_MATH_PI);
}

void main() 
{
	for(int face = 0; face < 6; ++face)
	{		
		vec3 scan = uvToXYZ(face, inUV*2.0-1.0);		
			
		vec3 direction = normalize(scan);		
	
		vec2 src = dirToUV(direction);		
			
		writeFace(face, texture(uPanorama, src).rgb);
	}
}
