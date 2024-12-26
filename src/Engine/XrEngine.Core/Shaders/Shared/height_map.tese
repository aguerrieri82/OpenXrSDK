
layout(quads) in;

#include "uniforms.glsl"

#ifdef HAS_TANGENTS
    in mat3 tcTangentBasis[];
    out mat3 fTangentBasis;
#else
    in vec3 tcNormal[];
#endif

in vec2 tcUv[];

out vec2 fUv;     
out vec3 fPos;      
out vec3 fCameraPos; 
out vec3 fNormal; 
out vec3 fDebug;


layout(binding=8) uniform sampler2D heightMap; 

uniform float uHeightScale;  
uniform vec2 uHeightTexSize;
uniform float uHeightNormalStrength;


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


    fPos  = mix(mix(p0, p1, gl_TessCoord.x),
                mix(p3, p2, gl_TessCoord.x),
                gl_TessCoord.y);

    vec2 texelSize = (1.0 / uHeightTexSize);

    vec3 curNormal;

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

        curNormal  =  mix(mix(tcNormal[0], tcNormal[1], gl_TessCoord.x),
                    mix(tcNormal[3], tcNormal[2], gl_TessCoord.x),
                    gl_TessCoord.y);
    #endif
    
    float height = texture(heightMap, fUv).r * uHeightScale;

    fPos += curNormal * height;

    #ifdef USE_NORMAL_MAP
        fNormal = curNormal;
    #else

        float dX, dY;

        #ifdef NORMAL_SOBEL

            vec3 h00 = texture(heightMap, fUv + vec2(-texelSize.x, -texelSize.y)).rgb;
            vec3 h10 = texture(heightMap, fUv + vec2(0.0, -texelSize.y)).rgb;
            vec3 h20 = texture(heightMap, fUv + vec2(texelSize.x, -texelSize.y)).rgb;

            vec3 h01 = texture(heightMap, fUv + vec2(-texelSize.x, 0.0)).rgb;
            vec3 h21 = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).rgb;

            vec3 h02 = texture(heightMap, fUv + vec2(-texelSize.x, texelSize.y)).rgb;
            vec3 h12 = texture(heightMap, fUv + vec2(0.0, texelSize.y)).rgb;
            vec3 h22 = texture(heightMap, fUv + vec2(texelSize.x, texelSize.y)).rgb;

            dX = h00.r * -1.0 + h20.r * 1.0 + h01.r * -2.0 + h21.r * 2.0 + h02.r * -1.0 + h22.r * 1.0;
            dY = h00.r * -1.0 + h02.r * 1.0 + h10.r * -2.0 + h12.r * 2.0 + h20.r * -1.0 + h22.r * 1.0;

        #else

            float hL = texture(heightMap, fUv - vec2(texelSize.x, 0.0)).r;
            float hR = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r;
            float hD = texture(heightMap, fUv - vec2(0.0, texelSize.y)).r;
            float hU = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r;

            dX = (hR - hL); 
            dY = (hU - hD);

        #endif

        vec3 hNormal = normalize(vec3(-dX * uHeightNormalStrength, -dY * uHeightNormalStrength, 1.0));

        #ifdef HAS_TANGENTS
            fNormal = normalize(fTangentBasis * hNormal);
        #else
            fNormal = normalize(curNormal + hNormal);
        #endif

    #endif


    gl_Position = viewProj * vec4(fPos, 1.0);
}