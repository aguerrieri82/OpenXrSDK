
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
out vec2 tessLevelInner[];  

uniform float uTargetTriSize;
uniform float uHeightScale;  



#ifdef PBR_V2
    #include "../PbrV2/uniforms.glsl"
#else
    uniform vec3 uCameraPos;
    uniform vec3 uViewProj;
#endif

// calculate edge tessellation level from two edge vertices in screen space
float calcEdgeTessellation(vec2 s0, vec2 s1)
{
    float d = distance(s0, s1);
    return clamp(d / uTargetTriSize, 1, 64);
}

vec2 worldToScreen(vec4 p)
{
    #ifdef PBR_V2
        mat4 viewProj = uCamera.viewProj;
        vec2 viewSize = vec2(uCamera.viewSize);
    #else
        mat4 viewProj = uViewProj;
    #endif

    vec4 r = viewProj * p;    // to clip space
    r.xy /= r.w;              // project
    r.xy = r.xy * 0.5 + 0.5;  // to NDC
    r.xy *= viewSize;         // to pixels
    return r.xy;
}

vec2 eyeToScreen(vec4 p)
{
    #ifdef PBR_V2
        mat4 proj = uCamera.proj;
        vec2 viewSize = vec2(uCamera.viewSize);
    #else
        mat4 proj = uProj;
        vec2 viewSize = uViewSize;
    #endif

    vec4 r = proj * p;    // to clip space
    r.xy /= r.w;              // project
    r.xy = r.xy * 0.5 + 0.5;  // to NDC
    r.xy *= viewSize;         // to pixels
    return r.xy;
}


// calculate tessellation level by fitting sphere to edge
float calcEdgeTessellationSphere(vec3 w0, vec3 w1, float diameter)
{
    #ifdef PBR_V2
        mat4 viewMat = uCamera.view;
    #else
        mat4 viewMat = uView;
    #endif

    vec3 centre = (w0 + w1) * 0.5;
    vec4 view0 = viewMat * vec4(centre, 1.0);
    vec4 view1 = view0 + vec4(diameter, 0, 0, 0);

    vec2 s0 = eyeToScreen(view0);
    vec2 s1 = eyeToScreen(view1);

    return calcEdgeTessellation(s0, s1);
}


bool sphereInFrustum(vec3 pos, float r, vec4 plane[6])
{
    for(int i=0; i<6; i++) {
        if (dot(vec4(pos, 1.0), plane[i]) + r < 0.0) 
            return false;
    }
    return true;
}

void main() {   
    
    tcUv[gl_InvocationID] = fUv[gl_InvocationID];

    #ifdef HAS_TANGENTS
        tcTangentBasis[gl_InvocationID] = fTangentBasis[gl_InvocationID]; 
    #else
        tcNormal[gl_InvocationID] = fNormal[gl_InvocationID];
    #endif

    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;

    if (gl_InvocationID == 0)
    {
        vec3 cameraPos;
        vec4 frustumPlanes[6];
        
        #ifdef PBR_V2
            cameraPos = uCamera.cameraPosition;
            frustumPlanes = uCamera.frustumPlanes;  
        #else
            cameraPos = uCameraPos;
        #endif

        vec3 v0 = gl_in[0].gl_Position.xyz;
        vec3 v1 = gl_in[1].gl_Position.xyz;
        vec3 v2 = gl_in[2].gl_Position.xyz;
        vec3 v3 = gl_in[3].gl_Position.xyz;

        vec2 tileSize = vec2(length(v1-v0), length(v3-v0));

        vec3 halfTileSize = vec3(tileSize.x, tileSize.y, uHeightScale) * 0.5;

        float tileBoundingSphereR  = length(halfTileSize); 

        vec3 spherePos = (v0 + v1 + v2 + v3) / 4.0;

        bool visible = sphereInFrustum(spherePos, tileBoundingSphereR, frustumPlanes);

        if (!visible) {

            gl_TessLevelOuter[0] = 0.0;
            gl_TessLevelOuter[1] = 0.0;
            gl_TessLevelOuter[2] = 0.0;
            gl_TessLevelOuter[3] = 0.0;

            gl_TessLevelInner[0] = 0.0;
            gl_TessLevelInner[1] = 0.0;
        }
        else
        {
            float sphereD = tileSize.x * 2.0f;
 
            gl_TessLevelOuter[0] = calcEdgeTessellationSphere(v3, v0, sphereD);
            gl_TessLevelOuter[1] = calcEdgeTessellationSphere(v0, v1, sphereD);
            gl_TessLevelOuter[2] = calcEdgeTessellationSphere(v1, v2, sphereD);
            gl_TessLevelOuter[3] = calcEdgeTessellationSphere(v2, v3, sphereD);

            gl_TessLevelInner[0] = 0.5 * (gl_TessLevelOuter[1] + gl_TessLevelOuter[3]);
            gl_TessLevelInner[1] = 0.5 * (gl_TessLevelOuter[0] + gl_TessLevelOuter[2]);

            tessLevelInner[gl_InvocationID] = vec2(gl_TessLevelInner[0], gl_TessLevelInner[1]);
        }
   
    }
}
