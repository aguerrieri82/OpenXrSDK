layout(location=5) in ivec4 aJointIndices;
layout(location=6) in vec4 aJointWeights;

#ifndef MAX_SKIN_JOINTS
    #define MAX_SKIN_JOINTS 128
#endif

layout(std140, binding=19) uniform SkinMatrices
{
    mat4 uSkinMatrices[MAX_SKIN_JOINTS];
};

void skinTransform(inout vec3 pos, inout vec3 normal)
{
    ivec4 joints = aJointIndices;
    vec4 weights = aJointWeights;

    mat4 skin =
        weights.x * uSkinMatrices[joints.x] +
        weights.y * uSkinMatrices[joints.y] +
        weights.z * uSkinMatrices[joints.z] +
        weights.w * uSkinMatrices[joints.w];

    pos = (skin * vec4(pos, 1.0)).xyz;
    normal = normalize(mat3(skin) * normal);
}