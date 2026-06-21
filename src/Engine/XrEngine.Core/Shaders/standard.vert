#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

#ifdef HAS_SKIN
    #include "Shared/skin.glsl"
#endif

#ifdef PLANAR_REFLECTION
    #include "Shared/planar_reflection.glsl"
    out vec2 fPlanarUv;
#endif


layout(location=0) in vec3 a_position;
layout(location=1) in vec3 a_normal;
layout(location=2) in vec2 a_texcoord;

#ifdef HAS_TANGENTS
    layout(location=4) in vec4 a_tangent;
#endif

#ifdef USE_CLIP_PLANE 
    uniform vec4 uClipPlane;
#endif

#ifdef USE_DEPTH_CULL

    struct ObjectData {
        vec3 bboxMax;
        vec3 bboxMin;
        vec2 extent;
        bool visible;
        bool culled;
    };

    layout(std430, binding = 0) buffer ObjectBuffer {
        ObjectData objects[];
    };

#endif

#ifdef USE_INSTANCE

    struct ModelInstance {
        mat4 worldMatrix;
        mat4 normalMatrix;
	    uint drawId;    
    };

    layout(std430, binding = 4) buffer InstanceData {
        ModelInstance data[];
    };

#endif

#ifdef HAS_UV2
    layout(location=3) in vec2 a_texcoord2;
    out vec2 fUv2;
#endif

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#ifdef USE_CAMERA_POS
    out vec3 fCameraPos; 
#endif

#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS) 
    out mat3 fTangentBasis;
#endif

#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif

#ifdef USE_HEIGHT_MAP
    out vec3 fOrigin;
#endif

#ifdef HAS_COLORMAP_PROJ
    uniform mat4 uColorMapProj;
    out vec4 fProjCoord;
#endif

#ifdef TANGENT_AS_CONST
    flat out vec4 fConst;
#endif 

void main()
{
    #ifdef USE_INSTANCE

        #ifdef USE_DEPTH_CULL

            ObjectData obj = objects[data[gl_InstanceID].drawId];

            if (!obj.visible) {
                gl_Position = vec4(10.0, 0.0, 0.0, 1.0);
                return;
            }

        #endif

        mat4 worldMatrix = data[gl_InstanceID].worldMatrix;
        mat4 normalMatrix = data[gl_InstanceID].normalMatrix;

    #else
        mat4 worldMatrix = uModel.worldMatrix;
        mat4 normalMatrix = uModel.normalMatrix;
    #endif

    vec3 position = a_position;
    vec3 normal = a_normal;

    #ifdef HAS_SKIN
        skinTransform(position, normal);
    #endif

    #ifdef NORMAL_SCALE
        position += normalize(normal) * NORMAL_SCALE;
    #endif

    vec4 pos = worldMatrix * vec4(position, 1.0);
    vec3 N = normalize(vec3(normalMatrix * vec4(normal, 0.0)));

    #ifdef FRAG_RAW_POS
        fPos = a_position;
    #else
	    fPos = pos.xyz; 
    #endif

	fUv = a_texcoord;

    #ifdef USE_CAMERA_POS
	    fCameraPos = getViewPos();
    #endif

    #ifdef HAS_UV2
        fUv2 = a_texcoord2;
    #endif
    
    #ifdef PLANAR_REFLECTION
        fPlanarUv = planarUV(pos);
    #endif

	#ifdef HAS_TEX_TRANSFORM
	    fUv = (vec3(a_texcoord.xy, 1) * HAS_TEX_TRANSFORM).xy;
	#endif

	#ifdef USE_SHADOW_MAP
	    fPosLightSpace = uCamera.lightSpaceMatrix * pos;
	#endif

    #if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)
        vec3 T = normalize(vec3(worldMatrix * vec4(a_tangent.xyz, 0.0)));
	    vec3 B = cross(N, T) * a_tangent.w;

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
        fConst = a_tangent;
    #endif 

    computePos(pos);
}
