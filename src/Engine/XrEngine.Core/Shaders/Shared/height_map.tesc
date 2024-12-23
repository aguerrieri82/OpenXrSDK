#ifdef USE_HEIGHT_MAP

layout(vertices = 3) out; // Quad patch for terrain

in vec2 texCoords[]; // Input texture coordinates from vertex shader
out vec2 tcTexCoords[]; // Pass to tessellation evaluation shader

uniform float uTessFactor; // Base tessellation factor
uniform vec3 uViewPos;      // Camera position for LOD

void main() {
    // Pass through texture coordinates
    tcTexCoords[gl_InvocationID] = texCoords[gl_InvocationID];

    // Compute tessellation level based on distance to camera
    vec3 worldPos = gl_in[gl_InvocationID].gl_Position.xyz;
    float distance = length(uViewPos - worldPos);
    float tessLevel = mix(uTessFactor * 4.0, uTessFactor, distance / 100.0);
    tessLevel = clamp(tessLevel, 1.0, 64.0);

    // Set tessellation levels
    gl_TessLevelOuter[0] = tessLevel;
    gl_TessLevelOuter[1] = tessLevel;
    gl_TessLevelOuter[2] = tessLevel;
    gl_TessLevelInner[0] = tessLevel;
}

#else

layout(vertices = 3) out; 

void main() {

    gl_TessLevelOuter[0] = 1.0;
    gl_TessLevelOuter[1] = 1.0;
    gl_TessLevelOuter[2] = 1.0;
    gl_TessLevelInner[0] = 1.0;

    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
}

#endif