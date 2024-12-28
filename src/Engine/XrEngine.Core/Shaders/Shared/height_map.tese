
layout(quads, equal_spacing, ccw) in;

#include "uniforms.glsl"

#ifdef HAS_TANGENTS
    in mat3 tcTangentBasis[];
    out mat3 fTangentBasis;
#else
    in vec3 tcNormal[];
#endif

in vec2 tcUv[];

#ifdef NORMAL_GEO

    layout(location = 0) out vec2 fUv;     
    layout(location = 1) out vec3 fPos;      
    layout(location = 2) out vec3 fCameraPos; 
    layout(location = 3) out vec3 fNormal; 
    layout(location = 4) out float fHeight;

#else
    out vec2 fUv;     
    out vec3 fPos;      
    out vec3 fCameraPos; 
    out vec3 fNormal; 
    out float fHeight;
#endif


layout(binding=8) uniform sampler2D heightMap; 

uniform float uHeightScale;  
uniform vec2 uHeightTexSize;
uniform vec3 uHeightNormalStrength;
uniform float uSphereRadius;    


void computeTBN(vec3 curNormal, out vec3 tangent, out vec3 bitangent) {

    vec3 reference = abs(curNormal.y) < 0.99 ? vec3(0.0, 1.0, 0.0) : vec3(1.0, 0.0, 0.0);
    tangent = normalize(cross(reference, curNormal));
    bitangent = normalize(cross(curNormal, tangent));
}

void main() {
    
    mat4 viewProj = uCamera.viewProj;
    fCameraPos = uCamera.pos;
  
    vec3 p0 = gl_in[0].gl_Position.xyz;
    vec3 p1 = gl_in[1].gl_Position.xyz;
    vec3 p2 = gl_in[2].gl_Position.xyz;
    vec3 p3 = gl_in[3].gl_Position.xyz;

    fUv = mix(mix(tcUv[0], tcUv[1], gl_TessCoord.x),
              mix(tcUv[3], tcUv[2], gl_TessCoord.x),
              gl_TessCoord.y);

    float hValue = texture(heightMap, fUv).r;

    #ifdef HEIGHT_MASK_VALUE

        if (hValue == HEIGHT_MASK_VALUE)
            hValue = 0.0;

    #endif


    fPos  = mix(mix(p0, p1, gl_TessCoord.x),
                mix(p3, p2, gl_TessCoord.x),
                gl_TessCoord.y);

    vec3 curNormal;

    vec2 texelSize = (1.0 / uHeightTexSize);

    #ifdef IS_SPHERE
    
        #ifndef HAS_TANGENTS
           #define HAS_TANGENTS
           mat3 fTangentBasis;
        #endif


        curNormal = normalize(fPos);

        fPos = curNormal * uSphereRadius;

        computeTBN(curNormal, fTangentBasis[0], fTangentBasis[1]);

        fTangentBasis[2] = curNormal;

    #else

        #ifdef HAS_TANGENTS
        
            fTangentBasis[0] = mix(mix(tcTangentBasis[0][0], tcTangentBasis[1][0], gl_TessCoord.x),
                                  mix(tcTangentBasis[3][0], tcTangentBasis[2][0], gl_TessCoord.x),
                                  gl_TessCoord.y);
            fTangentBasis[1] = mix(mix(tcTangentBasis[0][1], tcTangentBasis[1][1], gl_TessCoord.x),
                                  mix(tcTangentBasis[3][1], tcTangentBasis[2][1], gl_TessCoord.x),
                                  gl_TessCoord.y);
            fTangentBasis[2] = mix(mix(tcTangentBasis[0][2], tcTangentBasis[1][2], gl_TessCoord.x),
                                  mix(tcTangentBasis[3][2], tcTangentBasis[2][2], gl_TessCoord.x),
                                  gl_TessCoord.y);

           curNormal = fTangentBasis[2]; 

        #else

            curNormal = mix(mix(tcNormal[0], tcNormal[1], gl_TessCoord.x),
                mix(tcNormal[3], tcNormal[2], gl_TessCoord.x),
                gl_TessCoord.y);

        #endif

    #endif
    
    fHeight = hValue * uHeightScale;

    fPos += curNormal * fHeight;

    #if defined(USE_NORMAL_MAP) || defined(NORMAL_GEO)
        fNormal = curNormal;
    #else

        float dX, dY;

        #ifdef NORMAL_SOBEL

            float h00 = texture(heightMap, fUv + vec2(-texelSize.x, -texelSize.y)).r;
            float h10 = texture(heightMap, fUv + vec2(0.0, -texelSize.y)).r;
            float h20 = texture(heightMap, fUv + vec2(texelSize.x, -texelSize.y)).r;

            float h01 = texture(heightMap, fUv + vec2(-texelSize.x, 0.0)).r;
            float h21 = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r;

            float h02 = texture(heightMap, fUv + vec2(-texelSize.x, texelSize.y)).r;
            float h12 = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r;
            float h22 = texture(heightMap, fUv + vec2(texelSize.x, texelSize.y)).r;

            dX = h00 * -1.0 + h20 * 1.0 + h01 * -2.0 + h21 * 2.0 + h02 * -1.0 + h22 * 1.0;
            dY = h00 * -1.0 + h02 * 1.0 + h10 * -2.0 + h12 * 2.0 + h20 * -1.0 + h22 * 1.0;

        #else

            float hL = texture(heightMap, fUv - vec2(texelSize.x, 0.0)).r;
            float hR = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r;
            float hD = texture(heightMap, fUv - vec2(0.0, texelSize.y)).r;
            float hU = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r;

            dX = (hR - hL); 
            dY = (hU - hD);

        #endif

        vec3 hNormal = normalize(vec3(-dX * uHeightScale, -dY * uHeightScale, 1.0) * uHeightNormalStrength);

        #ifdef HAS_TANGENTS
            fNormal = normalize(fTangentBasis * hNormal);
        #else
            fNormal = normalize(curNormal + hNormal);
        #endif

    #endif


    gl_Position = viewProj * vec4(fPos, 1.0);
}