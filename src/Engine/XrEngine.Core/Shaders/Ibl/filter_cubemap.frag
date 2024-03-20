#include "ibl_shared.frag";

void main() 
{
	//vec2 newUV = inUV * float(1 << (uCurrentMipLevel));
	 
	vec2 newUV = inUV*2.0-1.0;
	
	for(int face = 0; face < 6; ++face)
	{
		vec3 scan = uvToXYZ(face, newUV);		
			
		vec3 direction = normalize(scan);	
		direction.y = -direction.y;

		writeFace(face, filterColor(direction));
		
		//Debug output:
		//writeFace(face,  texture(uCubeMap, direction).rgb);
		//writeFace(face,   direction);
	}

	// Write LUT:
	// x-coordinate: NdotV
	// y-coordinate: roughness
	if (uCurrentMipLevel == 0u)
	{	
		outLUT = LUT(inUV.x, inUV.y);
	}

}