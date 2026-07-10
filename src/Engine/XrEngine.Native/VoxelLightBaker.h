#pragma once

#define MAX_BOUNCES 4
#define VOXEL_LIGHT_FACE_COUNT 6

enum class DirectionCollapseMode 
{
    Add,
    Luminance
};

enum class LightTrackMode
{
    Full,
    Occlusions,
    OcclusionsOnly
};

enum class VoxelLightState : int8_t {
    Empty,
    Light,
    Occlusion
};

enum class VoxelLightMergeMode
{
    Add,
    MaxSample,
    AddPreserveDir
};

enum class LightCurveType : int32_t
{
    None = 0,
    Linear = 1,
    Quadratic = 2
};

enum class RayIntersectionMode : int32_t
{
    Direction = 0,
    Geometry = 1
};


enum VoxelLightFaceIndex : int32_t
{
	VoxelLightFaceNegX = 0,
	VoxelLightFacePosX = 1,
	VoxelLightFaceNegY = 2,
	VoxelLightFacePosY = 3,
	VoxelLightFaceNegZ = 4,
	VoxelLightFacePosZ = 5
};

struct VoxelFaceWeight
{
    int32_t Face;
    float Weight;
};


struct VoxelLightFieldView
{
    Vec3I Size;

    Vec3* Color[VOXEL_LIGHT_FACE_COUNT];
    Vec3* Direction[VOXEL_LIGHT_FACE_COUNT];

    int32_t CellCount;
    int32_t CellCapacity;
};

struct SmoothDirParams {
    int32_t Iterations;
    float Smoothness;
    float Relaxation;
    float MaxSlope;
};

struct BounceParams {

    int32_t MaxCount;
    int32_t RayCount;
    float RayDecay;
    float CenterWeight;
    float NormalWeight;
    float ConeMaxAngle;
};

struct BlurSample
{
    int32_t Dx;
    int32_t Dy;
    int32_t Dz;
    float Weight;
};


struct LightCurve
{
    LightCurveType Type;
    float Range;
    float Factor;
};

struct BlurParams
{
    int32_t Passes;
    float Strength;
    bool ColorOnly;
};



struct VoxelLightBakeParams
{
    LightTrackMode Mode;

    RayIntersectionMode IntersectMode;

    float EnergyThreshold;

    int32_t ThreadCount;
    int32_t RaySubsample;

    bool SnapBounceDirection;
    bool InitiateLightField;
    bool NormalizeDir;
    bool FillEmptyDir;
    bool CleanupMultisample;

    VoxelLightMergeMode RayMergeMode;
    VoxelLightMergeMode GenMergeMode;
    VoxelLightMergeMode LightMergeMode;

    DirectionCollapseMode DirCollapseMode;

    BlurParams Blur;

    BounceParams Bounce;

    SmoothDirParams SmoothDir;

    LightCurve Recovery;

    VoxelLightBakeParams();
};


struct PointLight
{
    Vec3 Position;
    Vec3 Color;

    float Intensity;
    LightCurve Falloff;
};

struct DirectionalLight
{
    Vec3 Position;
    Vec3 Direction;
    Vec3 Color;

    float Intensity;
    float Width;
    float Height;
    LightCurve Falloff;
};

struct SpotLight
{
    Vec3 Position;
    Vec3 Direction;
    Vec3 Color;

    float Intensity;
    LightCurve Falloff;

    float InnerCos;
    float OuterCos;
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

    int16_t VisitCount;
};

struct VoxelLightFace
{
    VoxelLightEnergy Incoming;
    VoxelLightEnergy Outgoing;
    VoxelLightState State;
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
    Vec3I Size;

    std::vector<Vec3> Color[VOXEL_LIGHT_FACE_COUNT];
    std::vector<Vec3> Direction[VOXEL_LIGHT_FACE_COUNT];
};

struct VoxelLightRay
{
    Vec3 Position;
    Vec3 Direction;
    Vec3 Energy;

    float OriginDistance;
    LightCurve Falloff;
    LightCurve Recovery;
};

