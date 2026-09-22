#include "pch.h"
#include "VoxelLightBakerV2.h"
#include <bit>

namespace {

	constexpr float Pi = 3.14159265358979323846f;
	constexpr float Epsilon = 1e-5f;

	static constexpr Vec3 FaceNormals[VOXEL_LIGHT_FACE_COUNT] =
	{
		{ -1.0f,  0.0f,  0.0f },
		{ 1.0f,  0.0f,  0.0f },
		{ 0.0f, -1.0f,  0.0f },
		{ 0.0f,  1.0f,  0.0f },
		{ 0.0f,  0.0f, -1.0f },
		{ 0.0f,  0.0f,  1.0f }
	};

	FORCE_INLINE bool HasEnergy(const Vec3& energy, float threshold)
	{
		return energy.X > threshold ||
			energy.Y > threshold ||
			energy.Z > threshold;
	}


	FORCE_INLINE Vec3 FaceNormal(int32_t face)
	{
		return FaceNormals[face];
	}


	FORCE_INLINE int32_t VoxelIndex(const VoxelGridDesc& grid, Vec3I cell)
	{
		return (cell.Z * grid.Size.Y + cell.Y) * grid.Size.X + cell.X;
	}

	FORCE_INLINE uint32_t Hash(uint32_t value)
	{
		value ^= value >> 16;
		value *= 0x7feb352du;
		value ^= value >> 15;
		value *= 0x846ca68bu;
		value ^= value >> 16;
		return value;
	}

	FORCE_INLINE float HashFloat(uint32_t value)
	{
		return float(Hash(value) & 0x00FFFFFFu) *
			(1.0f / 16777216.0f);
	}


	Vec3 VoxelCenter(const VoxelGridDesc& grid, Vec3I cell)
	{
		float size = grid.VoxelSize;

		return {
			grid.Origin.X + (float(cell.X) + 0.5f) * size,
			grid.Origin.Y + (float(cell.Y) + 0.5f) * size,
			grid.Origin.Z + (float(cell.Z) + 0.5f) * size
		};
	}

	Vec3I VoxelCell(const VoxelGridDesc& grid, int32_t index)
	{
		Vec3I cell{};

		cell.X = index % grid.Size.X;
		index /= grid.Size.X;

		cell.Y = index % grid.Size.Y;
		index /= grid.Size.Y;

		cell.Z = index;

		return cell;
	}



	FORCE_INLINE bool IsInsideGrid(const VoxelGridDesc& grid, Vec3I cell)
	{
		return cell.X >= 0 &&
			cell.Y >= 0 &&
			cell.Z >= 0 &&
			cell.X < grid.Size.X &&
			cell.Y < grid.Size.Y &&
			cell.Z < grid.Size.Z;
	}

	bool WorldToVoxel(
		const VoxelGridDesc& grid,
		const Vec3& p,
		Vec3I& cell)
	{
		float invSize = 1.0f / grid.VoxelSize;

		cell.X = int32_t(std::floor((p.X - grid.Origin.X) * invSize));
		cell.Y = int32_t(std::floor((p.Y - grid.Origin.Y) * invSize));
		cell.Z = int32_t(std::floor((p.Z - grid.Origin.Z) * invSize));

		return IsInsideGrid(grid, cell);
	}


	float LightCurveAtT(const LightCurve& falloff, float t)
	{
		if (t <= 0.0f)
			return 0.0f;

		if (t >= 1.0f)
			t = 1.0f;

		float factor = falloff.Factor;

		if (factor <= Epsilon)
			factor = 1.0f;

		switch (falloff.Type)
		{
		case LightCurveType::Linear:
			return t * factor;

		case LightCurveType::Quadratic:
			return t * t * factor;

		default:
			return factor;
		}
	}

	float LightFalloffAtDistance(const LightCurve& falloff, float distance)
	{
		if (falloff.Type == LightCurveType::None)
			return 1.0f;

		if (falloff.Range <= Epsilon)
			return 0.0f;

		return LightCurveAtT(falloff, 1.0f - distance / falloff.Range);
	}

	float LightRecoveryAtDistance(const LightCurve& recovery, float distance)
	{
		if (recovery.Type == LightCurveType::None)
			return 1.0f;

		if (recovery.Range <= Epsilon)
			return 0.0f;

		return LightCurveAtT(recovery, distance / recovery.Range);
	}

	float SpotConeAttenuation(
		const Vec3& lightDirection,
		float innerCos,
		float outerCos,
		const Vec3& rayDirection)
	{
		Vec3 axis = lightDirection.Normalized();
		Vec3 dir = rayDirection.Normalized();

		if (Dot(axis, axis) <= Epsilon || Dot(dir, dir) <= Epsilon)
			return 0.0f;

		if (innerCos < outerCos)
			std::swap(innerCos, outerCos);

		float c = Dot(axis, dir);

		if (c <= outerCos)
			return 0.0f;

		if (c >= innerCos || innerCos - outerCos <= Epsilon)
			return 1.0f;

		return std::clamp((c - outerCos) / (innerCos - outerCos), 0.0f, 1.0f);
	}

	FORCE_INLINE Vec3 DirectionalLightEnergy(const DirectionalLight& light)
	{
		return light.Color * light.Intensity;
	}

	void DirectionBasis(
		const Vec3& direction,
		Vec3& right,
		Vec3& up)
	{
		Vec3 axis = direction.Normalized();

		Vec3 ref = std::fabs(axis.Y) < 0.9f
			? Vec3{ 0.0f, 1.0f, 0.0f }
		: Vec3{ 1.0f, 0.0f, 0.0f };

		right = Cross(ref, axis).Normalized();

		if (Dot(right, right) <= Epsilon)
			right = { 1.0f, 0.0f, 0.0f };

		up = Cross(axis, right).Normalized();
	}

	FORCE_INLINE float EnergyScore(const Vec3& energy)
	{
		return energy.X + energy.Y + energy.Z;
	}


	struct VoxelLightBuildCluster
	{
		Vec3 DirectionSum;
		Vec3 Direction;
		Vec3 Color;
		float Weight;

		// Radius as sin/cos avoids acos in the clustering hot path.
		float RadiusCos;
		float RadiusSin;
	};

	struct VoxelLightClusterMerge
	{
		Vec3 DirectionSum;
		Vec3 Direction;
		float RadiusCos;
		float RadiusSin;
	};

	FORCE_INLINE float ContributionWeight(const Vec3& color)
	{
		return std::max(EnergyScore(color), Epsilon);
	}

	void InitCluster(VoxelLightBuildCluster& cluster, const VoxelLightContributionSampleV2& sample)
	{
		const float weight = ContributionWeight(sample.Energy);

		cluster.DirectionSum = sample.Direction * weight;
		cluster.Direction = sample.Direction;
		cluster.Color = sample.Energy;
		cluster.Weight = weight;
		cluster.RadiusCos = 1.0f;
		cluster.RadiusSin = 0.0f;
	}

	FORCE_INLINE bool TryMergeCluster(const VoxelLightBuildCluster& cluster, const VoxelLightContributionSampleV2& sample,
		float sampleWeight, float toleranceCos, float toleranceSin, float doubleToleranceCos,
		bool useDoubleToleranceReject, VoxelLightClusterMerge& result)
	{
		// A representative/sample separation above 2*tolerance cannot merge.
		if (useDoubleToleranceReject && Dot(cluster.Direction, sample.Direction) < doubleToleranceCos)
		{
			return false;
		}

		const Vec3 directionSum = cluster.DirectionSum + sample.Direction * sampleWeight;

		// Use the weighted mean so cancellation is independent of brightness.
		const Vec3 meanDirection = directionSum / (cluster.Weight + sampleWeight);
		const float lengthSq = Dot(meanDirection, meanDirection);

		if (lengthSq <= Epsilon * Epsilon)
			return false;

		const float invLength = 1.0f / std::sqrt(lengthSq);
		const Vec3 direction = meanDirection * invLength;
		const float shiftCos = std::clamp(Dot(cluster.Direction, direction), -1.0f, 1.0f);
		const float sampleCos = std::clamp(Dot(sample.Direction, direction), -1.0f, 1.0f);

		if (sampleCos < toleranceCos)
			return false;

		// shift <= tolerance - currentRadius
		const float remainingCos = toleranceCos * cluster.RadiusCos + toleranceSin * cluster.RadiusSin;

		if (shiftCos < remainingCos)
			return false;

		const float shiftSin = std::sqrt(std::max(0.0f, 1.0f - shiftCos * shiftCos));
		const float expandedRadiusCos = cluster.RadiusCos * shiftCos - cluster.RadiusSin * shiftSin;
		const float expandedRadiusSin = cluster.RadiusSin * shiftCos + cluster.RadiusCos * shiftSin;

		result.DirectionSum = directionSum;
		result.Direction = direction;

		// max(radius + shift, sampleAngle) => smaller cosine.
		if (expandedRadiusCos <= sampleCos)
		{
			result.RadiusCos = std::clamp(expandedRadiusCos, -1.0f, 1.0f);
			result.RadiusSin = std::clamp(expandedRadiusSin, 0.0f, 1.0f);
		}
		else
		{
			result.RadiusCos = sampleCos;
			result.RadiusSin = std::sqrt(std::max(0.0f, 1.0f - sampleCos * sampleCos));
		}

		return true;
	}

	FORCE_INLINE void CommitClusterMerge(VoxelLightBuildCluster& cluster, const VoxelLightContributionSampleV2& sample,
		float sampleWeight, const VoxelLightClusterMerge& merge)
	{
		cluster.DirectionSum = merge.DirectionSum;
		cluster.Direction = merge.Direction;
		cluster.Color += sample.Energy;
		cluster.Weight += sampleWeight;
		cluster.RadiusCos = merge.RadiusCos;
		cluster.RadiusSin = merge.RadiusSin;
	}

