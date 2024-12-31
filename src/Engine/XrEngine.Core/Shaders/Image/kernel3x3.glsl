layout (local_size_x = 16, local_size_y = 16, local_size_z=1) in;

// Input texture (source)
layout (binding = 10) uniform highp sampler2DArray sourceTexture;

// Output texture (destination, upscaled)
layout (binding = 0, rgba16f) restrict writeonly uniform highp image2DArray destTexture;

uniform vec2 texelSize; // (1.0 / width, 1.0 / height) of the upscaled texture
uniform float weights[9];

void main() {

    vec2 uv = vec2(gl_GlobalInvocationID.xy) * texelSize;

    float color = 0.0;

    // 3x3 Gaussian blur, sample neighboring texels
    vec2 offsets[9] = vec2[](
        vec2(-texelSize.x, texelSize.y),  // top-left
        vec2(0.0, texelSize.y),           // top-center
        vec2(texelSize.x, texelSize.y),   // top-right
        vec2(-texelSize.x, 0.0),          // center-left
        vec2(0.0, 0.0),                   // center-center
        vec2(texelSize.x, 0.0),           // center-right
        vec2(-texelSize.x, -texelSize.y), // bottom-left
        vec2(0.0, -texelSize.y),          // bottom-center
        vec2(texelSize.x, -texelSize.y)   // bottom-right
    );

    // Loop through neighboring texels and apply Gaussian weights
    for (int i = 0; i < 9; i++) {
        color += texture(sourceTexture, vec3(uv + offsets[i], gl_GlobalInvocationID.z)).r * weights[i];
    }

    // Write the final blurred color to the destination texture
    imageStore(destTexture, ivec3(gl_GlobalInvocationID), vec4(color));
}