layout(points) in;
layout(triangle_strip, max_vertices = 4) out; // Max 6 slices (2 triangles each)

#include "uniforms.glsl"

uniform mat4 uModel;            // Model matrix
uniform int uNumSlices;         // Number of slices


out vec3 fPos; // Pass vertex position to fragment shader


const vec3 cubeSize = vec3(1.0, 1.0, 1.0);
const vec3 cubeCenter = vec3(0.0, 0.0, 0.0);


void main() {

    float faceId = gl_in[0].gl_Position.x;

    if (faceId >= float(uNumSlices))
        return;

    mat4 viewProj = uCamera.viewProj;
    mat4 view = uCamera.view;
    vec3 up = normalize( vec3(view[0][1], view[1][1], view[2][1]));
    vec3 forward = normalize(-vec3(view[0][2], view[1][2], view[2][2]));
    vec3 right = normalize(cross(forward, up));

    float t = float(faceId) / float(uNumSlices - 1); // Interpolation factor
    vec3 sliceCenter = cubeCenter + uCameraForward * (t - 0.5) * cubeSize.z;

    // Define the four corners of the slice quad in the camera-aligned plane
    vec3 corners[4] = vec3[](
        sliceCenter - right * cubeSize.x * 0.5 - up * cubeSize.y * 0.5,
        sliceCenter + right * cubeSize.x * 0.5 - up * cubeSize.y * 0.5,
        sliceCenter + right * cubeSize.x * 0.5 + up * cubeSize.y * 0.5,
        sliceCenter - right * cubeSize.x * 0.5 + up * cubeSize.y * 0.5
    );

    // Emit two triangles to form the quad
       
    vec4 pos;

    pos = uModel * vec4(corners[0], 1.0);
    fPos = pos.xyz;
    gl_Position = viewProj * pos;
    EmitVertex();

    pos = uModel * vec4(corners[1], 1.0);
    fPos = pos.xyz;
    gl_Position = viewProj * pos;
    EmitVertex();

    pos = uModel * vec4(corners[3], 1.0);
    fPos = pos.xyz;
    gl_Position = viewProj * pos;
    EmitVertex();
       
    pos = uModel * vec4(corners[2], 1.0);
    fPos = pos.xyz;
    gl_Position = viewProj * pos;
    EmitVertex();

    EndPrimitive();
}