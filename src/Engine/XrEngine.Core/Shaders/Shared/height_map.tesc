
layout(vertices = 4) out; 

#ifdef HAS_TANGENTS
    in mat3 fTangentBasis[];
    out mat3 tcTangentBasis[];
#else
    in vec3 fNormal[]; 
    out vec3 tcNormal[]; 
#endif

in vec2 fUv[]; 
out vec2 tcUv[]; 

uniform float uTessFactor; 

#ifdef PBR_V2
    #include "../PbrV2/uniforms.glsl"
#else
    uniform vec3 uCameraPos;
#endif


void main() {
    
    vec3 cameraPos;

    #ifdef PBR_V2
        cameraPos = uCamera.cameraPosition;
    #else
        cameraPos = uCameraPos;
    #endif

    tcUv[gl_InvocationID] = fUv[gl_InvocationID];

    #ifdef HAS_TANGENTS
        tcTangentBasis[gl_InvocationID] = fTangentBasis[gl_InvocationID]; 
    #else
        tcNormal[gl_InvocationID] = fNormal[gl_InvocationID];
    #endif

    vec3 worldPos = gl_in[gl_InvocationID].gl_Position.xyz;
    float distance = length(cameraPos - worldPos);
    float tessLevel = mix(uTessFactor, 1.0, distance / 2.0);

    tessLevel = clamp(tessLevel, 1.0, 64.0);

    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;


    if (gl_InvocationID == 0) {

        gl_TessLevelOuter[0] = tessLevel;
        gl_TessLevelOuter[1] = tessLevel;
        gl_TessLevelOuter[2] = tessLevel;
        gl_TessLevelOuter[3] = tessLevel;
        gl_TessLevelInner[0] = tessLevel;
        gl_TessLevelInner[1] = tessLevel;
    }


}
