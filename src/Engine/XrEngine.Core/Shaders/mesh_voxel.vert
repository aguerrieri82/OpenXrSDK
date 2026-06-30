#version 310 es

precision highp float;
precision highp int;

struct VoxelFaceInstance
{
    ivec3 Pos;
    int Face;
};

struct VoxelFaceData
{
    int TriangleId;
    vec2 UV;
    vec2 HitPosition;
    int Side;
};

layout(std430, binding = 0) readonly buffer VoxelFaceInstanceBuffer
{
    VoxelFaceInstance uFaceInstances[];
};

layout(std430, binding = 1) readonly buffer VoxelFaceDataBuffer
{
    VoxelFaceData uVoxelFaces[];
};

// Existing VBO reused as float storage.
// VertexData = 56 bytes = 14 floats:
// Pos      0..2
// Normal   3..5
// UV       6..7
// UV1      8..9
// Tangent 10..13
layout(std430, binding = 2) readonly buffer VertexBuffer
{
    float uVertexData[];
};

layout(std430, binding = 3) readonly buffer IndexBuffer
{
    uint uIndices[];
};

layout(location = 2) in vec2 a_texcoord_0;

uniform mat4 uViewProj;
uniform vec3 uGridOrigin;
uniform ivec3 uGridSize;
uniform float uVoxelSize;

out vec3 vWorldPos;
out vec3 vFaceNormal;
out vec2 vFaceUv;

flat out int vFace;
flat out int vFaceSlot;
flat out vec3 vTriNormal;
flat out vec4 vTriTangent;

const int VertexStrideFloats = 14;

const vec3 FaceAnchor[6] = vec3[6](
    vec3(0.0, 0.0, 0.0),
    vec3(1.0, 0.0, 0.0),

    vec3(0.0, 0.0, 0.0),
    vec3(0.0, 1.0, 0.0),

    vec3(0.0, 0.0, 0.0),
    vec3(0.0, 0.0, 1.0)
);

const vec3 FaceU[6] = vec3[6](
    vec3(0.0, 0.0, 1.0),
    vec3(0.0, 1.0, 0.0),

    vec3(1.0, 0.0, 0.0),
    vec3(0.0, 0.0, 1.0),

    vec3(0.0, 1.0, 0.0),
    vec3(1.0, 0.0, 0.0)
);

const vec3 FaceV[6] = vec3[6](
    vec3(0.0, 1.0, 0.0),
    vec3(0.0, 0.0, 1.0),

    vec3(0.0, 0.0, 1.0),
    vec3(1.0, 0.0, 0.0),

    vec3(1.0, 0.0, 0.0),
    vec3(0.0, 1.0, 0.0)
);

const vec3 FaceNormal[6] = vec3[6](
    vec3(-1.0,  0.0,  0.0),
    vec3( 1.0,  0.0,  0.0),

    vec3( 0.0, -1.0,  0.0),
    vec3( 0.0,  1.0,  0.0),

    vec3( 0.0,  0.0, -1.0),
    vec3( 0.0,  0.0,  1.0)
);

int VertexBase(uint vertexIndex)
{
    return int(vertexIndex) * VertexStrideFloats;
}

vec3 VertexPos(uint vertexIndex)
{
    int b = VertexBase(vertexIndex);
    return vec3(uVertexData[b + 0], uVertexData[b + 1], uVertexData[b + 2]);
}

vec3 VertexNormal(uint vertexIndex)
{
    int b = VertexBase(vertexIndex);
    return vec3(uVertexData[b + 3], uVertexData[b + 4], uVertexData[b + 5]);
}

vec4 VertexTangent(uint vertexIndex)
{
    int b = VertexBase(vertexIndex);
    return vec4(
        uVertexData[b + 10],
        uVertexData[b + 11],
        uVertexData[b + 12],
        uVertexData[b + 13]);
}

int VoxelIndex(ivec3 p)
{
    return p.x + p.y * uGridSize.x + p.z * uGridSize.x * uGridSize.y;
}

void main()
{
    VoxelFaceInstance item = uFaceInstances[gl_InstanceID];

    int face = item.Face;
    int voxelIndex = VoxelIndex(item.Pos);
    int faceSlot = voxelIndex * 6 + face;

    VoxelFaceData faceData = uVoxelFaces[faceSlot];

    int indexBase = faceData.TriangleId * 3;

    uint i0 = uIndices[indexBase + 0];
    uint i1 = uIndices[indexBase + 1];
    uint i2 = uIndices[indexBase + 2];

    vec3 n0 = VertexNormal(i0);
    vec3 n1 = VertexNormal(i1);
    vec3 n2 = VertexNormal(i2);

    vec4 t0 = VertexTangent(i0);
    vec4 t1 = VertexTangent(i1);
    vec4 t2 = VertexTangent(i2);

#ifdef USE_GEOMETRIC_TRI_NORMAL
    vec3 p0 = VertexPos(i0);
    vec3 p1 = VertexPos(i1);
    vec3 p2 = VertexPos(i2);
    vec3 triNormal = normalize(cross(p1 - p0, p2 - p0));
#else
    vec3 triNormal = normalize(n0 + n1 + n2);
#endif

    vec3 triTangent = normalize(t0.xyz + t1.xyz + t2.xyz);
    float tangentSign = t0.w;

    vec2 quadUv = a_texcoord_0;

    vec3 local =
        vec3(item.Pos) +
        FaceAnchor[face] +
        FaceU[face] * quadUv.x +
        FaceV[face] * quadUv.y;

    vec3 worldPos = uGridOrigin + local * uVoxelSize;

    vWorldPos = worldPos;
    vFaceNormal = FaceNormal[face];
    vFaceUv = faceData.UV;

    vFace = face;
    vFaceSlot = faceSlot;
    vTriNormal = triNormal;
    vTriTangent = vec4(triTangent, tangentSign);

    gl_Position = uViewProj * vec4(worldPos, 1.0);
}