struct GpuVoxelFaceData
{
    Vec3I Cell;

    int32_t Face;

    int32_t Side;

    Vec4 BaseColor;
    Vec3 Normal;
    float Roughness;
    float Metallic;
};

struct VoxelRayDebugState
{
    Vec3 Position;
    Vec3 Origin;
    Vec3 Direction;
    Vec3 Energy;

    float Distance;
    float OriginDistance;
    float TotalDistance;

    Vec3I Cell;

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
        Vec3 MaxEnergy;

        Vec3 OcclusionOrigin;
        Vec3 OcclusionEnergy;

        float Distance;
        float OriginDistance;
        float TotalDistance;

        LightCurve Falloff;
        LightCurve Recovery;

        Vec3 Position;

        Vec3I Cell;

        int32_t LastHitVoxel;
        int32_t LastAffectedVoxel;
        int32_t LastAffectedFace;

        int32_t BounceCount;
        int32_t OriginStep;

        VoxelLightState LightState;

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
        int32_t endRay,
        int32_t generation);

    bool CreateRay(
        const VoxelLightRay& ray,
        int32_t generation);

    void GetDebugState(VoxelRayDebugState& state) const;

    bool Step();

    void ClearContribution();

    const VoxelLightContribution& Contribution() const { return _local.Contribution; }
    const RayState& Ray() const { return _ray; }

    std::vector<VoxelLightRay>& NextRays() { return _nextRays; }

private:
    VoxelLightBaker* _baker;
    int32_t _workerIndex;

    RayState _ray;
    WorkerContribution _local;
    std::vector<VoxelLightRay> _nextRays;

private:

    void TraceRay(
        const VoxelLightRay& ray,
        int32_t generation);

    bool MoveToNextVoxel();

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
        const Vec3I& origin,
        const Vec3I& size,
        const VoxelData* voxels,
        const VoxelMeshResolvedFace* faces,
        int32_t faceCount);

    void AddGpuMeshFaces(
        const GpuVoxelFaceData* faces,
        int32_t faceCount);

    void BakePointLight(
        const PointLight& light,
        VoxelLightContribution& contribution);

    void BakeDirectionalLight(
        const DirectionalLight& light,
        VoxelLightContribution& contribution);

    void BakeSpotLight(
        const SpotLight& light,
        VoxelLightContribution& contribution);

    void ClearLightField();

    void AdjustLightFieldDirections();

    void AccumulateLight(
        const VoxelLightContribution& contribution);

    void ReconstructDirectionSurfaceForFace(
        int32_t face);

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

    std::vector<VoxelLightRay> _rays;
    std::vector<VoxelLightRay> _nextRays;
    std::vector<VoxelRayMarcher> _marchers;

    ContributionMergeState _currentMerge;

    std::mutex _mergeLock;

private:

    void BlurLightField();

    void PrefillPointLightContribution(
        const PointLight& light,
        VoxelLightContribution& contribution);

    void PrefillDirectionalLightContribution(
        const DirectionalLight& light,
        VoxelLightContribution& contribution);

    void PrefillSpotLightContribution(
        const SpotLight& light,
        VoxelLightContribution& contribution);

    void GeneratePointLightRays(
        const PointLight& light, bool fillMode);

    void GenerateDirectionalLightRays(
        const DirectionalLight& light);

    void GenerateSpotLightRays(
        const SpotLight& light);

    void TraceRays(
        VoxelLightContribution& contribution,
        std::vector<VoxelLightRay>& nextRays,
        int32_t generation);

    void CleanupUnvisitedFaces(
        VoxelLightContribution& contribution);

    void MergeContribution(
        VoxelLightContribution& target,
        ContributionMergeState& mergeState,
        const VoxelLightContribution& source,
        VoxelLightMergeMode mode = VoxelLightMergeMode::MaxSample);

    void ClearMergeState(
        ContributionMergeState& mergeState);

    void BuildLightField();

    void BakeGeneratedRays(VoxelLightContribution& contribution);

};