	FORCE_INLINE void MergeClusterUnbounded(VoxelLightBuildCluster& cluster, const VoxelLightContributionSampleV2& sample)
	{
		const float weight = ContributionWeight(sample.Energy);
		const Vec3 directionSum = cluster.DirectionSum + sample.Direction * weight;
		const Vec3 meanDirection = directionSum / (cluster.Weight + weight);
		const float lengthSq = Dot(meanDirection, meanDirection);

		// Keep the real accumulated sum even when directions cancel out.
		cluster.DirectionSum = directionSum;

		if (lengthSq > Epsilon * Epsilon)
		{
			cluster.Direction = meanDirection * (1.0f / std::sqrt(lengthSq));
		}

		cluster.Color += sample.Energy;
		cluster.Weight += weight;

		// Radius is unused after the strong-sample pass.
	}

	FORCE_INLINE bool SelectVoxelHitFace(
		const VoxelData& voxelData,
		const Vec3& rayDirection,
		int32_t originStep,
		int32_t& hitFace)
	{
		hitFace = -1;

		float bestScore = 0.0f;

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			const VoxelFaceData& faceData = voxelData.Faces[face];

			if (faceData.Side == VoxelTriangleSide::None)
				continue;

			if (originStep <= 1 && faceData.Side == VoxelTriangleSide::Back)
				continue;

			Vec3 normal = faceData.Normal;

			float score = Dot(-rayDirection, normal);

			//We need unfortunately hit even with unrelated faces
			/*
			if (score <= 0.0f)
				continue;
			*/

			if (hitFace < 0 || score > bestScore)
			{
				bestScore = score;
				hitFace = face;
			}
		}

		return hitFace >= 0;
	}

	int32_t IncomingBucketFaceFromDirection(const Vec3& dir)
	{
		int32_t bestFace = 0;
		float bestDot = Dot(dir, FaceNormal(0));

		for (int32_t face = 1; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			float d = Dot(dir, FaceNormal(face));

			if (d < bestDot)
			{
				bestDot = d;
				bestFace = face;
			}
		}

		return bestFace;
	}

	int32_t OutgoingBucketFaceFromDirection(const Vec3& dir)
	{
		return IncomingBucketFaceFromDirection(dir) ^ 1;
	}


	FORCE_INLINE bool RayAabbInterval(
		const Vec3& boxMin,
		const Vec3& boxMax,
		const Vec3& origin,
		const Vec3& direction,
		float& enterT,
		float& exitT)
	{
		const float minAxis[3] = { boxMin.X, boxMin.Y, boxMin.Z };
		const float maxAxis[3] = { boxMax.X, boxMax.Y, boxMax.Z };
		const float originAxis[3] = { origin.X, origin.Y, origin.Z };
		const float dirAxis[3] = { direction.X, direction.Y, direction.Z };

		enterT = -FLT_MAX;
		exitT = FLT_MAX;

		for (int32_t axis = 0; axis < 3; ++axis)
		{
			const float o = originAxis[axis];
			const float d = dirAxis[axis];

			if (std::fabs(d) <= Epsilon)
			{
				if (o < minAxis[axis] || o > maxAxis[axis])
					return false;

				continue;
			}

			const float inv = 1.0f / d;
			float t0 = (minAxis[axis] - o) * inv;
			float t1 = (maxAxis[axis] - o) * inv;

			if (t0 > t1)
				std::swap(t0, t1);

			enterT = std::max(enterT, t0);
			exitT = std::min(exitT, t1);

			if (enterT > exitT)
				return false;
		}

		return true;
	}

	Vec3 RayGridExitPoint(
		const VoxelGridDesc& grid,
		const Vec3& origin,
		const Vec3& direction)
	{
		Vec3 gridMax{
			grid.Origin.X + float(grid.Size.X) * grid.VoxelSize,
			grid.Origin.Y + float(grid.Size.Y) * grid.VoxelSize,
			grid.Origin.Z + float(grid.Size.Z) * grid.VoxelSize
		};

		float enterT;
		float exitT;

		if (!RayAabbInterval(grid.Origin, gridMax, origin, direction, enterT, exitT) ||
			exitT <= Epsilon)
			return { 0 };

		return origin + direction * exitT;
	}



	bool RayVoxelSurfacePoint(
		const Vec3& voxelCenter,
		float voxelSize,
		const Vec3& origin,
		const Vec3& direction,
		Vec3& point)
	{
		float half = voxelSize * 0.5f;

		Vec3 boxMin{
			voxelCenter.X - half,
			voxelCenter.Y - half,
			voxelCenter.Z - half
		};

		Vec3 boxMax{
			voxelCenter.X + half,
			voxelCenter.Y + half,
			voxelCenter.Z + half
		};

		float enterT;
		float exitT;

		if (!RayAabbInterval(boxMin, boxMax, origin, direction, enterT, exitT))
			return false;

		float t = enterT >= 0.0f ? enterT : exitT;

		if (t < 0.0f)
			return false;

		point = origin + direction * t;
		return true;
	}


	Vec3 SurfaceBounceEnergy(
		const Vec3& incomingEnergy,
		const VoxelFaceData& face)
	{
		Vec3 albedo{
			face.BaseColor.X,
			face.BaseColor.Y,
			face.BaseColor.Z
		};

		float roughness = std::clamp(face.Roughness, 0.0f, 1.0f);
		float metallic = std::clamp(face.Metallic, 0.0f, 1.0f);

		Vec3 diffuse = incomingEnergy * albedo;
		Vec3 metal = incomingEnergy;

		return Lerp(diffuse, metal, metallic) * roughness;
	}


	int32_t BounceRayCountForGeneration(
		int32_t generation,
		const VoxelLightBakeParams& params)
	{
		if (params.Bounce.RayCount <= 1)
			return 1;

		int32_t baseCount = std::max(1, params.Bounce.RayCount);
		float decay = std::clamp(params.Bounce.RayDecay, 0.0f, 1.0f);
		float scaled = float(baseCount) * std::pow(decay, float(generation));

		return std::max(1, int32_t(std::round(scaled)));
	}

	Vec3 ConeDirection(
		const Vec3& center,
		float maxAngle,
		uint32_t seed,
		int32_t index,
		int32_t count)
	{
		if (count <= 1 || maxAngle <= Epsilon)
			return center;

		Vec3 ref = std::fabs(center.Y) < 0.9f
			? Vec3{ 0.0f, 1.0f, 0.0f }
		: Vec3{ 1.0f, 0.0f, 0.0f };

		Vec3 tangent = Cross(ref, center).Normalized();

		if (Dot(tangent, tangent) <= Epsilon)
			return center;

		Vec3 bitangent = Cross(center, tangent).Normalized();

		float radialJitter = HashFloat(
			seed ^ uint32_t(index) * 0x9e3779b9u);

		float azimuthJitter = HashFloat(
			seed ^ uint32_t(index) * 0x85ebca6bu);

		// One sample in each equal-solid-angle radial stratum.
		float u =
			(float(index) + radialJitter) /
			float(count);

		float cosTheta =
			1.0f - u * (1.0f - std::cos(maxAngle));

		float sinTheta =
			std::sqrt(std::max(
				0.0f,
				1.0f - cosTheta * cosTheta));

		float azimuth =
			2.0f * Pi * azimuthJitter;

		Vec3 radial =
			tangent * std::cos(azimuth) +
			bitangent * std::sin(azimuth);

		return (
			center * cosTheta +
			radial * sinTheta).Normalized();
	}





}

VoxelLightBakeParamsV2::VoxelLightBakeParamsV2()
{
	AngularTolerance = 10.0f * Pi / 180.0f;
	RelativeEnergyTolerance = 0.01f;
}

VoxelRayMarcherV2::VoxelRayMarcherV2() {
	_baker = nullptr; _workerIndex = -1;

	_ray = {};
	_ray.LastHitVoxel = -1;
	_ray.BounceCount = 0;
	_step = nullptr;
}

void VoxelRayMarcherV2::SetContext(VoxelLightBakerV2* baker, int32_t workerIndex)
{
	_baker = baker;
	_workerIndex = workerIndex;

	const VoxelLightBakeParams& params = baker->_params;

	_step = SelectStep(params.Mode);
}

void VoxelRayMarcherV2::Prepare(int32_t voxelCount)
{
	_local.Contribution.Samples.clear();
	_local.TouchedVoxels.clear();
	_local.Contribution.Samples.reserve(voxelCount);
	_ray.IsAlive = true;
}

