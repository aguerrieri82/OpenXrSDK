#include "uniforms.glsl"
#include "../Shared/position.glsl"

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=4) in vec4 tangent;
layout(location=2) in vec2 texcoord;

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;
out vec3 fCameraPos;    
out mat3 fTangentBasis;

#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif


void main()
{
	vec4 pos = uModel.worldMatrix * vec4(position, 1.0);

	fPos = pos.xyz; 

	fUv = texcoord;

	fCameraPos = getViewPos();

	#ifdef HAS_TEX_TRANSFORM
	    fUv = (vec3(texcoord.xy, 1) * uMaterial.texTransform).xy;
	#endif

	#ifdef USE_SHADOW_MAP
	    fPosLightSpace = uCamera.lightSpaceMatrix * pos;
	#endif

    vec3 N = normalize(vec3(uModel.normalMatrix * vec4(normal, 0.0)));

    fNormal = N;

    #ifdef HAS_TANGENTS
        vec3 T = normalize(vec3(uModel.worldMatrix * vec4(tangent.xyz, 0.0)));
	    vec3 B = normalize(cross(T, N) * tangent.w);

        fTangentBasis = mat3(T, B, N);

    #else
        fTangentBasis = mat3(N, N, N);
    #endif

    #ifdef USE_CLIP_PLANE 
        gl_ClipDistance[0] = -dot(pos, uClipPlane);
    #endif

    #ifdef USE_HEIGHT_MAP
        gl_Position = pos;
    #else   
	    gl_Position = getViewProj() * pos;
    #endif
}
