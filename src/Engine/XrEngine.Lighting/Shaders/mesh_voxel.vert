
precision highp float;
precision highp int;

layout(location = 2) in vec2 a_texcoord_0;

struct VoxelFaceData
{
    vec2 UV;
    vec2 HitPosition;
    int TriangleId;
    int Side;
};

struct VoxelFaceInstance
{
    ivec3 Pos;
    int Face;

    VoxelFaceData Data;
};

layout(std430, binding = 11) readonly buffer VoxelFaceBuffer
{
    VoxelFaceInstance uFaces[];
};

// Existing packed VertexData buffer reused as float storage.
// VertexData = 56 bytes = 14 floats
// Pos      0..2
// Normal   3..5
// UV       6..7
// UV1      8..9
// Tangent 10..13
layout(std430, binding = 12) readonly buffer VertexBuffer
{
    float uVertexData[];
};

layout(std430, binding = 13) readonly buffer IndexBuffer
{
    uint uIndices[];
};

uniform mat4 uViewProj;
uniform vec3 uGridOrigin;
uniform float uVoxelSize;

out vec3 vWorldPos;
out vec3 vFaceNormal;
out vec2 vUv;

flat out int vFace;
flat out int vOutIndex;

#ifdef VOXEL_REMAP
flat out vec3 vTriNormal;
flat out vec4 vTriTangent;
#endif

const int VertexStrideFloats = 14;

const vec3 FaceAnchor[6] = vec3[6](
    vec3(0.0, 0.0, 0.0), // NegX
    vec3(1.0, 0.0, 0.0), // PosX

    vec3(0.0, 0.0, 0.0), // NegY
    vec3(0.0, 1.0, 0.0), // PosY

    vec3(0.0, 0.0, 0.0), // NegZ
    vec3(0.0, 0.0, 1.0)  // PosZ
);

const vec3 FaceU[6] = vec3[6](
    vec3(0.0, 0.0, 1.0), // NegX
    vec3(0.0, 1.0, 0.0), // PosX

    vec3(1.0, 0.0, 0.0), // NegY
    vec3(0.0, 0.0, 1.0), // PosY

    vec3(0.0, 1.0, 0.0), // NegZ
    vec3(1.0, 0.0, 0.0)  // PosZ
);

const vec3 FaceV[6] = vec3[6](
    vec3(0.0, 1.0, 0.0), // NegX
    vec3(0.0, 0.0, 1.0), // PosX

    vec3(0.0, 0.0, 1.0), // NegY
    vec3(1.0, 0.0, 0.0), // PosY

    vec3(1.0, 0.0, 0.0), // NegZ
    vec3(0.0, 1.0, 0.0)  // PosZ
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
    return vec3(
        uVertexData[b + 0],
        uVertexData[b + 1],
        uVertexData[b + 2]);
}

vec3 VertexNormal(uint vertexIndex)
{
    int b = VertexBase(vertexIndex);
    return vec3(
        uVertexData[b + 3],
        uVertexData[b + 4],
        uVertexData[b + 5]);
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

void main()
{
    VoxelFaceInstance item = uFaces[gl_InstanceID];

    int face = item.Face;
    vec2 quadUv = a_texcoord_0;

    vec3 local =
        vec3(item.Pos) +
        FaceAnchor[face] +
        FaceU[face] * quadUv.x +
        FaceV[face] * quadUv.y;

    vec3 worldPos = uGridOrigin + local * uVoxelSize;

    vWorldPos = worldPos;
    vFaceNormal = FaceNormal[face];
    vUv = item.Data.UV;

    vFace = face;
    vOutIndex = gl_InstanceID;

#ifdef VOXEL_REMAP
    int tri = item.Data.TriangleId;
    int indexBase = tri * 3;

    uint i0 = uIndices[indexBase + 0];
    uint i1 = uIndices[indexBase + 1];
    uint i2 = uIndices[indexBase + 2];

#ifdef USE_GEOMETRIC_TRI_NORMAL
    vec3 p0 = VertexPos(i0);
    vec3 p1 = VertexPos(i1);
    vec3 p2 = VertexPos(i2);

    vTriNormal = normalize(cross(p1 - p0, p2 - p0));
#else
    vec3 n0 = VertexNormal(i0);
    vec3 n1 = VertexNormal(i1);
    vec3 n2 = VertexNormal(i2);

    vTriNormal = normalize(n0 + n1 + n2);
#endif

    vec4 t0 = VertexTangent(i0);
    vec4 t1 = VertexTangent(i1);
    vec4 t2 = VertexTangent(i2);

    vec3 triTangent = normalize(t0.xyz + t1.xyz + t2.xyz);
    vTriTangent = vec4(triTangent, t0.w);
#endif

    gl_Position = uViewProj * vec4(worldPos, 1.0);
}