bool VoxelRayMarcherV2::CreateRay(const VoxelLightRay& ray, int32_t generation)
{
	_ray.Origin = ray.Position;
	_ray.Position = ray.Position;
	_ray.Direction = ray.Direction;
	_ray.DirectionNormal = ray.DirectionNormal;
	_ray.Energy = ray.Energy;
	_ray.OcclusionEnergy = ray.Energy;
	_ray.Distance = 0.0f;
	_ray.Falloff = ray.Falloff;
	_ray.Recovery = ray.Recovery;
	_ray.OriginStep = 0;
	_ray.LightState = VoxelLightState::Empty;
	_ray.Cell = { 0 };
	_ray.LastHitVoxel = -1;
	_ray.LastAffectedVoxel = -1;
	_ray.LastAffectedFace = -1;
	_ray.BounceCount = generation;
	_ray.IsAlive = true;
	_ray.TravelDistance = 0.0;
	_ray.OcclusionDistance = 0.0;

	const VoxelGridDesc& grid = _baker->_grid;
	const float origins[3] = { ray.Position.X, ray.Position.Y, ray.Position.Z };
	const float directions[3] = { ray.Direction.X, ray.Direction.Y, ray.Direction.Z };
	const float gridOrigins[3] = { grid.Origin.X, grid.Origin.Y, grid.Origin.Z };
	const int32_t sizes[3] = { grid.Size.X, grid.Size.Y, grid.Size.Z };

	if (!(grid.VoxelSize > 0.0f) || !std::isfinite(grid.VoxelSize) || Dot(ray.Direction, ray.Direction) == 0.0f)
		return _ray.IsAlive = false;

	double enter = 0.0;
	double exit = INFINITY;

	for (int axis = 0; axis < 3; ++axis)
	{
		if (sizes[axis] <= 0 || !std::isfinite(origins[axis]) ||
			!std::isfinite(directions[axis]) || !std::isfinite(gridOrigins[axis]))
			return _ray.IsAlive = false;

		const double low = gridOrigins[axis];
		const double high = low + double(sizes[axis]) * grid.VoxelSize;

		_ray.InverseDirection[axis] = directions[axis] == 0.0f ? 0.0 : 1.0 / double(directions[axis]);

		if (directions[axis] == 0.0f)
		{
			if (origins[axis] < low || origins[axis] >= high)
				return _ray.IsAlive = false;

			continue;
		}

		double t0 = (low - origins[axis]) * _ray.InverseDirection[axis];
		double t1 = (high - origins[axis]) * _ray.InverseDirection[axis];

		if (t0 > t1)
			std::swap(t0, t1);

		enter = std::max(enter, t0);
		exit = std::min(exit, t1);
	}

	if (enter >= exit)
		return _ray.IsAlive = false;

	int32_t cells[3];
	bool atOrigin = enter == 0.0;

	for (int axis = 0; axis < 3; ++axis)
	{
		const double coordinate =
			(double(origins[axis]) + double(directions[axis]) * enter - gridOrigins[axis]) / grid.VoxelSize;

		double cell = std::floor(coordinate);

		// On a boundary, use the cell on the outgoing side.
		if (coordinate == cell && directions[axis] != 0.0f)
		{
			atOrigin = false;

			if (directions[axis] < 0.0f)
				--cell;
		}

		cells[axis] = int32_t(std::clamp(cell, 0.0, double(sizes[axis] - 1)));
	}

	_ray.Cell = { cells[0], cells[1], cells[2] };

	return SetCellInterval(enter, atOrigin);
}

void VoxelRayMarcherV2::TraceRay(const VoxelLightRay& ray, int32_t generation) {

	if (!CreateRay(ray, generation))
		return;

	while (StepImpl()) {}
}

void VoxelRayMarcherV2::TraceRange(int32_t startRay, int32_t endRay, int32_t generation) {

	ClearContribution();

	_local.Contribution.Samples.reserve(
		std::min(_baker->_voxelCount, (endRay - startRay) * 8));

	_nextRays.clear();

	for (int32_t i = startRay; i < endRay; ++i)
		TraceRay(_baker->_rays[i], generation);
}

void VoxelRayMarcherV2::GetDebugState(
	VoxelRayDebugState& state) const
{
	if (_ray.LightState == VoxelLightState::Occlusion)
	{
		float recovery = LightRecoveryAtDistance(_ray.Recovery, _ray.Distance);
		state.Energy = _ray.MaxEnergy * recovery;
	}
	else
	{
		float falloff = LightFalloffAtDistance(
			_ray.Falloff,
			_ray.Distance);

		state.Energy = _ray.Energy * falloff;
	}

	state.Origin = _ray.Origin;
	state.Position = _ray.Position;
	state.Direction = _ray.Direction;

	state.Distance = _ray.Distance;

	state.Cell = _ray.Cell;

	state.LastHitVoxel = _ray.LastHitVoxel;
	state.LastAffectedVoxel = _ray.LastAffectedVoxel;
	state.LastAffectedFace = _ray.LastAffectedFace;

	state.BounceCount = _ray.BounceCount;

	state.LastVoxel = {};
	state.LastLightData = {};

	if (_ray.LastAffectedVoxel < 0 || _ray.LastAffectedVoxel >= _baker->_voxelCount)
		return;

	state.LastVoxel = _baker->_scene[_ray.LastAffectedVoxel];
}

void VoxelRayMarcherV2::GetContribution(VoxelLightContributionV2& contribution) const
{
	if (_baker == nullptr)
	{
		contribution.Cells.clear();
		contribution.Samples.clear();
		return;
	}

	_baker->FinalizeContribution(_local.Contribution, contribution);
}


bool VoxelRayMarcherV2::SetCellInterval(double entryDistance, bool atOrigin)
{
	const VoxelGridDesc& grid = _baker->_grid;
	const float origins[3] = { _ray.Origin.X, _ray.Origin.Y, _ray.Origin.Z };
	const float gridOrigins[3] = { grid.Origin.X, grid.Origin.Y, grid.Origin.Z };
	const float directions[3] = { _ray.Direction.X, _ray.Direction.Y, _ray.Direction.Z };

	while (IsInsideGrid(grid, _ray.Cell))
	{
		const int32_t cells[3] = { _ray.Cell.X, _ray.Cell.Y, _ray.Cell.Z };

		for (int axis = 0; axis < 3; ++axis)
		{
			const double boundary =
				double(gridOrigins[axis]) +
				(double(cells[axis]) + (directions[axis] > 0.0f ? 1.0 : 0.0)) * grid.VoxelSize;

			_ray.CellExit[axis] = directions[axis] == 0.0f
				? INFINITY
				: (boundary - origins[axis]) * _ray.InverseDirection[axis];
		}

		const double exitDistance = std::min({ _ray.CellExit[0], _ray.CellExit[1], _ray.CellExit[2] });

		if (exitDistance > entryDistance)
		{
			// Sample inside the interval rather than on a voxel boundary.
			const float distance =
				atOrigin ? 0.0f : float(entryDistance + (exitDistance - entryDistance) * 0.5);

			const Vec3 position = _ray.Origin + _ray.Direction * distance;
			Vec3I evaluatedCell;

			if (WorldToVoxel(grid, position, evaluatedCell) &&
				evaluatedCell.X == _ray.Cell.X &&
				evaluatedCell.Y == _ray.Cell.Y &&
				evaluatedCell.Z == _ray.Cell.Z)
			{
				_ray.TravelDistance = distance;
				_ray.Position = position;
				_ray.Distance = float(_ray.TravelDistance - _ray.OcclusionDistance);
				return true;
			}

			if (atOrigin)
			{
				atOrigin = false;
				continue;
			}
		}

		// Skip grazing intervals that cannot be represented reliably in float world coordinates.
		if (_ray.CellExit[0] == exitDistance)
			_ray.Cell.X += directions[0] > 0.0f ? 1 : -1;

		if (_ray.CellExit[1] == exitDistance)
			_ray.Cell.Y += directions[1] > 0.0f ? 1 : -1;

		if (_ray.CellExit[2] == exitDistance)
			_ray.Cell.Z += directions[2] > 0.0f ? 1 : -1;

		entryDistance = exitDistance;
		atOrigin = false;
	}

	return _ray.IsAlive = false;
}

FORCE_INLINE bool VoxelRayMarcherV2::MoveToNextVoxel()
{
	const double crossing = std::min({ _ray.CellExit[0], _ray.CellExit[1], _ray.CellExit[2] });

	if (_ray.CellExit[0] == crossing)
		_ray.Cell.X += _ray.Direction.X > 0.0f ? 1 : -1;

	if (_ray.CellExit[1] == crossing)
		_ray.Cell.Y += _ray.Direction.Y > 0.0f ? 1 : -1;

	if (_ray.CellExit[2] == crossing)
		_ray.Cell.Z += _ray.Direction.Z > 0.0f ? 1 : -1;

	if (!IsInsideGrid(_baker->_grid, _ray.Cell))
		return _ray.IsAlive = false;

	++_ray.OriginStep;

	return SetCellInterval(crossing, false);
}

VoxelRayMarcherV2::StepFn VoxelRayMarcherV2::SelectStep(LightTrackMode mode)
{
	switch (mode)
	{
	case LightTrackMode::Full:
		return &VoxelRayMarcherV2::Step<LightTrackMode::Full>;

	case LightTrackMode::Occlusions:
		return &VoxelRayMarcherV2::Step<LightTrackMode::Occlusions>;

	case LightTrackMode::OcclusionsOnly:
		return &VoxelRayMarcherV2::Step<LightTrackMode::OcclusionsOnly>;
	}

	return nullptr;
}

