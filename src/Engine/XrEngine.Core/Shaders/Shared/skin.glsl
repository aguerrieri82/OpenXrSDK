

struct SkinVertex
{
    uvec4 jointIndices;
    vec4 jointWeights;
};

layout(std430, binding = 18) readonly buffer SkinVertices
{
    SkinVertex uSkinVertices[];
};

layout(std430, binding = 19) readonly buffer SkinMatrices
{
    mat4 uSkinMatrices[];
};


void skinTransform(inout vec3 pos, inout vec3 normal)
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

    normal = normalize(mat3(skin) * normal);
}

