layout(triangles) in;
layout(triangle_strip, max_vertices = 18) out; // Max 6 slices (2 triangles each)

uniform mat4 uViewProj;
uniform mat4 uModel;
uniform vec3 uViewPos;
uniform vec3 uCameraForward; // Camera forward vector
uniform vec3 uCameraUp; // Camera up vector
uniform int uNumSlices; // Number of slices

out vec3 fPos; // Pass texture coordinates to the fragment shader

const vec3 cubeCenter = vec3(0.0, 0.0, 0.0);
const vec3 cubeSize = vec3(1.0, 1.0, 1.0);


void main() {

    // Camera's right vector (cross product of forward and up)
    vec3 cameraRight = normalize(cross(uCameraForward, uCameraUp));
    vec3 cameraUp = normalize(cross(cameraRight, uCameraForward));

    for (int i = 0; i < uNumSlices; ++i) {
        float t = float(i) / float(uNumSlices - 1); // Interpolation factor
        vec3 sliceCenter = cubeCenter + uCameraForward * (t - 0.5) * cubeSize.z;

        // Define the four corners of the slice quad in the camera-aligned plane
        vec3 corners[4] = vec3[](
            sliceCenter - cameraRight * cubeSize.x * 0.5 - cameraUp * cubeSize.y * 0.5,
            sliceCenter + cameraRight * cubeSize.x * 0.5 - cameraUp * cubeSize.y * 0.5,
            sliceCenter + cameraRight * cubeSize.x * 0.5 + cameraUp * cubeSize.y * 0.5,
            sliceCenter - cameraRight * cubeSize.x * 0.5 + cameraUp * cubeSize.y * 0.5
        );

        for (int j = 0; j < 4; ++j) {
            gl_Position = uViewProj * uModel * vec4(corners[j], 1.0);
            fPos = corners[j];
            EmitVertex();
        }
        EndPrimitive();
    }
}