template<LightTrackMode Mode>
bool VoxelRayMarcherV2::Step()
{
	if (!_ray.IsAlive)
		return false;

	const VoxelGridDesc& grid = _baker->_grid;
	const VoxelLightBakeParams& params = _baker->_params;

	const float energyThreshold = params.EnergyThreshold;

	const int32_t maxBounceCount = params.Bounce.MaxCount;
	const float bounceNormalWeightParam = params.Bounce.NormalWeight;
	const float bounceCenterWeightParam = params.Bounce.CenterWeight;
	const float bounceConeMaxAngle = params.Bounce.ConeMaxAngle;

	_ray.LastAffectedVoxel = -1;
	_ray.LastAffectedFace = -1;

	const float falloff = LightFalloffAtDistance(
		_ray.Falloff,
		_ray.Distance);

	Vec3 stepEnergy = _ray.Energy * falloff;

	if (!HasEnergy(stepEnergy, energyThreshold))
		_ray.IsAlive = false;
	else
		_ray.IsAlive = IsInsideGrid(grid, _ray.Cell);

	if (!_ray.IsAlive)
		return false;

	const int32_t index = VoxelIndex(grid, _ray.Cell);

	if (Mode != LightTrackMode::OcclusionsOnly ||
		_ray.LightState == VoxelLightState::Occlusion)
	{
		_ray.LastAffectedVoxel = index;
	}

	const VoxelData& voxelData = _baker->_scene[index];

	int32_t hitFace;

	const bool hasHit = SelectVoxelHitFace(
		voxelData,
		_ray.Direction,
		_ray.OriginStep,
		hitFace);

	if (hasHit)
	{
		_ray.LastHitVoxel = index;
		_ray.LastAffectedFace = hitFace;

		if (Mode != LightTrackMode::Full)
		{
			const Vec3 exitPoint = RayGridExitPoint(
				grid,
				_ray.Origin,
				_ray.Direction);

			const float exitDistance =
				(exitPoint - _ray.Origin).Length();

			const float exitFalloff = LightFalloffAtDistance(
				_ray.Falloff,
				exitDistance);

			const Vec3 exitEnergy = _ray.Energy * exitFalloff;

			_ray.LightState = VoxelLightState::Occlusion;
			_ray.MaxEnergy = Min(_ray.OcclusionEnergy, exitEnergy);

			_ray.Distance = 0.0f;
			_ray.OcclusionDistance = _ray.TravelDistance;
			_ray.OcclusionOrigin = _ray.Position;
			_ray.OcclusionEnergy = stepEnergy;
		}
		else
		{
			const VoxelFaceData& faceData = voxelData.Faces[hitFace];
			const int32_t nextGeneration = _ray.BounceCount + 1;

			if (nextGeneration < maxBounceCount)
			{
				const Vec3 normal = faceData.Normal;
				const Vec3 reflectDir =
					Reflect(_ray.Direction, normal).Normalized();

				const float roughness =
					std::clamp(faceData.Roughness, 0.0f, 1.0f);

				const float metallic =
					std::clamp(faceData.Metallic, 0.0f, 1.0f);

				const float normalWeight = std::clamp(
					bounceNormalWeightParam * (1.0f - metallic),
					0.0f,
					1.0f);

				Vec3 bounceDir =
					Lerp(reflectDir, normal, normalWeight).Normalized();

				if (Dot(bounceDir, bounceDir) <= Epsilon)
					bounceDir = reflectDir;

				Vec3 bounceOrigin;

				if (!RayVoxelSurfacePoint(VoxelCenter(grid, _ray.Cell), grid.VoxelSize, _ray.Origin, _ray.Direction, bounceOrigin))
				{
					bounceOrigin = _ray.Position;
				}

				_ray.Position = bounceOrigin;
				_ray.Distance = (bounceOrigin - _ray.Origin).Length();
				_ray.TravelDistance = _ray.Distance;
				stepEnergy = _ray.Energy * LightFalloffAtDistance(_ray.Falloff, _ray.Distance);

				const Vec3 bounceEnergy =
					SurfaceBounceEnergy(stepEnergy, faceData);

				const VoxelLightBakeParams localParams = _baker->_params;

				const int32_t rayCount = std::max(
					1,
					BounceRayCountForGeneration(
						_ray.BounceCount,
						localParams));

				const float centerWeight = rayCount > 1
					? std::clamp(
						bounceCenterWeightParam,
						0.0f,
						1.0f)
					: 1.0f;

				const float coneAngle =
					bounceConeMaxAngle * roughness;

				auto pushBounceRay =
					[&](const Vec3& direction, const Vec3& energy)
					{
						if (!HasEnergy(energy, energyThreshold))
							return;

						if (Dot(direction, direction) <= Epsilon)
							return;

						VoxelLightRay ray{};

						ray.Position = bounceOrigin;
						ray.Direction = direction;
						ray.DirectionNormal = normal;
						ray.Energy = energy;
						ray.Falloff = _ray.Falloff;

						_nextRays.push_back(ray);
					};

				pushBounceRay(
					bounceDir,
					bounceEnergy * centerWeight);

				const int32_t sideCount = rayCount - 1;

				if (sideCount > 0)
				{
					const Vec3 sideEnergy =
						bounceEnergy *
						((1.0f - centerWeight) / float(sideCount));

					uint32_t coneSeed = Hash(
						std::bit_cast<uint32_t>(bounceOrigin.X) * 73856093u ^
						std::bit_cast<uint32_t>(bounceOrigin.Y) * 19349663u ^
						std::bit_cast<uint32_t>(bounceOrigin.Z) * 83492791u ^
						uint32_t(_ray.BounceCount) * 2654435761u);

					for (int32_t i = 0; i < sideCount; ++i)
					{
						const Vec3 sideDir = ConeDirection(
							bounceDir,
							coneAngle,
							coneSeed,
							i,
							sideCount);

						pushBounceRay(sideDir, sideEnergy);
					}
				}
			}

			_ray.IsAlive = false;
		}
	}
	else
	{
		_ray.LastAffectedFace =
			OutgoingBucketFaceFromDirection(_ray.Direction);

		bool writeEnergy = true;

		if (Mode != LightTrackMode::Full)
		{
			if (_ray.LightState == VoxelLightState::Occlusion)
			{
				const float recovery = LightRecoveryAtDistance(
					_ray.Recovery,
					_ray.Distance);

				stepEnergy = _ray.MaxEnergy * recovery;
			}
			else
			{
				writeEnergy = Mode == LightTrackMode::Occlusions;
			}
		}

		if (writeEnergy &&
			(_ray.BounceCount > 0 || !params.InitiateLightField))
		{
			_local.Contribution.Samples.push_back({
				index,
				_ray.Direction,
				stepEnergy
				});
		}
	}

	if (_ray.IsAlive)
		MoveToNextVoxel();

	return _ray.IsAlive;
}

void VoxelRayMarcherV2::ClearContribution() {

	_local.Contribution.Samples.clear();
	_local.TouchedVoxels.clear();

}

VoxelLightBakerV2::VoxelLightBakerV2()
	: VoxelLightBakerV2(VoxelLightBakeParamsV2())
{
}

VoxelLightBakerV2::VoxelLightBakerV2(const VoxelLightBakeParamsV2& params)
{
	_grid = {};
	_voxelCount = 0;

	SetParams(params);
}

void VoxelLightBakerV2::SetParams(const VoxelLightBakeParamsV2& params)
{
	_params = params;
}

void VoxelLightBakerV2::SetGrid(const VoxelGridDesc& grid)
{
	_grid = grid;
	_voxelCount = grid.Size.X * grid.Size.Y * grid.Size.Z;

	_scene.assign(_voxelCount, VoxelData{});

	_lightSamples.clear();
	_lightRanges.clear();
	_voxelRangeHeads.assign(_voxelCount, -1);

	_contributionCounts.assign(_voxelCount, 0);
	_contributionOffsets.assign(size_t(_voxelCount) + 1, 0);
	_contributionCursor.assign(_voxelCount, 0);

	_currentMerge.CellSlots.assign(_voxelCount, -1);
	_currentMerge.TouchedVoxels.clear();

	_currentMerge.TouchedVoxels.reserve(_voxelCount);



}


void VoxelLightBakerV2::ClearScene()
{
	std::fill(_scene.begin(), _scene.end(), VoxelData{});
}

void VoxelLightBakerV2::AddMesh(const Vec3I& origin, const Vec3I& size, const VoxelData* voxels, const VoxelMeshResolvedFace* faces, int32_t faceCount) {

	Vec3I dst = {};

	for (int32_t z = 0; z < size.Z; ++z) {
		dst.Z = origin.Z + z;

		if (dst.Z < 0 || dst.Z >= _grid.Size.Z)
			continue;

		for (int32_t y = 0; y < size.Y; ++y)
		{
			dst.Y = origin.Y + y;

			if (dst.Y < 0 || dst.Y >= _grid.Size.Y)
				continue;

			for (int32_t x = 0; x < size.X; ++x)
			{
				dst.X = origin.X + x;

				if (dst.X < 0 || dst.X >= _grid.Size.X)
					continue;

				int32_t srcIndex = (z * size.Y + y) * size.X + x;
				int32_t dstIndex = VoxelIndex(_grid, dst);

				const VoxelData& src = voxels[srcIndex];

				/*
				if (src.Status != VoxelStatus::Occupied)
					continue;
				*/

				if (_scene[dstIndex].Status == VoxelStatus::Occupied)
					continue;

				_scene[dstIndex] = src;
			}
		}
	}

	for (int32_t i = 0; i < faceCount; ++i)
	{
		const VoxelMeshResolvedFace& srcFace = faces[i];

		VoxelData& dst = _scene[srcFace.VoxelIndex];

		VoxelFaceData& face = dst.Faces[srcFace.Face];
		face.Metallic = srcFace.Metallic;
		face.Roughness = srcFace.Roughness;
		face.BaseColor = srcFace.BaseColor;
		face.Normal = srcFace.Normal;
	}
}

void VoxelLightBakerV2::AddGpuMeshFaces(
	const GpuVoxelFaceData* faces,
	int32_t faceCount)
{
	if (faces == nullptr || faceCount <= 0)
		return;

	for (int32_t i = 0; i < faceCount; ++i)
	{
		const GpuVoxelFaceData& src = faces[i];

		if (src.Cell.X < 0 || src.Cell.X >= _grid.Size.X)
			continue;

		if (src.Cell.Y < 0 || src.Cell.Y >= _grid.Size.Y)
			continue;

		if (src.Cell.Z < 0 || src.Cell.Z >= _grid.Size.Z)
			continue;

		if (src.Face < 0 || src.Face >= VOXEL_LIGHT_FACE_COUNT)
			continue;

		int32_t voxelIndex = VoxelIndex(_grid, src.Cell);

		VoxelData& dst = _scene[voxelIndex];

		dst.Status = VoxelStatus::Occupied;
		dst.Occupancy = 1.0f;

		VoxelFaceData& face = dst.Faces[src.Face];

		face.Side = static_cast<VoxelTriangleSide>(src.Side);
		face.BaseColor = src.BaseColor;
		face.Normal = src.Normal;
		face.Roughness = src.Roughness;
		face.Metallic = src.Metallic;
		face.TriangleId = 1;
	}
}

void VoxelLightBakerV2::BakeGeneratedRays(VoxelLightRawContributionV2& contribution)
{
	int32_t threadCount = _params.ThreadCount;

	if (threadCount <= 0)
		threadCount = int32_t(std::max(1u, std::thread::hardware_concurrency()));

	_marchers.resize(threadCount);

	for (int32_t i = 0; i < threadCount; ++i)
	{
		_marchers[i].SetContext(this, i);
		_marchers[i].Prepare(_voxelCount);
	}

	for (int32_t generation = 0; generation < _params.Bounce.MaxCount; ++generation)
	{
		if (_rays.empty())
			break;

		VoxelLightRawContributionV2 generationContribution;

		TraceRays(
			generationContribution,
			_nextRays,
			generation);

		MergeContribution(
			contribution,
			_currentMerge,
			generationContribution);

		_rays.swap(_nextRays);
		_nextRays.clear();
	}

	if (_params.InitiateLightField)
		CleanupUnvisitedFaces(contribution);
}


