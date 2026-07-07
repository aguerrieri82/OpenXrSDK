
#include "[XrEngine.Core]Shared/skin.glsl"

uniform bool uHasSkin;
uniform uint uActiveEye;

#ifdef MULTI_VIEW

    layout(num_views=2) in;
	#define EYE gl_ViewID_OVR

#else
	#define EYE uActiveEye
#endif


layout(std430, binding = 17) readonly buffer PrevSkinMatrices
{
    mat4 uPrevSkinMatrices[];
};


struct FrameMatrices {
	mat4 viewProj[2];
	mat4 model;
};

struct Matrices {
	FrameMatrices current;
	FrameMatrices prev;
};


uniform Matrices uMatrices;

layout (location = 0) in vec3 a_position;

out vec4 clipPos;
out vec4 prevClipPos;

void skinTransformCurPos(inout vec3 pos)
{
    SkinVertex skinVertex = uSkinVertices[gl_VertexID];

    uvec4 joints = skinVertex.jointIndices;
    vec4 weights = skinVertex.jointWeights;

    mat4 skin =
        weights.x * uSkinMatrices[joints.x] +
        weights.y * uSkinMatrices[joints.y] +
        weights.z * uSkinMatrices[joints.z] +
        weights.w * uSkinMatrices[joints.w];

    pos = (skin * vec4(pos, 1.0)).xyz;
}

void skinTransformPrevPos(inout vec3 pos)
{
    SkinVertex skinVertex = uSkinVertices[gl_VertexID];

    uvec4 joints = skinVertex.jointIndices;
    vec4 weights = skinVertex.jointWeights;

    mat4 skin =
        weights.x * uPrevSkinMatrices[joints.x] +
        weights.y * uPrevSkinMatrices[joints.y] +
        weights.z * uPrevSkinMatrices[joints.z] +
        weights.w * uPrevSkinMatrices[joints.w];

    pos = (skin * vec4(pos, 1.0)).xyz;
}


void main()
{
	vec3 curPos = a_position;
    vec3 prevPos = a_position;

	if (uHasSkin)
    {
        skinTransformCurPos(curPos);
        skinTransformPrevPos(prevPos);
    }

	clipPos = uMatrices.current.viewProj[EYE] * ( uMatrices.current.model * vec4(curPos, 1.0 ) );
	prevClipPos = uMatrices.prev.viewProj[EYE] * ( uMatrices.prev.model * vec4(prevPos, 1.0 ) );

	gl_Position = clipPos;
}