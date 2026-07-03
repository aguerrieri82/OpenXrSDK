#pragma once



#define MAX_BOUNCES 4
#define VOXEL_LIGHT_FACE_COUNT 6

enum class VoxelLightMergeMode
{
    Add,
    MaxSample
};


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

struct VoxelLightBakeParams
{

    float EnergyThreshold;

    int32_t MaxBounceCount;
    int32_t ThreadCount;
    int32_t RaySubsample;

    bool SnapBounceDirection;
    bool InitiateLightField;
    bool NormalizeDir;
    bool FillEmptyDir;
    VoxelLightMergeMode MergeMode;

    VoxelLightBakeParams();
};

enum LightFalloffType : int32_t
{
    LightFalloffNone = 0,
    LightFalloffLinear = 1,
    LightFalloffQuadratic = 2
};

struct LightFalloff
{
    int32_t Type;
    float Range;
    float Factor;
};

struct PointLight
{
    Vec3 Position;
    Vec3 Color;

    float Intensity;
    LightFalloff Falloff;
};

struct VoxelMeshResolvedFace
{
    int32_t VoxelIndex;
    int32_t Face;

    Vec4 BaseColor;
    Vec3 Normal;
    float Roughness;
    float Metallic;

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
    VoxelLightEnergy Incoming[MAX_BOUNCES];
    VoxelLightEnergy Outgoing[MAX_BOUNCES];

    int16_t InVisitCount;
    int16_t OutVisitCount;
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
    Vec3 Origin;
    Vec3 Direction;
    Vec3 Energy;

    float Distance;
    float OriginTotalDistance;
    float TotalDistance;

    int32_t X;
    int32_t Y;
    int32_t Z;

    int32_t LastHitVoxel;
    int32_t LastAffectedVoxel;
    int32_t LastAffectedFace;

    int32_t BounceCount;

    VoxelData LastVoxel;
    VoxelLightData LastLightData;

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
        Vec3 Origin;
        Vec3 Direction;
        Vec3 Energy;

        float Distance;
        float OriginTotalDistance;
        float TotalDistance;

		Vec3 Position;

        int32_t X;
        int32_t Y;
        int32_t Z;

        int32_t LastHitVoxel;
        int32_t LastAffectedVoxel;
        int32_t LastAffectedFace;

        int32_t BounceCount;
        int32_t EnterFace;
        int32_t OriginStep;

		bool IsAlive;
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

    const VoxelLightContribution& Contribution() const { return _local.Contribution; }

private:
    VoxelLightBaker* _baker;
    int32_t _workerIndex;

    RayState _ray;
    WorkerContribution _local;

private:
    void TraceRay(const VoxelLightRay& ray);

    bool MoveToNextVoxel(int32_t& exitFace);

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

    void AdjustLightFieldDirections();

    void AccumulateLight(
        const VoxelLightContribution& contribution);

    VoxelLightField& GetLightField();

    int32_t GetVoxelSize() {
        return _grid.VoxelSize;
    }

    int32_t GetVoxelCount() {
        return _voxelCount;
    }

    std::vector<VoxelData>* GetScene() {
        return &_scene;
    }

private:
    VoxelLightBakeParams _params;

    VoxelGridDesc _grid;
    int32_t _voxelCount;

    VoxelLightField _field;

    std::vector<VoxelData> _scene;
    std::vector<VoxelLightData> _lightData;

    Vec3 _currentLightEnergy;
    LightFalloff _currentLightFalloff;
    Vec3 _currentLightCenter;
    int32_t _currentLightVoxel;

    std::vector<VoxelLightRay> _rays;
    std::vector<VoxelRayMarcher> _marchers;

    VoxelLightContribution* _currentContribution;
    ContributionMergeState _currentMerge;

    std::mutex _mergeLock;

private:
    void PrefillPointLightContribution();

    void GeneratePointLightRays();

    void TraceRays(
        VoxelLightContribution& contribution);

    void CleanupUnvisitedFaces(
        VoxelLightContribution& contribution);

    void MergeWorkerContribution(
        const VoxelLightContribution& workerContribution);

    void MergeContribution(
        VoxelLightContribution& target,
        ContributionMergeState& mergeState,
        const VoxelLightContribution& source);

    void ClearMergeState(
        ContributionMergeState& mergeState);

    void BuildLightField();

};