void VoxelLightBakerV2::BakeAreaLight(
	const AreaLight& light,
	VoxelLightContributionV2& contribution)
{
	contribution.Cells.clear();
	contribution.Samples.clear();
	ClearMergeState(_currentMerge);

	VoxelLightRawContributionV2 raw;

	Vec3 normal = light.Normal.Normalized();
	Vec3 direction = light.Direction.Normalized();
	Vec3 up = (light.Up - normal * Dot(light.Up, normal)).Normalized();
	Vec3 energy = light.Color * light.Intensity;

	if (Dot(normal, normal) <= Epsilon ||
		Dot(up, up) <= Epsilon ||
		Dot(direction, direction) <= Epsilon ||
		light.Width <= Epsilon ||
		light.Height <= Epsilon ||
		!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillAreaLightContribution(light, raw);

	if (_params.Bounce.MaxCount > 0)
	{
		GenerateAreaLightRays(light);
		BakeGeneratedRays(raw);
	}

	FinalizeContribution(raw, contribution);
	ClearMergeState(_currentMerge);
}

void VoxelLightBakerV2::PrefillAreaLightContribution(
	const AreaLight& light,
	VoxelLightRawContributionV2& contribution)
{
	VoxelLightRawContributionV2 directContribution;
	directContribution.Samples.reserve(_voxelCount);

	Vec3 normal = light.Normal.Normalized();
	Vec3 direction = light.Direction.Normalized();
	Vec3 up =
		(light.Up - normal * Dot(light.Up, normal)).Normalized();

	if (Dot(normal, normal) <= Epsilon ||
		Dot(up, up) <= Epsilon ||
		Dot(direction, direction) <= Epsilon)
	{
		return;
	}

	Vec3 right = Cross(up, normal).Normalized();
	Vec3 lightEnergy = light.Color * light.Intensity;

	float planeDenominator = Dot(normal, direction);

	if (std::fabs(planeDenominator) <= Epsilon)
		return;

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				Vec3I coord = { x, y, z };
				Vec3 center = VoxelCenter(_grid, coord);

				float distance =
					Dot(normal, center - light.Position) /
					planeDenominator;

				if (distance < -Epsilon)
					continue;

				distance = std::max(0.0f, distance);

				Vec3 emissionPoint =
					center - direction * distance;

				Vec3 local = emissionPoint - light.Position;

				if (std::fabs(Dot(local, right)) >
					light.Width * 0.5f)
				{
					continue;
				}

				if (std::fabs(Dot(local, up)) >
					light.Height * 0.5f)
				{
					continue;
				}

				float falloff =
					LightFalloffAtDistance(
						light.Falloff,
						distance);

				Vec3 energy = lightEnergy * falloff;

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				int32_t index = VoxelIndex(_grid, coord);

				directContribution.Samples.push_back({
					index,
					direction,
					energy
					});
			}
		}
	}

	MergeContribution(
		contribution,
		_currentMerge,
		directContribution);
}

void VoxelLightBakerV2::GenerateAreaLightRays(
	const AreaLight& light)
{
	_rays.clear();

	Vec3 normal = light.Normal.Normalized();
	Vec3 direction = light.Direction.Normalized();
	Vec3 up =
		(light.Up - normal * Dot(light.Up, normal)).Normalized();

	if (Dot(normal, normal) <= Epsilon ||
		Dot(up, up) <= Epsilon ||
		Dot(direction, direction) <= Epsilon ||
		light.Width <= Epsilon ||
		light.Height <= Epsilon)
	{
		return;
	}

	Vec3 right = Cross(up, normal).Normalized();
	Vec3 rayEnergy = light.Color * light.Intensity;

	if (!HasEnergy(rayEnergy, _params.EnergyThreshold))
		return;

	int32_t subSample =
		std::max(1, _params.RaySubsample);

	float sampleSpacing =
		_grid.VoxelSize / float(subSample);

	int32_t sampleCountX =
		std::max(
			1,
			int32_t(std::ceil(
				light.Width / sampleSpacing)));

	int32_t sampleCountY =
		std::max(
			1,
			int32_t(std::ceil(
				light.Height / sampleSpacing)));

	float spacingX =
		light.Width / float(sampleCountX);

	float spacingY =
		light.Height / float(sampleCountY);

	rayEnergy =
		rayEnergy / float(subSample * subSample);

	_rays.reserve(sampleCountX * sampleCountY);

	for (int32_t y = 0; y < sampleCountY; ++y)
	{
		float localY =
			-light.Height * 0.5f +
			(float(y) + 0.5f) * spacingY;

		for (int32_t x = 0; x < sampleCountX; ++x)
		{
			float localX =
				-light.Width * 0.5f +
				(float(x) + 0.5f) * spacingX;

			VoxelLightRay ray{};

			ray.Position =
				light.Position +
				right * localX +
				up * localY;

			ray.Direction = direction;
			ray.DirectionNormal = direction;
			ray.Energy = rayEnergy;
			ray.Falloff = light.Falloff;
			ray.Recovery = _params.Recovery;

			_rays.push_back(ray);
		}
	}
}

void VoxelLightBakerV2::BakePointLight(
	const PointLight& light,
	VoxelLightContributionV2& contribution)
{
	contribution.Cells.clear();
	contribution.Samples.clear();
	ClearMergeState(_currentMerge);

	VoxelLightRawContributionV2 raw;
	Vec3 energy = light.Color * light.Intensity;

	if (!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillPointLightContribution(light, raw);

	if (_params.Bounce.MaxCount > 0)
	{
		GeneratePointLightRays(light, false);
		BakeGeneratedRays(raw);

		if (_currentMerge.TouchedVoxels.size() < size_t(_voxelCount))
		{
			GeneratePointLightRays(light, true);
			BakeGeneratedRays(raw);
		}
	}

	FinalizeContribution(raw, contribution);
	ClearMergeState(_currentMerge);
}

void VoxelLightBakerV2::BakeDirectionalLight(
	const DirectionalLight& light,
	VoxelLightContributionV2& contribution)
{
	contribution.Cells.clear();
	contribution.Samples.clear();
	ClearMergeState(_currentMerge);

	VoxelLightRawContributionV2 raw;
	Vec3 direction = light.Direction.Normalized();
	Vec3 energy = light.Color * light.Intensity;

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillDirectionalLightContribution(light, raw);

	if (_params.Bounce.MaxCount > 0)
	{
		GenerateDirectionalLightRays(light);
		BakeGeneratedRays(raw);
	}

	FinalizeContribution(raw, contribution);
	ClearMergeState(_currentMerge);
}

void VoxelLightBakerV2::BakeSpotLight(
	const SpotLight& light,
	VoxelLightContributionV2& contribution)
{
	contribution.Cells.clear();
	contribution.Samples.clear();
	ClearMergeState(_currentMerge);

	VoxelLightRawContributionV2 raw;
	Vec3 direction = light.Direction.Normalized();
	Vec3 energy = light.Color * light.Intensity;

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(energy, _params.EnergyThreshold) ||
		light.OuterCos <= -1.0f ||
		light.InnerCos < light.OuterCos)
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillSpotLightContribution(light, raw);

	if (_params.Bounce.MaxCount > 0)
	{
		GenerateSpotLightRays(light);
		BakeGeneratedRays(raw);
	}

	FinalizeContribution(raw, contribution);
	ClearMergeState(_currentMerge);
}

void VoxelLightBakerV2::ClearLightField()
{
	_lightSamples.clear();
	_lightRanges.clear();
	std::fill(_voxelRangeHeads.begin(), _voxelRangeHeads.end(), -1);

	_field.Lookup.clear();
	_field.Contributions.clear();
}

void VoxelLightBakerV2::AccumulateLight(const VoxelLightContributionV2& contribution)
{
	AccumulateLight(
		contribution.Cells.data(),
		int32_t(contribution.Cells.size()),
		contribution.Samples.data(),
		int32_t(contribution.Samples.size()));
}

void VoxelLightBakerV2::AccumulateLight(
	const VoxelLightContributionCellV2* cells,
	int32_t cellCount,
	const VoxelLightContributionSampleV2* samples,
	int32_t sampleCount)
{
	if (cells == nullptr || cellCount <= 0 || samples == nullptr || sampleCount <= 0)
		return;

	if (uint64_t(_lightSamples.size()) + uint64_t(sampleCount) > uint64_t(UINT32_MAX))
		return;

	const uint32_t sampleBase = uint32_t(_lightSamples.size());

	_lightSamples.insert(_lightSamples.end(), samples, samples + sampleCount);

	for (int32_t i = 0; i < cellCount; ++i)
	{
		const VoxelLightContributionCellV2& cell = cells[i];

		if (cell.Index < 0 || cell.Index >= _voxelCount || cell.Count == 0)
			continue;

		const uint64_t end = uint64_t(cell.Offset) + uint64_t(cell.Count);

		if (end > uint64_t(sampleCount))
			continue;

		VoxelLightSampleRangeV2 range{};
		range.Offset = sampleBase + cell.Offset;
		range.Count = cell.Count;
		range.Next = _voxelRangeHeads[cell.Index];

		_voxelRangeHeads[cell.Index] = int32_t(_lightRanges.size());
		_lightRanges.push_back(range);
	}
}

VoxelLightFieldV2& VoxelLightBakerV2::GetLightField()
{
	BuildLightField();
	return _field;
}

