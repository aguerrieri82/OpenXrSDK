#pragma once

#pragma pack(push, 4)

struct Vec2
{
    float X;
    float Y;
};

struct Vec3
{
    float X;
    float Y;
    float Z;
};

struct Vec4
{
    float X;
    float Y;
    float Z;
    float W;
};

struct Bounds3
{
    Vec3 Max;
    Vec3 Min;
};

    struct VertexData
    {
        Vec3 Pos;
        Vec3 Normal;
        Vec2 UV;
        Vec2 UV1;
        Vec4 Tangent;
    };

struct Int3
{
    int32_t X;
    int32_t Y;
    int32_t Z;
};

struct VoxelGridDesc
{
    Vec3 Origin;
    float VoxelSize;

    int32_t SizeX;
    int32_t SizeY;
    int32_t SizeZ;
};

struct VoxelizeMeshParams
{
    int32_t ScanSubdiv;
};

enum class VoxelStatus : int32_t
{
    Free = 0,
    Occupied = 1
};

enum class VoxelTriangleSide : int32_t
{
    None = 0,
    Front = 1,
    Back = 2
};

enum class VoxelFace : int32_t
{
    NegX = 0,
    PosX = 1,
    NegY = 2,
    PosY = 3,
    NegZ = 4,
    PosZ = 5
};

enum class VoxelScanAxis : int32_t
{
    X = 0,
    Y = 1,
    Z = 2
};

struct VoxelFaceData
{
    Vec2 UV;
    Vec2 HitPosition;           // local 0..1 coordinate on the voxel face
    int32_t TriangleId;          // -1 = no hit
    VoxelTriangleSide Side;
    Vec4 BaseColor;
    Vec3 Normal;
    float Roughness;
    float Metallic;
};


struct VoxelData
{
    VoxelStatus Status;
    float Occupancy;
    VoxelFaceData Faces[6];
};

struct MeshVoxelGridInfo
{
    Int3 Origin;                 // global voxel coordinate
    Int3 Size;
};

struct MeshVoxelGrid
{
    MeshVoxelGridInfo Info;
    std::vector<VoxelData> Voxels;
};

struct MeshVoxelGridView
{
    MeshVoxelGridInfo Info;
    const VoxelData* Voxels;
    int32_t VoxelCount;
};

#pragma pack(pop)

struct VoxelTriangle
{
    Vec3 P0;
    Vec3 P1;
    Vec3 P2;

    Vec2 UV0;
    Vec2 UV1;
    Vec2 UV2;

    Vec3 Normal;

    int32_t Id;
};

struct VoxelProjectedIndex
{
    VoxelScanAxis ScanAxis;

    int32_t SizeU = 0;
    int32_t SizeV = 0;

    float OriginU = 0.0f;
    float OriginV = 0.0f;
    float CellSize = 0.0f;

    std::vector<std::vector<int32_t>> Cells;
};

struct VoxelFaceBuildData
{
    int32_t HitCount = 0;
    VoxelFaceData LastHit{};
};


static_assert(sizeof(Vec2) == 8);
static_assert(sizeof(Vec3) == 12);
static_assert(sizeof(Vec4) == 16);

static_assert(offsetof(VertexData, Pos) == 0);
static_assert(offsetof(VertexData, Normal) == 12);
static_assert(offsetof(VertexData, UV) == 24);
static_assert(offsetof(VertexData, UV1) == 32);
static_assert(offsetof(VertexData, Tangent) == 40);

static_assert(sizeof(VertexData) == 56);

class MeshVoxelizer
{
public:
    MeshVoxelizer();

    MeshVoxelGrid Voxelize(
        const VertexData* vertices,
        int32_t vertexCount,
        const uint32_t* indices,
        int32_t indexCount,
        const Bounds3& bounds,
        const VoxelGridDesc& grid,
        const VoxelizeMeshParams& params);

private:
    const VertexData* _vertices = nullptr;
    const uint32_t* _indices = nullptr;

    int32_t _vertexCount = 0;
    int32_t _indexCount = 0;

    Bounds3 _bounds{};
    VoxelGridDesc _grid{};
    VoxelizeMeshParams _params{};

    MeshVoxelGrid _result{};

    std::vector<VoxelTriangle> _triangles;

    VoxelProjectedIndex _indexX{};
    VoxelProjectedIndex _indexY{};
    VoxelProjectedIndex _indexZ{};

    std::vector<VoxelFaceBuildData> _faceBuild;

private:
    void ComputeSubGrid();
    void BuildTriangles();
    void BuildProjectedIndices();



    void BuildProjectedIndex(VoxelProjectedIndex& index, VoxelScanAxis axis);

    void SurfacePass(VoxelScanAxis axis);
    void ResolveFaces();

    void SolidPass(VoxelScanAxis axis);

    bool IntersectScanLineTriangle(
        VoxelScanAxis axis,
        float u,
        float v,
        const VoxelTriangle& tri,
        float& axisCoord,
        Vec2& uv,
        VoxelTriangleSide& side) const;

    void AddFaceHit(
        int32_t localX,
        int32_t localY,
        int32_t localZ,
        VoxelFace face,
        const VoxelFaceData& hit);

    int32_t VoxelIndex(int32_t x, int32_t y, int32_t z) const;
    int32_t FaceBuildIndex(int32_t voxelIndex, VoxelFace face) const;
};