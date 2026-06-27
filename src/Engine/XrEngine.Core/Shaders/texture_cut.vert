#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

layout(location=0) in vec3 a_position;
layout(location=1) in vec3 a_normal;
layout(location=2) in vec2 a_texcoord;

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#define MODE_MAIN   0u
#define MODE_LAYERS 1u

uniform int uCount;
#define MAIN_QUOD_ID (uint(uCount) - 1u)

void main()
{
    mat4 worldMatrix = uModel.worldMatrix;
    mat4 normalMatrix = uModel.normalMatrix;
    vec3 position = a_position;
    vec3 normal = a_normal;
   
    #if (MODE == MODE_MAIN)

        uint quadId = uint(gl_VertexID) / 6u;

        if (quadId != MAIN_QUOD_ID)
        {
            position.z = 0.0001;

            uint corner = uint(gl_VertexID) % 6u;

            vec2 dir;

            if (corner == 0u)      dir = vec2(-1.0, -1.0); // A
            else if (corner == 1u) dir = vec2( 1.0, -1.0); // B
            else if (corner == 2u) dir = vec2(-1.0,  1.0); // D
            else if (corner == 3u) dir = vec2( 1.0, -1.0); // B
            else if (corner == 4u) dir = vec2( 1.0,  1.0); // C
            else                   dir = vec2(-1.0,  1.0); // D

            const float QUAD_PADDING = 0.004;

            
            position.xy += dir * QUAD_PADDING;
        }

    #endif

    vec4 pos = worldMatrix * vec4(position, 1.0);

	fUv = a_texcoord;

    fNormal = normalize(vec3(normalMatrix * vec4(normal, 0.0)));
    
    computePos(pos);
}