void VoxelLightBakerV2::PrefillPointLightContribution(
	const PointLight& light,
	VoxelLightRawContributionV2& contribution)
{
	VoxelLightRawContributionV2 directContribution;
	directContribution.Samples.reserve(_voxelCount);

	Vec3 lightEnergy = light.Color * light.Intensity;

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				Vec3I coord = { x, y, z };

				int32_t index = VoxelIndex(_grid, coord);

				Vec3 center = VoxelCenter(_grid, coord);
				Vec3 lightToVoxel = center - light.Position;

				float distance = lightToVoxel.Length();

				if (distance <= Epsilon)
					continue;

				float falloff = LightFalloffAtDistance(light.Falloff, distance);
				Vec3 energy = lightEnergy * falloff;

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				Vec3 direction = lightToVoxel / distance;

				directContribution.Samples.push_back({
					index,
					direction,
					energy
					});
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBakerV2::PrefillDirectionalLightContribution(
	const DirectionalLight& light,
	VoxelLightRawContributionV2& contribution)
{
	VoxelLightRawContributionV2 directContribution;
	directContribution.Samples.reserve(_voxelCount);

	Vec3 direction = light.Direction.Normalized();
	Vec3 lightEnergy = DirectionalLightEnergy(light);

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(lightEnergy, _params.EnergyThreshold))
	{
		return;
	}

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				Vec3I coord = { x, y, z };

				int32_t index = VoxelIndex(_grid, coord);
				Vec3 center = VoxelCenter(_grid, coord);
				Vec3 local = center - light.Position;

				float distance = Dot(local, direction);

				if (distance < -Epsilon)
					continue;

				float falloff = LightFalloffAtDistance(light.Falloff, std::max(0.0f, distance));
				Vec3 energy = lightEnergy * falloff;

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				directContribution.Samples.push_back({
					index,
					direction,
					energy
					});
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBakerV2::PrefillSpotLightContribution(
	const SpotLight& light,
	VoxelLightRawContributionV2& contribution)
{
	VoxelLightRawContributionV2 directContribution;
	directContribution.Samples.reserve(_voxelCount);

	Vec3 lightEnergy = light.Color * light.Intensity;
	Vec3 lightDirection = light.Direction.Normalized();

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				Vec3I coord = { x, y, z };

				int32_t index = VoxelIndex(_grid, coord);

				Vec3 center = VoxelCenter(_grid, coord);
				Vec3 lightToVoxel = center - light.Position;

				float distance = lightToVoxel.Length();

				if (distance <= Epsilon)
					continue;

				Vec3 direction = lightToVoxel / distance;

				float cone = SpotConeAttenuation(
					lightDirection,
					light.InnerCos,
					light.OuterCos,
					direction);

				if (cone <= 0.0f)
					continue;

				float falloff = LightFalloffAtDistance(light.Falloff, distance);
				Vec3 energy = lightEnergy * (falloff * cone);

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				directContribution.Samples.push_back({
					index,
					direction,
					energy
					});
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBakerV2::GeneratePointLightRays(const PointLight& light, bool fillMode)
{
	_rays.clear();

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 rayEnergy = light.Color * light.Intensity;

	rayEnergy = rayEnergy / float(subSample * subSample);

	auto addRay = [this, &light, rayEnergy](const Vec3& origin)
		{
			Vec3 dir = (origin - light.Position).Normalized();

			if (Dot(dir, dir) <= Epsilon)
				return;

			VoxelLightRay ray{};
			ray.Position = light.Position;
			ray.Direction = dir;
			ray.DirectionNormal = dir;
			ray.Energy = rayEnergy;
			ray.Falloff = light.Falloff;
			ray.Recovery = _params.Recovery;

			_rays.push_back(ray);
		};

	if (fillMode) {

		for (int i = 0; i < _voxelCount; i++) {

			if (_currentMerge.CellSlots[i] == -1)
			{
				Vec3I cell = VoxelCell(_grid, i);
				Vec3 origin = VoxelCenter(_grid, cell);

				float fallOf = LightFalloffAtDistance(light.Falloff, (origin - light.Position).Length());

				if (fallOf > 0)
					addRay(origin);
			}
		}

		return;
	}

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t sy = 0; sy < subSample; ++sy)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + fz * size
						});

					addRay({
						_grid.Origin.X + float(_grid.Size.X) * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t x = 0; x < _grid.Size.X; ++x)
		{
			for (int32_t sx = 0; sx < subSample; ++sx)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y,
						_grid.Origin.Z + fz * size
						});

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + float(_grid.Size.Y) * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t y = 0; y < _grid.Size.Y; ++y)
	{
		for (int32_t x = 0; x < _grid.Size.X; ++x)
		{
			for (int32_t sx = 0; sx < subSample; ++sx)
			{
				for (int32_t sy = 0; sy < subSample; ++sy)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z
						});

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + float(_grid.Size.Z) * size
						});
				}
			}
		}
	}
}

void VoxelLightBakerV2::GenerateDirectionalLightRays(const DirectionalLight& light)
{
	_rays.clear();

	Vec3 direction = light.Direction.Normalized();
	Vec3 lightEnergy = DirectionalLightEnergy(light);

	if (Dot(direction, direction) <= Epsilon)
		return;

	if (!HasEnergy(lightEnergy, _params.EnergyThreshold))
		return;

	Vec3 right;
	Vec3 up;
	DirectionBasis(direction, right, up);


	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 gridMin = _grid.Origin;
	Vec3 gridMax =
	{
		gridMin.X + float(_grid.Size.X) * size,
		gridMin.Y + float(_grid.Size.Y) * size,
		gridMin.Z + float(_grid.Size.Z) * size
	};

	Vec3 rayEnergy = lightEnergy;

	rayEnergy = rayEnergy / float(subSample * subSample);

	auto tryAddRay = [&](const Vec3& destination)
		{
			// Intersect the backward line from the uniformly sampled
			// grid destination with the emission plane.
			float distanceToPlane =
				Dot(destination - light.Position, direction);

			if (distanceToPlane < -Epsilon)
				return;

			Vec3 emissionPoint =
				destination - direction * distanceToPlane;

			// Find the first usable point of the segment
			// emissionPoint -> destination inside the voxel grid.
			// If emissionPoint is already inside, entryDistance remains zero.
			float entryDistance = 0.0f;
			float exitDistance = distanceToPlane;

			if (std::fabs(direction.X) <= Epsilon)
			{
				if (emissionPoint.X < gridMin.X ||
					emissionPoint.X > gridMax.X)
					return;
			}
			else
			{
				float t0 =
					(gridMin.X - emissionPoint.X) / direction.X;
				float t1 =
					(gridMax.X - emissionPoint.X) / direction.X;

				if (t0 > t1)
					std::swap(t0, t1);

				entryDistance = std::max(entryDistance, t0);
				exitDistance = std::min(exitDistance, t1);
			}

			if (std::fabs(direction.Y) <= Epsilon)
			{
				if (emissionPoint.Y < gridMin.Y ||
					emissionPoint.Y > gridMax.Y)
					return;
			}
			else
			{
				float t0 =
					(gridMin.Y - emissionPoint.Y) / direction.Y;
				float t1 =
					(gridMax.Y - emissionPoint.Y) / direction.Y;

				if (t0 > t1)
					std::swap(t0, t1);

				entryDistance = std::max(entryDistance, t0);
				exitDistance = std::min(exitDistance, t1);
			}

			if (std::fabs(direction.Z) <= Epsilon)
			{
				if (emissionPoint.Z < gridMin.Z ||
					emissionPoint.Z > gridMax.Z)
					return;
			}
			else
			{
				float t0 =
					(gridMin.Z - emissionPoint.Z) / direction.Z;
				float t1 =
					(gridMax.Z - emissionPoint.Z) / direction.Z;

				if (t0 > t1)
					std::swap(t0, t1);

				entryDistance = std::max(entryDistance, t0);
				exitDistance = std::min(exitDistance, t1);
			}

			if (entryDistance > exitDistance + Epsilon)
				return;

			if (entryDistance > distanceToPlane + Epsilon)
				return;

			Vec3 energy =
				rayEnergy *
				LightFalloffAtDistance(light.Falloff, entryDistance);

			if (!HasEnergy(energy, _params.EnergyThreshold))
				return;

			VoxelLightRay ray{};

			// CreateRay clips to the grid; Step evaluates falloff from the emission origin.
			ray.Position = emissionPoint;
			ray.Direction = direction;
			ray.DirectionNormal = direction;
			ray.Energy = rayEnergy;
			ray.Falloff = light.Falloff;
			ray.Recovery = _params.Recovery;

			_rays.push_back(ray);
		};

	// Sample only the three possible destination faces.

	if (std::fabs(direction.X) > Epsilon)
	{
		float destinationX =
			direction.X > 0.0f ? gridMax.X : gridMin.X;

		for (int32_t z = 0; z < _grid.Size.Z; ++z)
		{
			for (int32_t y = 0; y < _grid.Size.Y; ++y)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					for (int32_t sy = 0; sy < subSample; ++sy)
					{
						float fy =
							float(y) +
							(float(sy) + 0.5f) * invSubSample;

						float fz =
							float(z) +
							(float(sz) + 0.5f) * invSubSample;

						tryAddRay(
							{
								destinationX,
								gridMin.Y + fy * size,
								gridMin.Z + fz * size
							});
					}
				}
			}
		}
	}

	if (std::fabs(direction.Y) > Epsilon)
	{
		float destinationY =
			direction.Y > 0.0f ? gridMax.Y : gridMin.Y;

		for (int32_t z = 0; z < _grid.Size.Z; ++z)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					for (int32_t sx = 0; sx < subSample; ++sx)
					{
						float fx =
							float(x) +
							(float(sx) + 0.5f) * invSubSample;

						float fz =
							float(z) +
							(float(sz) + 0.5f) * invSubSample;

						tryAddRay(
							{
								gridMin.X + fx * size,
								destinationY,
								gridMin.Z + fz * size
							});
					}
				}
			}
		}
	}

	if (std::fabs(direction.Z) > Epsilon)
	{
		float destinationZ =
			direction.Z > 0.0f ? gridMax.Z : gridMin.Z;

		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t x = 0; x < _grid.Size.X; ++x)
			{
				for (int32_t sy = 0; sy < subSample; ++sy)
				{
					for (int32_t sx = 0; sx < subSample; ++sx)
					{
						float fx =
							float(x) +
							(float(sx) + 0.5f) * invSubSample;

						float fy =
							float(y) +
							(float(sy) + 0.5f) * invSubSample;

						tryAddRay(
							{
								gridMin.X + fx * size,
								gridMin.Y + fy * size,
								destinationZ
							});
					}
				}
			}
		}
	}
}

