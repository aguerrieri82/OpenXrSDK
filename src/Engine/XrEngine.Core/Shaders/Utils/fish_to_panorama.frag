in vec2 inUV;
out vec4 FragColor;

uniform sampler2D uTexture;

void main() {
    
   const float PI = 3.14159265358979323846;
   ivec2 textureSize = textureSize(uTexture, 0);


    vec2 pfish;
	float theta,phi,r;
	vec3 psph;
	
	float FOV = PI;

	theta = 2.0 * PI * (inUV.x - 0.5); // -pi to pi
	phi = PI * (inUV.y - 0.5);	// -pi/2 to pi/2

	// Vector in 3D space
	psph.x = cos(phi) * sin(theta);
	psph.y = cos(phi) * cos(theta);
	psph.z = sin(phi);
	
	// Calculate fisheye angle and radius
	theta = atan(psph.z, psph.x);
	phi = atan(length(psph.xz), psph.y);
	r = phi / FOV; 

	// Pixel in fisheye space
	pfish.x = 0.5 + r * cos(theta);
	pfish.y = 0.5 + r * sin(theta);

	FragColor = texture(uTexture, pfish);

}