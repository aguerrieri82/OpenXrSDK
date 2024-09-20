#include <animation.glsl>


uniform mat4 uModelMatrix;
uniform mat4 uNormalMatrix;
 

layout(std140) uniform Camera {
    mat4 ViewMatrix;
    mat4 ProjectionMatrix;
    mat4 ViewProjectionMatrix;
    vec3 Position;
    float Exposure;
    float FarPlane;
} uCamera;

 
layout (location = 0) in vec3 a_position;
out vec3 v_Position;
out vec3 v_CameraPos;

#ifdef USE_SHADOW_MAP
uniform mat4 uLightSpaceMatrix;   
out vec4 v_PosLightSpace;
#endif

#ifdef HAS_NORMAL_VEC3
layout (location = 1) in vec3 a_normal;
#endif

#ifdef HAS_NORMAL_VEC3
#ifdef HAS_TANGENT_VEC4
layout (location = 4) in vec4 a_tangent;
out mat3 v_TBN;
#else
out vec3 v_Normal;
#endif
#endif

#ifdef HAS_TEXCOORD_0_VEC2
layout (location = 2) in vec2 a_texcoord_0;
#endif

#ifdef HAS_TEXCOORD_1_VEC2
layout (location = 3) in vec2 a_texcoord_1;
#endif

out vec2 v_texcoord_0;
out vec2 v_texcoord_1;

#ifdef HAS_COLOR_0_VEC3
in vec3 a_color_0;
out vec3 v_Color;
#endif

#ifdef HAS_COLOR_0_VEC4
in vec4 a_color_0;
out vec4 v_Color;
#endif


#ifdef MULTI_VIEW

    #define NUM_VIEWS 2
    #define VIEW_ID gl_ViewID_OVR

    layout(num_views=NUM_VIEWS) in;

    layout(std140) uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
        uniform vec4 position[NUM_VIEWS];
        float farPlane;
    } uMatrices;

    mat4 getViewProj() 
    {
       return uMatrices.viewProj[VIEW_ID];
    }

    vec3 getCameraPos() 
    {
       return uMatrices.position[VIEW_ID].xyz;
    }

#else
    mat4 getViewProj() 
    {
       return uCamera.ViewProjectionMatrix;
    }

    vec3 getCameraPos() 
    {
       return uCamera.Position;
    }
#endif


vec4 getPosition()
{
    vec4 pos = vec4(a_position, 1.0);

#ifdef USE_MORPHING
    pos += getTargetPosition(gl_VertexID);
#endif

#ifdef USE_SKINNING
    pos = getSkinningMatrix() * pos;
#endif

    return pos;
}


#ifdef HAS_NORMAL_VEC3
vec3 getNormal()
{
    vec3 normal = a_normal;

#ifdef USE_MORPHING
    normal += getTargetNormal(gl_VertexID);
#endif

#ifdef USE_SKINNING
    normal = mat3(getSkinningNormalMatrix()) * normal;
#endif

    return normalize(normal);
}
#endif

#ifdef HAS_NORMAL_VEC3
#ifdef HAS_TANGENT_VEC4
vec3 getTangent()
{
    vec3 tangent = a_tangent.xyz;

#ifdef USE_MORPHING
    tangent += getTargetTangent(gl_VertexID);
#endif

#ifdef USE_SKINNING
    tangent = mat3(getSkinningMatrix()) * tangent;
#endif

    return normalize(tangent);
}
#endif
#endif


void main()
{
    gl_PointSize = 1.0f;
    vec4 pos = uModelMatrix * getPosition();
    v_Position = vec3(pos.xyz) / pos.w;

#ifdef HAS_NORMAL_VEC3
#ifdef HAS_TANGENT_VEC4
    vec3 tangent = getTangent();
    vec3 normalW = normalize(vec3(uNormalMatrix * vec4(getNormal(), 0.0)));
    vec3 tangentW = normalize(vec3(uModelMatrix * vec4(tangent, 0.0)));
    vec3 bitangentW = cross(normalW, tangentW) * a_tangent.w;
    v_TBN = mat3(tangentW, bitangentW, normalW);
#else
    v_Normal = normalize(vec3(uNormalMatrix * vec4(getNormal(), 0.0)));
#endif
#endif

    v_texcoord_0 = vec2(0.0, 0.0);
    v_texcoord_1 = vec2(0.0, 0.0);

#ifdef HAS_TEXCOORD_0_VEC2
    v_texcoord_0 = a_texcoord_0;
#endif

#ifdef HAS_TEXCOORD_1_VEC2
    v_texcoord_1 = a_texcoord_1;
#endif

#ifdef USE_MORPHING
    v_texcoord_0 += getTargetTexCoord0(gl_VertexID);
    v_texcoord_1 += getTargetTexCoord1(gl_VertexID);
#endif


#ifdef HAS_COLOR_0_VEC3
    v_Color = a_color_0;
#if defined(USE_MORPHING)
    v_Color = clamp(v_Color + getTargetColor0(gl_VertexID).xyz, 0.0f, 1.0f);
#endif
#endif

#ifdef HAS_COLOR_0_VEC4
    v_Color = a_color_0;
#ifdef USE_MORPHING
    v_Color = clamp(v_Color + getTargetColor0(gl_VertexID), 0.0f, 1.0f);
#endif
#endif

#ifdef USE_SHADOW_MAP
    v_PosLightSpace = uLightSpaceMatrix * pos;
#endif

    v_CameraPos = getCameraPos();

    gl_Position = getViewProj() * pos;
    #ifdef ZLOG_F
        gl_Position.z = log(ZLOG_F*gl_Position.z + 1.0) / log(ZLOG_F*uCamera.FarPlane + 1.0) * gl_Position.w;
    #endif
}