void VoxelLightBakerV2::GenerateSpotLightRays(const SpotLight& light)
{
	_rays.clear();

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;
	Vec3 lightEnergy = light.Color * light.Intensity;
	Vec3 lightDirection = light.Direction.Normalized();

	auto addRay = [this, &light, lightEnergy, lightDirection, subSample](const Vec3& origin)
		{
			Vec3 dir = (origin - light.Position).Normalized();

			if (Dot(dir, dir) <= Epsilon)
				return;

			float cone = SpotConeAttenuation(
				lightDirection,
				light.InnerCos,
				light.OuterCos,
				dir);

			if (cone <= 0.0f)
				return;

			Vec3 rayEnergy = lightEnergy * cone;

			rayEnergy = rayEnergy / float(subSample * subSample);

			if (!HasEnergy(rayEnergy, _params.EnergyThreshold))
				return;

			VoxelLightRay ray{};
			ray.Position = light.Position;
			ray.Direction = dir;
			ray.DirectionNormal = dir;
			ray.Energy = rayEnergy;
			ray.Falloff = light.Falloff;
			ray.Recovery = _params.Recovery;

			_rays.push_back(ray);
		};

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t y = 0; y < _grid.Size.Y; ++y)
		{
			for (int32_t sy = 0; sy < subSample; ++sy)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + fz * size
						});

					addRay({
						_grid.Origin.X + float(_grid.Size.X) * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t z = 0; z < _grid.Size.Z; ++z)
	{
		for (int32_t x = 0; x < _grid.Size.X; ++x)
		{
			for (int32_t sx = 0; sx < subSample; ++sx)
			{
				for (int32_t sz = 0; sz < subSample; ++sz)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y,
						_grid.Origin.Z + fz * size
						});

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + float(_grid.Size.Y) * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t y = 0; y < _grid.Size.Y; ++y)
	{
		for (int32_t x = 0; x < _grid.Size.X; ++x)
		{
			for (int32_t sx = 0; sx < subSample; ++sx)
			{
				for (int32_t sy = 0; sy < subSample; ++sy)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z
						});

					addRay({
						_grid.Origin.X + fx * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + float(_grid.Size.Z) * size
						});
				}
			}
		}
	}
}

void VoxelLightBakerV2::CleanupUnvisitedFaces(VoxelLightRawContributionV2& contribution)
{
	(void)contribution;
}

void VoxelLightBakerV2::TraceRays(
	VoxelLightRawContributionV2& contribution,
	std::vector<VoxelLightRay>& nextRays,
	int32_t generation)
{
	contribution.Samples.clear();
	nextRays.clear();

	int32_t rayCount = int32_t(_rays.size());

	if (rayCount == 0)
		return;

	int32_t threadCount = int32_t(_marchers.size());

	if (threadCount <= 1 || rayCount < threadCount)
	{
		_marchers[0].TraceRange(0, rayCount, generation);

		const VoxelLightRawContributionV2& workerContribution =
			_marchers[0].Contribution();

		contribution.Samples.insert(
			contribution.Samples.end(),
			workerContribution.Samples.begin(),
			workerContribution.Samples.end());

		const std::vector<VoxelLightRay>& workerNext = _marchers[0].NextRays();
		nextRays.insert(nextRays.end(), workerNext.begin(), workerNext.end());

		return;
	}

	std::vector<std::thread> threads;
	threads.reserve(threadCount);

	int32_t rangeStart = 0;

	for (int32_t i = 0; i < threadCount; ++i)
	{
		int32_t rangeEnd = (rayCount * (i + 1)) / threadCount;
		int32_t start = rangeStart;
		int32_t end = rangeEnd;

		threads.emplace_back([this, i, start, end, generation]()
			{
				_marchers[i].TraceRange(start, end, generation);
			});

		rangeStart = rangeEnd;
	}

	for (std::thread& thread : threads)
		thread.join();

	size_t sampleCount = 0;
	size_t nextRayCount = 0;

	for (int32_t i = 0; i < threadCount; ++i)
	{
		sampleCount += _marchers[i].Contribution().Samples.size();
		nextRayCount += _marchers[i].NextRays().size();
	}

	contribution.Samples.reserve(sampleCount);
	nextRays.reserve(nextRayCount);

	for (int32_t i = 0; i < threadCount; ++i)
	{
		const VoxelLightRawContributionV2& workerContribution =
			_marchers[i].Contribution();

		contribution.Samples.insert(
			contribution.Samples.end(),
			workerContribution.Samples.begin(),
			workerContribution.Samples.end());

		const std::vector<VoxelLightRay>& workerNext = _marchers[i].NextRays();
		nextRays.insert(nextRays.end(), workerNext.begin(), workerNext.end());
	}
}

void VoxelLightBakerV2::MergeContribution(
	VoxelLightRawContributionV2& target,
	ContributionMergeStateV2& mergeState,
	const VoxelLightRawContributionV2& source)
{

	target.Samples.insert(
		target.Samples.end(),
		source.Samples.begin(),
		source.Samples.end());

	for (const VoxelLightSample& sample : source.Samples)
	{
		if (sample.Index < 0 || sample.Index >= _voxelCount)
			continue;

		if (mergeState.CellSlots[sample.Index] >= 0)
			continue;

		mergeState.CellSlots[sample.Index] = 0;
		mergeState.TouchedVoxels.push_back(sample.Index);
	}
}

void VoxelLightBakerV2::ClearMergeState(ContributionMergeStateV2& mergeState) {
	for (int32_t index : mergeState.TouchedVoxels)mergeState.CellSlots[index] = -1;

	mergeState.TouchedVoxels.clear();

}


void VoxelLightBakerV2::FinalizeContribution(
	const VoxelLightRawContributionV2& source,
	VoxelLightContributionV2& target)
{
	target.Cells.clear();
	target.Samples.clear();

	if (source.Samples.empty() || _voxelCount <= 0)
		return;

	_contributionCounts.assign(_voxelCount, 0);

	uint32_t validCount = 0;

	for (const VoxelLightSample& sample : source.Samples)
	{
		if (sample.Index < 0 || sample.Index >= _voxelCount)
			continue;

		++_contributionCounts[sample.Index];
		++validCount;
	}

	if (validCount == 0)
		return;

	_contributionOffsets.resize(size_t(_voxelCount) + 1);
	_contributionOffsets[0] = 0;

	for (int32_t i = 0; i < _voxelCount; ++i)
		_contributionOffsets[size_t(i) + 1] = _contributionOffsets[i] + _contributionCounts[i];

	_contributionCursor.resize(_voxelCount);

	for (int32_t i = 0; i < _voxelCount; ++i)
		_contributionCursor[i] = _contributionOffsets[i];

	target.Samples.resize(validCount);

	for (const VoxelLightSample& sample : source.Samples)
	{
		if (sample.Index < 0 || sample.Index >= _voxelCount)
			continue;

		const uint32_t dst = _contributionCursor[sample.Index]++;

		target.Samples[dst] = {
			sample.Direction,
			sample.Energy
		};
	}

	target.Cells.reserve(std::min<size_t>(source.Samples.size(), size_t(_voxelCount)));

	for (int32_t index = 0; index < _voxelCount; ++index)
	{
		const uint32_t count = _contributionCounts[index];

		if (count == 0)
			continue;

		const uint32_t offset = _contributionOffsets[index];
		auto begin = target.Samples.begin() + offset;
		auto end = begin + count;

		std::sort(
			begin,
			end,
			[](const VoxelLightContributionSampleV2& a, const VoxelLightContributionSampleV2& b)
			{
				return EnergyScore(a.Energy) > EnergyScore(b.Energy);
			});

		target.Cells.push_back({
			index,
			offset,
			count
			});
	}
}

