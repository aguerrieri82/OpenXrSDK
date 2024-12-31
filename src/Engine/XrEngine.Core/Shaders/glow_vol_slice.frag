
uniform vec3 uSphereCenter; // Center of the sphere in world space
uniform float uSphereRadius; // Radius of the sphere (r)
uniform float uHaloWidth; // Width of the halo (d)
uniform vec4 uHaloColor; // Color of the halo
uniform int uNumSlices; 

in vec3 fPos;

out vec4 fragColor; 

void main() {

    float dist = length(fPos - uSphereCenter);

    if (dist > uSphereRadius && dist <= uSphereRadius + uHaloWidth) {
        float alpha = 1.0 - ((dist - uSphereRadius) / uHaloWidth);
        fragColor = uHaloColor;
        fragColor.a *= alpha * (1.0 / float(uNumSlices)) * 100.0;
        return;
    }
    
    discard;
}