#pragma once

struct VoxelLightSample
{
	int32_t Index;
	Vec3 Direction;
	Vec3 Energy;
};

struct VoxelLightLookup
{
	uint32_t Offset;
	uint32_t Count;
};


struct alignas(16) VoxelLightGpuContribution
{
	Vec4 Direction;
	Vec4 Color;
};


struct VoxelLightContributionViewV2
{
	VoxelLightSample* Samples;
	int32_t SampleCount;
	int32_t SampleCapacity;
};

struct VoxelLightFieldViewV2
{
	Vec3I Size;

	VoxelLightLookup* Lookup;
	int32_t LookupCount;
	int32_t LookupCapacity;

	VoxelLightGpuContribution* Contributions;
	int32_t ContributionCount;
	int32_t ContributionCapacity;
};


struct VoxelLightBakeParamsV2 : VoxelLightBakeParams
{
	VoxelLightBakeParamsV2();

	float AngularTolerance;
	float RelativeEnergyTolerance;
};

struct VoxelLightContributionV2
{
	std::vector<VoxelLightSample> Samples;
};

struct VoxelLightFieldV2
{
	Vec3I Size;
	std::vector<VoxelLightLookup> Lookup;
	std::vector<VoxelLightGpuContribution> Contributions;
};

struct ContributionMergeStateV2
{
	VoxelLightContributionV2 Contribution;
	std::vector<int32_t> CellSlots;
	std::vector<int32_t> TouchedVoxels;
};



class VoxelLightBakerV2;

class VoxelRayMarcherV2
{
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
	using StepFn = bool (VoxelRayMarcherV2::*)();

	VoxelRayMarcherV2();

	void SetContext(VoxelLightBakerV2* baker, int32_t workerIndex);
	void Prepare(int32_t voxelCount);
	void TraceRay(const VoxelLightRay& ray, int32_t generation);
	void TraceRange(int32_t startRay, int32_t endRay, int32_t generation);
	void GetDebugState(VoxelRayDebugState& state) const;

	bool CreateRay(const VoxelLightRay& ray, int32_t generation);
	bool StepImpl() { return (this->*_step)(); }
	void ClearContribution();

	const RayState& Ray() const { return _ray; }
	const VoxelLightContributionV2& Contribution() const { return _local.Contribution; }
	std::vector<VoxelLightRay>& NextRays() { return _nextRays; }
	const std::vector<VoxelLightRay>& NextRays() const { return _nextRays; }

private:
	bool MoveToNextVoxel();
	StepFn SelectStep(LightTrackMode mode);

	template<LightTrackMode Mode>
	bool Step();

	VoxelLightBakerV2* _baker;
	int32_t _workerIndex;
	RayState _ray;
	ContributionMergeStateV2 _local;
	std::vector<VoxelLightRay> _nextRays;
	StepFn _step;
};


class VoxelLightBakerV2
{
	friend class VoxelRayMarcherV2;

public:
	VoxelLightBakerV2();
	VoxelLightBakerV2(const VoxelLightBakeParamsV2& params);

	void SetParams(const VoxelLightBakeParamsV2& params);
	void SetGrid(const VoxelGridDesc& grid);

	void ClearScene();
	void ClearLightField();

	void AddMesh(const Vec3I& origin, const Vec3I& size, const VoxelData* voxels, const VoxelMeshResolvedFace* faces, int32_t faceCount);
	void AddGpuMeshFaces(const GpuVoxelFaceData* faces, int32_t faceCount);

	void BakeAreaLight(const AreaLight& light, VoxelLightContributionV2& contribution);
	void BakePointLight(const PointLight& light, VoxelLightContributionV2& contribution);
	void BakeDirectionalLight(const DirectionalLight& light, VoxelLightContributionV2& contribution);
	void BakeSpotLight(const SpotLight& light, VoxelLightContributionV2& contribution);

	void AccumulateLight(const VoxelLightContributionV2& contribution);

	VoxelLightFieldV2& GetLightField();
	void BuildLightField(VoxelLightFieldV2& field, float angularTolerance, float relativeEnergyTolerance);

	std::vector<VoxelData>* GetScene() { return &_scene; }
	const std::vector<VoxelData>* GetScene() const { return &_scene; }
	int32_t GetVoxelCount() const { return _voxelCount; }

private:
	void BakeGeneratedRays(VoxelLightContributionV2& contribution);

	void PrefillAreaLightContribution(const AreaLight& light, VoxelLightContributionV2& contribution);
	void PrefillPointLightContribution(const PointLight& light, VoxelLightContributionV2& contribution);
	void PrefillDirectionalLightContribution(const DirectionalLight& light, VoxelLightContributionV2& contribution);
	void PrefillSpotLightContribution(const SpotLight& light, VoxelLightContributionV2& contribution);

	void GenerateAreaLightRays(const AreaLight& light);
	void GeneratePointLightRays(const PointLight& light, bool fillMode);
	void GenerateDirectionalLightRays(const DirectionalLight& light);
	void GenerateSpotLightRays(const SpotLight& light);

	void CleanupUnvisitedFaces(VoxelLightContributionV2& contribution);

	void TraceRays(VoxelLightContributionV2& contribution, std::vector<VoxelLightRay>& nextRays, int32_t generation);
	void MergeContribution(VoxelLightContributionV2& target, ContributionMergeStateV2& mergeState, const VoxelLightContributionV2& source);
	void ClearMergeState(ContributionMergeStateV2& mergeState);
	void BuildLightField();

	VoxelLightBakeParamsV2 _params;
	VoxelGridDesc _grid;
	int32_t _voxelCount;

	std::vector<VoxelData> _scene;
	std::vector<VoxelLightRay> _rays;
	std::vector<VoxelLightRay> _nextRays;
	std::vector<VoxelRayMarcherV2> _marchers;

	ContributionMergeStateV2 _currentMerge;

	std::vector<VoxelLightSample> _lightSamples;
	bool _lightSamplesSorted;
	VoxelLightFieldV2 _field;
};



