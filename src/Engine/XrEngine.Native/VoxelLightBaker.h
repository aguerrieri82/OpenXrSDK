#pragma once

#define MAX_BOUNCES 4
#define VOXEL_LIGHT_FACE_COUNT 6



enum class DirectionCollapseMode 
{
    Add,
    Luminance,
    Normal
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

    bool InitiateLightField;
    bool NormalizeDir;

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

    Vec3 DirectionN;

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
    Vec3 DirectionNormal;
    Vec3 Energy;

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

struct ContributionMergeState
{
    VoxelLightContribution Contribution;
    std::vector<int32_t> CellSlots;
    std::vector<int32_t> TouchedVoxels;
};


class VoxelLightBaker;
class VoxelRayMarcher;


using MergeEnergyFn = void (*)(
    VoxelLightEnergy& target,
    const VoxelLightEnergy& source,
    VoxelLightState targetState,
    VoxelLightState sourceState);

using MergeVoxelLightDataFn = void (*)(
    VoxelLightData& target,
    const VoxelLightData& source);

using MakeEnergyFn = VoxelLightEnergy(*)(
    const Vec3& energy,
    const Vec3& direction);


class VoxelRayMarcher
{
private:
    struct RayState
    {
        Vec3 Origin;
        Vec3 Direction;
        Vec3 DirectionNormal;
        Vec3 Energy;
        Vec3 MaxEnergy;

        Vec3 OcclusionOrigin;
        Vec3 OcclusionEnergy;

        float Distance;

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

    template<
        LightTrackMode Mode,
        RayIntersectionMode IntersectMode,
        bool NormalMode,
        VoxelLightMergeMode RayMergeMode>
    bool Step();

    void ClearContribution();

    const VoxelLightContribution& Contribution() const { return _local.Contribution; }
    const RayState& Ray() const { return _ray; }

    std::vector<VoxelLightRay>& NextRays() { return _nextRays; }

private:
    using StepFn = bool (VoxelRayMarcher::*)();

    VoxelLightBaker* _baker;
    int32_t _workerIndex;
    StepFn _step;

    RayState _ray;
    ContributionMergeState _local;
    std::vector<VoxelLightRay> _nextRays;

private:

    StepFn SelectStep(
        LightTrackMode mode,
        RayIntersectionMode intersectMode,
        bool normalMode,
        VoxelLightMergeMode rayMergeMode);


    void TraceRay(
        const VoxelLightRay& ray,
        int32_t generation);

    bool MoveToNextVoxel();

public:

    bool StepImpl() { return (this->*_step)(); }
};

class VoxelLightBaker
{
    friend class VoxelRayMarcher;

private:



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

    void AccumulateLight(
        const VoxelLightContribution& contribution);

    void ReconstructDirectionSurfaceForFace(
        int32_t face);

    VoxelLightField& GetLightField();

    float GetVoxelSize() {
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

    MergeEnergyFn _mergeEnergyLight;

    MergeVoxelLightDataFn _mergeVoxelLightDataLight;
    MergeVoxelLightDataFn _mergeVoxelLightDataGen;
    MergeVoxelLightDataFn _mergeVoxelLightDataRay;

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
        MergeVoxelLightDataFn mergeVoxelLightData);

    void ClearMergeState(
        ContributionMergeState& mergeState);

    void BuildLightField();

    void BakeGeneratedRays(VoxelLightContribution& contribution);

};
