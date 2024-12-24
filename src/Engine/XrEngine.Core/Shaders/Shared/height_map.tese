
layout(quads) in;

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

#ifdef PBR_V2
    #include "../PbrV2/uniforms.glsl"
#else
    uniform vec3 uCameraPos;
    uniform mat4 uViewProj;
#endif

void main() {
    
    mat4 viewProj;

    #ifdef PBR_V2
        fCameraPos = uCamera.cameraPosition;
        viewProj = uCamera.viewProj;
    #else
        fCameraPos = uCameraPos;
        viewProj = uViewProj;
    #endif

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


    float height = texture(heightMap, fUv).r * uHeightScale;

    #ifdef HAS_TANGENTS
        
        mat3 tangentBasis;

        tangentBasis[0] = mix(mix(tcTangentBasis[0][0], tcTangentBasis[1][0], gl_TessCoord.x),
                              mix(tcTangentBasis[3][0], tcTangentBasis[2][0], gl_TessCoord.x),
                              gl_TessCoord.y);
        tangentBasis[1] = mix(mix(tcTangentBasis[0][1], tcTangentBasis[1][1], gl_TessCoord.x),
                              mix(tcTangentBasis[3][1], tcTangentBasis[2][1], gl_TessCoord.x),
                              gl_TessCoord.y);
        tangentBasis[2] = mix(mix(tcTangentBasis[0][2], tcTangentBasis[1][2], gl_TessCoord.x),
                              mix(tcTangentBasis[3][2], tcTangentBasis[2][2], gl_TessCoord.x),
                              gl_TessCoord.y);

        fPos += tangentBasis[2] * height;

        #ifdef USE_NORMAL_MAP
            fNormal = fTangentBasis[2];
        #else
            float hL = texture(heightMap, fUv - vec2(texelSize.x, 0.0)).r;
            float hR = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r;
            float hD = texture(heightMap, fUv - vec2(0.0, texelSize.y)).r;
            float hU = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r;

            float dhdx = (hR - hL); 
            float dhdy = (hU - hD);

            vec3 hNormal = normalize(vec3(dhdx, dhdy, uHeightNormalStrength));

            fNormal = normalize(tangentBasis * hNormal);
        #endif

    #else
    
        vec3 normal  =  mix(mix(tcNormal[0], tcNormal[1], gl_TessCoord.x),
                        mix(tcNormal[3], tcNormal[2], gl_TessCoord.x),
                        gl_TessCoord.y);
                    
        normal = normalize(normal);

        fPos += normal * height;

        vec3 position_dx = mix(mix(p0, p1, gl_TessCoord.x + texelSize.x),
                               mix(p3, p2, gl_TessCoord.x + texelSize.x),
                               gl_TessCoord.y);
        vec3 position_dy = mix(mix(p0, p1, gl_TessCoord.x),
                               mix(p3, p2, gl_TessCoord.x),
                               gl_TessCoord.y + texelSize.y);

        // Apply displacement to the offset positions
        float displacement_dx = texture(heightMap, fUv + vec2(texelSize.x, 0.0)).r * uHeightScale;
        float displacement_dy = texture(heightMap, fUv + vec2(0.0, texelSize.y)).r * uHeightScale;

        position_dx += normal * displacement_dx;
        position_dy += normal * displacement_dy;

        // Compute tangent vectors
        vec3 tangent_u = position_dx - fPos;
        vec3 tangent_v = position_dy - fPos;

        // Compute the normal as the cross product of the tangents
        fNormal = normalize(cross(tangent_u, tangent_v));


    #endif

    gl_Position = viewProj * vec4(fPos, 1.0);
}