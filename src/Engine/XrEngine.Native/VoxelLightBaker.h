#pragma once


#ifndef VOXEL_LIGHT_FACE_COUNT
#define VOXEL_LIGHT_FACE_COUNT 6
#endif



struct VoxelLightFieldView
{
    int32_t SizeX;
    int32_t SizeY;
    int32_t SizeZ;

    Vec3* Color[VOXEL_LIGHT_FACE_COUNT];
    Vec3* Direction[VOXEL_LIGHT_FACE_COUNT];

    int32_t CellCount;
    int32_t CellCapacity;
};



struct alignas(16) VoxelResolvedFace
{
    Vec4 BaseColor;
    Vec3 Normal;
    float Roughness;
    float Metallic;
};

static_assert(alignof(VoxelResolvedFace) == 16);
static_assert(sizeof(VoxelResolvedFace) == 48);

struct VoxelLightBakeParams
{
    float RaySpacingFactor;
    float EmptyDissipation;
    float EnergyThreshold;

    int32_t MaxBounceCount;
    int32_t ThreadCount;

    VoxelLightBakeParams();
};

struct PointLight
{
    Vec3 Position;
    Vec3 Color;

    float Intensity;
    float FalloffDistance;
};

struct VoxelMeshResolvedFace
{
    int32_t VoxelIndex;
    int32_t Face;

    VoxelFaceData Data;
    VoxelResolvedFace Resolved;
};

struct VoxelLightEnergy
{
    Vec3 Energy;

    Vec3 DirectionR;
    Vec3 DirectionG;
    Vec3 DirectionB;
};

struct VoxelLightFace
{
    VoxelLightEnergy Incoming;
    VoxelLightEnergy Outgoing;
};

struct VoxelLightData
{
    VoxelLightFace Faces[VOXEL_LIGHT_FACE_COUNT];
};

struct VoxelLightCell
{
    int32_t Index;
    VoxelLightData Data;
};

struct VoxelLightContribution
{
    std::vector<VoxelLightCell> Cells;
};

struct VoxelLightField
{
    int32_t SizeX;
    int32_t SizeY;
    int32_t SizeZ;

    std::vector<Vec3> Color[VOXEL_LIGHT_FACE_COUNT];
    std::vector<Vec3> Direction[VOXEL_LIGHT_FACE_COUNT];
};

struct VoxelLightRay
{
    Vec3 Position;
    Vec3 Direction;
    Vec3 Energy;
};

struct VoxelRayDebugState
{
    Vec3 Position;
    Vec3 Direction;
    Vec3 Energy;

    int32_t X;
    int32_t Y;
    int32_t Z;

    int32_t LastHitVoxel;
    int32_t LastAffectedVoxel;
    int32_t LastAffectedFace;

    int32_t BounceCount;

    VoxelData LastVoxel;
    VoxelLightData LastLightData;
    VoxelResolvedFace LastResolvedFaces[VOXEL_LIGHT_FACE_COUNT];
};

struct VoxelLightContributionView
{
    VoxelLightCell* Cells;
    int32_t CellCount;
};


class VoxelLightBaker;

class VoxelRayMarcher
{
private:
    struct RayState
    {
        Vec3 Position;
        Vec3 Direction;
        Vec3 Energy;

        int32_t X;
        int32_t Y;
        int32_t Z;

        int32_t LastHitVoxel;
        int32_t LastAffectedVoxel;
        int32_t LastAffectedFace;

        int32_t BounceCount;
        int32_t EnterFace;
    };

    struct WorkerContribution
    {
        VoxelLightContribution Contribution;

        std::vector<int32_t> CellSlots;
        std::vector<int32_t> TouchedVoxels;
    };

public:
    VoxelRayMarcher();

    void SetContext(
        VoxelLightBaker* baker,
        int32_t workerIndex);

    void Prepare(
        int32_t voxelCount);

    void TraceRange(
        int32_t startRay,
        int32_t endRay);

    bool CreateRay(const VoxelLightRay& ray);

    void GetDebugState(VoxelRayDebugState& state) const;

    bool Step();

    void ClearContribution();

    const VoxelLightContribution& Contribution() const;

private:
    VoxelLightBaker* _baker;
    int32_t _workerIndex;

    RayState _ray;
    WorkerContribution _local;

private:
    void TraceRay(const VoxelLightRay& ray);


    void AddContribution(
        int32_t voxelIndex,
        const VoxelLightData& data);
};

class VoxelLightBaker
{
    friend class VoxelRayMarcher;

private:

    struct ContributionMergeState
    {
        VoxelLightContribution Contribution;
        std::vector<int32_t> CellSlots;
        std::vector<int32_t> TouchedVoxels;
    };

public:
    struct SceneVoxel
    {
        VoxelData Voxel;
        VoxelResolvedFace ResolvedFaces[VOXEL_LIGHT_FACE_COUNT];
    };
public:
    VoxelLightBaker();

    explicit VoxelLightBaker(const VoxelLightBakeParams& params);

    void SetParams(const VoxelLightBakeParams& params);

    void SetGrid(const VoxelGridDesc& grid);

    void ClearScene();

    void AddMesh(
        const Int3& origin,
        const Int3& size,
        const VoxelData* voxels,
        const VoxelMeshResolvedFace* faces,
        int32_t faceCount);

    void BakePointLight(
        const PointLight& light,
        VoxelLightContribution& contribution);

    void ClearLightField();

    void AccumulateLight(
        const VoxelLightContribution& contribution);

    void GetLightField(
        VoxelLightField& field) const;


    int32_t GetVoxelCount() {
        return _voxelCount;
    }

    std::vector<SceneVoxel>* GetScene() {
        return &_scene;
    }

private:
    VoxelLightBakeParams _params;

    VoxelGridDesc _grid;
    int32_t _voxelCount;

    std::vector<SceneVoxel> _scene;
    std::vector<VoxelLightData> _lightField;

    PointLight _currentPointLight;

    std::vector<VoxelLightRay> _rays;
    std::vector<VoxelRayMarcher> _marchers;

    VoxelLightContribution* _currentContribution;
    ContributionMergeState _currentMerge;

    std::mutex _mergeLock;

private:
    void GeneratePointLightRays();

    void TraceRays(
        VoxelLightContribution& contribution);

    void MergeWorkerContribution(
        const VoxelLightContribution& workerContribution);

    void MergeContribution(
        VoxelLightContribution& target,
        ContributionMergeState& mergeState,
        const VoxelLightContribution& source);

    void ClearMergeState(
        ContributionMergeState& mergeState);

    void BuildLightField(
        VoxelLightField& field) const;

};
