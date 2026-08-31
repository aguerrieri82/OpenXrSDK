#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"
#include "Shared/vertex_post.glsl"

#ifdef USE_SKIN
    #include "Shared/skin.glsl"
#endif

#ifdef USE_MORPH
    #include "Shared/morph.glsl"
#endif

#ifdef PLANAR_REFLECTION
    #include "Shared/planar_reflection.glsl"
    out vec2 fPlanarUv;
#endif
 
#if (defined(USE_NORMAL_MAP) || defined(USE_CLEARCOAT_NORMAL_MAP) || defined(USE_ANISOTROPY)) && defined(HAS_TANGENTS) 
    #define USE_TANGENTS
#endif

layout(location=0) in vec3 aPosition;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec2 aUv0;

#ifdef HAS_TANGENTS
    layout(location=4) in vec4 aTangent;
#endif

#ifdef USE_CLIP_PLANE 
    uniform vec4 uClipPlane;
#endif

#ifdef HAS_UV2
    layout(location=3) in vec2 aUv2;
    out vec2 fUv2;
#endif

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#ifdef USE_CAMERA_POS
    out vec3 fCameraPos; 
#endif

#ifdef USE_TANGENTS
    out mat3 fTangentBasis;
#endif

#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif

#ifdef USE_DISPLACMENT_MAP
    out vec3 fOrigin;
#endif

#ifdef HAS_COLORMAP_PROJ
    uniform mat4 uColorMapProj;
    out vec4 fProjCoord;
#endif

#ifdef TANGENT_AS_CONST
    flat out vec4 fConst;
#endif 

#ifdef MOTION_VECTORS
    #include "shared/motion_vectors.glsl"
#endif

#slot VS_INCLUDES

void main()
{
    mat4 worldMatrix = uModel.worldMatrix;
    mat4 normalMatrix = uModel.normalMatrix;

    #ifdef USE_DEPTH_CULL

        ObjectData obj = uObjects[uModel.drawId];

        if (!obj.visible) {
            gl_Position = vec4(10.0, 0.0, 0.0, 1.0);
            return;
        }

    #endif

    vec3 position = aPosition;
    vec3 normal = aNormal;
    
    #ifdef HAS_TANGENTS
        vec4 tangent = aTangent;
    #endif

    #ifdef USE_MORPH
        applyMorph(position, normal
        #ifdef HAS_TANGENTS
            , tangent.xyz
        #endif
        );
    #endif

    #ifdef USE_SKIN
        skinTransform(position, normal);
    #endif

    #ifdef NORMAL_SCALE
        position += normalize(normal) * NORMAL_SCALE;
    #endif

    #slot VERTEX_LOCAL_TRANSFORMS

    vec4 pos = worldMatrix * vec4(position, 1.0);
    vec3 N = normalize(vec3(normalMatrix * vec4(normal, 0.0)));

    #ifdef FRAG_RAW_POS
        fPos = aPosition;
    #else
	    fPos = pos.xyz; 
    #endif

	fUv = aUv0;

    #ifdef USE_CAMERA_POS
	    fCameraPos = getViewPos();
    #endif

    #ifdef HAS_UV2
        fUv2 = aUv2;
    #endif
    
    #ifdef PLANAR_REFLECTION
        fPlanarUv = planarUV(pos);
    #endif

	#ifdef HAS_TEX_TRANSFORM
	    fUv = (vec3(aUv0.xy, 1) * HAS_TEX_TRANSFORM).xy;
	#endif

	#ifdef USE_SHADOW_MAP
	    fPosLightSpace = uCamera.lightSpaceMatrix * pos;
	#endif

    #ifdef USE_TANGENTS
        vec3 T = normalize(vec3(worldMatrix * vec4(tangent.xyz, 0.0)));
	    vec3 B = cross(N, T) * tangent.w;

        fTangentBasis = mat3(T, B, N);

    #else
        fNormal = N;
    #endif

    #ifdef USE_CLIP_PLANE 
        gl_ClipDistance[0] = -dot(pos, uClipPlane);
    #endif
    
    #ifdef HAS_COLORMAP_PROJ
        fProjCoord = uColorMapProj * pos;
    #endif

    #ifdef TANGENT_AS_CONST
        fConst = aTangent;
    #endif 

    computePos(pos);

    #ifdef MOTION_VECTORS
        computeMotionVectors(position);
    #endif

    doPost();
}
