
uniform vec3 uSphereCenter; // Center of the sphere in world space
uniform float uSphereRadius; // Radius of the sphere (r)
uniform float uHaloWidth; // Width of the halo (d)
uniform vec4 uHaloColor; // Color of the halo

uniform vec3 uCameraPos; 
in vec3 fPos;

out vec4 fragColor; 


void main() {

    float dist = length(fPos - uSphereCenter);

    if (dist > uSphereRadius && dist <= uSphereRadius + uHaloWidth) {
        float alpha = 1.0 - ((dist - uSphereRadius) / uHaloWidth);
        fragColor = uHaloColor;
        fragColor.a *= alpha;
        return;
    }
    
    discard;
}