void VoxelLightBakerV2::AppendClusteredVoxel(VoxelLightFieldV2& field, int32_t voxelIndex,
	std::vector<VoxelLightContributionSampleV2>& samples, float angularTolerance, float relativeEnergyTolerance) const
{
	if (samples.empty())
		return;

	float totalEnergy = 0.0f;

	for (const VoxelLightContributionSampleV2& sample : samples)
		totalEnergy += ContributionWeight(sample.Energy);

	const float weakBudget = totalEnergy * relativeEnergyTolerance;

	float weakEnergy = 0.0f;
	size_t strongEnd = samples.size();

	while (strongEnd > 0)
	{
		const float weight = ContributionWeight(samples[strongEnd - 1].Energy);

		if (weakEnergy + weight > weakBudget)
			break;

		weakEnergy += weight;
		--strongEnd;
	}

	thread_local std::vector<VoxelLightBuildCluster> clusters;
	clusters.clear();

	const size_t reserveCount = std::min<size_t>(samples.size(), 16);

	if (clusters.capacity() < reserveCount)
		clusters.reserve(reserveCount);

	// angularTolerance is constant during the field build.
	thread_local float cachedTolerance = -1.0f;
	thread_local float toleranceCos = 1.0f;
	thread_local float toleranceSin = 0.0f;
	thread_local float doubleToleranceCos = -1.0f;
	thread_local bool useDoubleToleranceReject = false;

	if (cachedTolerance != angularTolerance)
	{
		cachedTolerance = angularTolerance;
		toleranceCos = std::cos(angularTolerance);
		toleranceSin = std::sqrt(std::max(0.0f, 1.0f - toleranceCos * toleranceCos));
		useDoubleToleranceReject = angularTolerance < (Pi * 0.5f);

		if (useDoubleToleranceReject)
			doubleToleranceCos = 2.0f * toleranceCos * toleranceCos - 1.0f;
		else
			doubleToleranceCos = -1.0f;
	}

	for (size_t i = 0; i < strongEnd; ++i)
	{
		const VoxelLightContributionSampleV2& sample = samples[i];
		const float sampleWeight = ContributionWeight(sample.Energy);

		int32_t bestCluster = -1;
		float bestDot = -2.0f;
		VoxelLightClusterMerge bestMerge{};

		for (int32_t ci = 0; ci < int32_t(clusters.size()); ++ci)
		{
			const float d = Dot(clusters[ci].Direction, sample.Direction);

			if (d <= bestDot)
				continue;

			VoxelLightClusterMerge merge{};

			if (!TryMergeCluster(clusters[ci], sample, sampleWeight, toleranceCos, toleranceSin, doubleToleranceCos,
				useDoubleToleranceReject, merge))
			{
				continue;
			}

			bestDot = d;
			bestCluster = ci;
			bestMerge = merge;
		}

		if (bestCluster < 0)
		{
			VoxelLightBuildCluster cluster{};
			InitCluster(cluster, sample);
			clusters.push_back(cluster);
		}
		else
		{
			CommitClusterMerge(clusters[bestCluster], sample, sampleWeight, bestMerge);
		}
	}

	for (size_t i = strongEnd; i < samples.size(); ++i)
	{
		const VoxelLightContributionSampleV2& sample = samples[i];

		if (clusters.empty())
		{
			VoxelLightBuildCluster cluster{};
			InitCluster(cluster, sample);
			clusters.push_back(cluster);
			continue;
		}

		int32_t bestCluster = 0;
		float bestDot = Dot(clusters[0].Direction, sample.Direction);

		for (int32_t ci = 1; ci < int32_t(clusters.size()); ++ci)
		{
			const float d = Dot(clusters[ci].Direction, sample.Direction);

			if (d > bestDot)
			{
				bestDot = d;
				bestCluster = ci;
			}
		}

		MergeClusterUnbounded(clusters[bestCluster], sample);
	}

	VoxelLightLookup& lookup = field.Lookup[voxelIndex];
	lookup.Offset = uint32_t(field.Contributions.size());
	lookup.Count = uint32_t(clusters.size());

	for (const VoxelLightBuildCluster& cluster : clusters)
	{
		VoxelLightGpuContribution contribution{};

		contribution.Direction = {
			cluster.Direction.X,
			cluster.Direction.Y,
			cluster.Direction.Z,
			0.0f
		};

		contribution.Color = {
			cluster.Color.X,
			cluster.Color.Y,
			cluster.Color.Z,
			0.0f
		};

		field.Contributions.push_back(contribution);
	}
}


void VoxelLightBakerV2::BlurLightFieldAxis(
	const VoxelLightFieldV2& source,
	VoxelLightFieldV2& target,
	int32_t axis,
	float angularTolerance,
	float relativeEnergyTolerance,
	float blurStrength,
	const VoxelLightFieldV2* blendSource) const
{
	target.Size = source.Size;
	target.Lookup.assign(_voxelCount, VoxelLightLookup{});
	target.Contributions.clear();

	static constexpr float Kernel[3] = { 1.0f, 2.0f, 1.0f };

	std::vector<VoxelLightContributionSampleV2> samples;

	auto appendCell = [&](const VoxelLightFieldV2& field, int32_t index, float weight)
		{
			if (weight <= 0.0f)
				return;

			const VoxelLightLookup& lookup = field.Lookup[index];
			const uint64_t end = uint64_t(lookup.Offset) + uint64_t(lookup.Count);

			if (end > field.Contributions.size())
				return;

			samples.reserve(samples.size() + lookup.Count);

			for (uint32_t i = 0; i < lookup.Count; ++i)
			{
				const VoxelLightGpuContribution& src = field.Contributions[lookup.Offset + i];

				samples.push_back({
					{ src.Direction.X, src.Direction.Y, src.Direction.Z },
					{ src.Color.X * weight, src.Color.Y * weight, src.Color.Z * weight }
					});
			}
		};

	for (int32_t index = 0; index < _voxelCount; ++index)
	{
		samples.clear();

		const Vec3I cell = VoxelCell(_grid, index);
		float weightSum = 0.0f;

		for (int32_t k = -1; k <= 1; ++k)
		{
			Vec3I neighbor = cell;

			if (axis == 0)
				neighbor.X += k;
			else if (axis == 1)
				neighbor.Y += k;
			else
				neighbor.Z += k;

			if (!IsInsideGrid(_grid, neighbor))
				continue;

			weightSum += Kernel[k + 1];
		}

		const float sourceScale = blendSource == nullptr ? 1.0f : blurStrength;

		if (weightSum > Epsilon && sourceScale > 0.0f)
		{
			for (int32_t k = -1; k <= 1; ++k)
			{
				Vec3I neighbor = cell;

				if (axis == 0)
					neighbor.X += k;
				else if (axis == 1)
					neighbor.Y += k;
				else
					neighbor.Z += k;

				if (!IsInsideGrid(_grid, neighbor))
					continue;

				const float weight = Kernel[k + 1] / weightSum * sourceScale;
				appendCell(source, VoxelIndex(_grid, neighbor), weight);
			}
		}

		if (blendSource != nullptr && blurStrength < 1.0f)
			appendCell(*blendSource, index, 1.0f - blurStrength);

		if (samples.empty())
			continue;

		std::sort(
			samples.begin(),
			samples.end(),
			[](const VoxelLightContributionSampleV2& a, const VoxelLightContributionSampleV2& b)
			{
				return EnergyScore(a.Energy) > EnergyScore(b.Energy);
			});

		AppendClusteredVoxel(target, index, samples, angularTolerance, relativeEnergyTolerance);
	}
}

void VoxelLightBakerV2::BlurLightField(
	VoxelLightFieldV2& field,
	float angularTolerance,
	float relativeEnergyTolerance)
{
	const float strength = std::clamp(_params.Blur.Strength, 0.0f, 1.0f);
	const int32_t passes = std::max(0, _params.Blur.Passes);

	if (strength <= 0.0f || passes <= 0 || field.Contributions.empty())
		return;

	VoxelLightFieldV2 xField;
	VoxelLightFieldV2 yField;
	VoxelLightFieldV2 zField;

	for (int32_t pass = 0; pass < passes; ++pass)
	{
		BlurLightFieldAxis(field, xField, 0, angularTolerance, relativeEnergyTolerance, 1.0f, nullptr);
		BlurLightFieldAxis(xField, yField, 1, angularTolerance, relativeEnergyTolerance, 1.0f, nullptr);
		BlurLightFieldAxis(yField, zField, 2, angularTolerance, relativeEnergyTolerance, strength, &field);

		field = std::move(zField);

		xField.Lookup.clear();
		xField.Contributions.clear();
		yField.Lookup.clear();
		yField.Contributions.clear();
	}
}

void VoxelLightBakerV2::BuildLightField()
{
	BuildLightField(_field, _params.AngularTolerance, _params.RelativeEnergyTolerance);
}

VoxelLightFieldV2& VoxelLightBakerV2::BuildLightField(
	float angularTolerance,
	float relativeEnergyTolerance)
{
	BuildLightField(_field, angularTolerance, relativeEnergyTolerance);
	return _field;
}

void VoxelLightBakerV2::BuildLightField(
	VoxelLightFieldV2& field,
	float angularTolerance,
	float relativeEnergyTolerance)
{
	field.Size = _grid.Size;
	field.Lookup.assign(_voxelCount, VoxelLightLookup{});
	field.Contributions.clear();

	if (_lightSamples.empty() || _lightRanges.empty())
		return;

	angularTolerance = std::clamp(angularTolerance, 0.0f, Pi);
	relativeEnergyTolerance = std::clamp(relativeEnergyTolerance, 0.0f, 1.0f);

	struct RangeCursor
	{
		const VoxelLightContributionSampleV2* Current;
		const VoxelLightContributionSampleV2* End;
	};

	struct HeapItem
	{
		float Weight;
		int32_t Cursor;
	};

	auto heapLess = [](const HeapItem& a, const HeapItem& b)
		{
			return a.Weight < b.Weight;
		};

	std::vector<RangeCursor> cursors;
	std::vector<HeapItem> heap;
	std::vector<VoxelLightContributionSampleV2> samples;

	for (int32_t voxelIndex = 0; voxelIndex < _voxelCount; ++voxelIndex)
	{
		cursors.clear();
		heap.clear();
		samples.clear();

		size_t totalCount = 0;

		for (int32_t rangeIndex = _voxelRangeHeads[voxelIndex];
			rangeIndex >= 0;
			rangeIndex = _lightRanges[rangeIndex].Next)
		{
			const VoxelLightSampleRangeV2& range = _lightRanges[rangeIndex];
			const uint64_t end = uint64_t(range.Offset) + uint64_t(range.Count);

			if (range.Count == 0 || end > _lightSamples.size())
				continue;

			const VoxelLightContributionSampleV2* begin = _lightSamples.data() + range.Offset;

			cursors.push_back({
				begin,
				begin + range.Count
				});

			totalCount += range.Count;
		}

		if (cursors.empty())
			continue;

		samples.reserve(totalCount);
		heap.reserve(cursors.size());

		for (int32_t i = 0; i < int32_t(cursors.size()); ++i)
		{
			heap.push_back({
				ContributionWeight(cursors[i].Current->Energy),
				i
				});
		}

		std::make_heap(heap.begin(), heap.end(), heapLess);

		while (!heap.empty())
		{
			std::pop_heap(heap.begin(), heap.end(), heapLess);
			const HeapItem item = heap.back();
			heap.pop_back();

			RangeCursor& cursor = cursors[item.Cursor];
			samples.push_back(*cursor.Current);
			++cursor.Current;

			if (cursor.Current < cursor.End)
			{
				heap.push_back({
					ContributionWeight(cursor.Current->Energy),
					item.Cursor
					});

				std::push_heap(heap.begin(), heap.end(), heapLess);
			}
		}

		AppendClusteredVoxel(field, voxelIndex, samples, angularTolerance, relativeEnergyTolerance);
	}

	BlurLightField(field, angularTolerance, relativeEnergyTolerance);
}
