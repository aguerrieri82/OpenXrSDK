#include "pch.h"

namespace {
	
	constexpr float Pi = 3.14159265358979323846f; 
	constexpr float Epsilon = 1e-5f;

	static constexpr Vec3 FaceNormals[VOXEL_LIGHT_FACE_COUNT] =
	{
		{ -1.0f,  0.0f,  0.0f },
		{  1.0f,  0.0f,  0.0f },
		{  0.0f, -1.0f,  0.0f },
		{  0.0f,  1.0f,  0.0f },
		{  0.0f,  0.0f, -1.0f },
		{  0.0f,  0.0f,  1.0f }
	};

	FORCE_INLINE float Luma(const Vec3& v)
	{
		return v.X * 0.2126f + v.Y * 0.7152f + v.Z * 0.0722f;
	}

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



	FORCE_INLINE int32_t FieldIndex(const VoxelLightField& field, Vec3I cell)
	{
		return cell.X + cell.Y * field.Size.X + cell.Z * field.Size.X * field.Size.Y;
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

	int32_t ReserveCell(ContributionMergeState& contrib, int32_t index, bool& isNew)
	{
		int32_t slot = contrib.CellSlots[index];

		if (slot < 0)
		{
			slot = int32_t(contrib.Contribution.Cells.size());

			contrib.CellSlots[index] = slot;
			contrib.TouchedVoxels.push_back(index);

			VoxelLightCell cell{};
			cell.Index = index;

			contrib.Contribution.Cells.push_back(cell);

			isNew = true;
		}
		else
			isNew = false;

		return slot;
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

	template<bool NormalMode>
	FORCE_INLINE VoxelLightEnergy MakeEnergy(const Vec3& energy, const Vec3& direction)
	{
		VoxelLightEnergy result{};

		result.Energy = energy;

		if (NormalMode)
			result.DirectionN = direction * Luma(energy);
		else
		{
			result.DirectionR = direction * energy.X;
			result.DirectionG = direction * energy.Y;
			result.DirectionB = direction * energy.Z;
		}
		return result;
	}


	FORCE_INLINE VoxelLightEnergy MakeEnergy(const Vec3& energy, const Vec3& direction, const bool normalMode)
	{
		return normalMode ? MakeEnergy<true>(energy, direction) : MakeEnergy<false>(energy, direction);
	}


	FORCE_INLINE float EnergyScore(const Vec3& energy)
	{
		return energy.X + energy.Y + energy.Z;
	}


	template<VoxelLightMergeMode Mode, bool NormalMode>
	void MergeEnergy(
		VoxelLightEnergy& target,
		const VoxelLightEnergy& source,
		VoxelLightState targetState,
		VoxelLightState sourceState)
	{
		if (Mode == VoxelLightMergeMode::Add)
		{
			target.Energy += source.Energy;

			if (NormalMode)
				target.DirectionN += source.DirectionN;
			else
			{
				target.DirectionR += source.DirectionR;
				target.DirectionG += source.DirectionG;
				target.DirectionB += source.DirectionB;
			}
		}
		else if (Mode == VoxelLightMergeMode::AddPreserveDir)
		{
			target.Energy += source.Energy;

			if (NormalMode)
			{
				if (target.DirectionN.LengthSquared() < Epsilon)
					target.DirectionN = source.DirectionN;
			}
			else
			{
				if (target.DirectionR.LengthSquared() < Epsilon)
					target.DirectionR = source.DirectionR;

				if (target.DirectionG.LengthSquared() < Epsilon)
					target.DirectionG = source.DirectionG;

				if (target.DirectionB.LengthSquared() < Epsilon)
					target.DirectionB = source.DirectionB;
			}
		}
		else if (Mode == VoxelLightMergeMode::MaxSample)
		{
			float srcScore = EnergyScore(source.Energy);
			float targetScore = EnergyScore(target.Energy);

			bool replace = false;

			if (sourceState == VoxelLightState::Occlusion)
			{
				if (targetState == VoxelLightState::Occlusion)
					replace = srcScore < targetScore;
				else
					replace = true;
			}
			else
			{
				replace = srcScore > targetScore;
			}

			if (replace)
			{
				target.Energy = source.Energy;

				if (NormalMode)
					target.DirectionN = source.DirectionN;
				else
				{
					target.DirectionR = source.DirectionR;
					target.DirectionG = source.DirectionG;
					target.DirectionB = source.DirectionB;
				}
			}
		}
	}

	template<VoxelLightMergeMode Mode, bool NormalMode>
	void MergeFace(
		VoxelLightFace& target,
		const VoxelLightFace& source)
	{
		MergeEnergy<Mode, NormalMode>(target.Outgoing, source.Outgoing, target.State, source.State);
		target.Outgoing.VisitCount = std::max(target.Outgoing.VisitCount, source.Outgoing.VisitCount);

#ifdef _DEBUG
		MergeEnergy<Mode, NormalMode>(target.Incoming, source.Incoming, target.State, source.State);
		target.Incoming.VisitCount = std::max(target.Incoming.VisitCount, source.Incoming.VisitCount);
#endif
	}

	template<VoxelLightMergeMode Mode, bool NormalMode>
	void MergeVoxelLightData(
		VoxelLightData& target,
		const VoxelLightData& source)
	{
		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
			MergeFace<Mode, NormalMode>(target.Faces[face], source.Faces[face]);
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



	int32_t RayVoxelExitFaceWeights(
		const Vec3& voxelCenter,
		float voxelSize,
		const Vec3& rayPosition,
		const Vec3& direction,
		VoxelFaceWeight outFaces[3],
		bool centerPlaneMode)
	{
		const float half = voxelSize * 0.5f;

		const float px[3] = {
			rayPosition.X,
			rayPosition.Y,
			rayPosition.Z
		};

		const float dc[3] = {
			direction.X,
			direction.Y,
			direction.Z
		};

		const float cc[3] = {
			voxelCenter.X,
			voxelCenter.Y,
			voxelCenter.Z
		};

		float bestT = FLT_MAX;
		int32_t mainAxis = -1;
		bool mainPositive = false;

		for (int32_t axis = 0; axis < 3; ++axis)
		{
			const float d = dc[axis];

			if (std::fabs(d) <= Epsilon)
				continue;

			const bool positive = d > 0.0f;

			const float boundary = centerPlaneMode
				? cc[axis]
				: cc[axis] + (positive ? half : -half);

			const float t = (boundary - px[axis]) / d;

			if (t >= 0.0f && t < bestT)
			{
				bestT = t;
				mainAxis = axis;
				mainPositive = positive;
			}
		}

		if (mainAxis < 0)
			return 0;

		const Vec3 p = rayPosition + direction * bestT;

		float u;
		float v;
		int32_t uAxis;
		int32_t vAxis;

		if (mainAxis == 0)
		{
			u = p.Y - voxelCenter.Y;
			v = p.Z - voxelCenter.Z;
			uAxis = 1;
			vAxis = 2;
		}
		else if (mainAxis == 1)
		{
			u = p.X - voxelCenter.X;
			v = p.Z - voxelCenter.Z;
			uAxis = 0;
			vAxis = 2;
		}
		else
		{
			u = p.X - voxelCenter.X;
			v = p.Y - voxelCenter.Y;
			uAxis = 0;
			vAxis = 1;
		}

		if (centerPlaneMode)
		{
			constexpr float weight = 1.0f / 3.0f;

			outFaces[0].Face =
				mainAxis * 2 + int32_t(mainPositive);
			outFaces[0].Weight = weight;

			// Deliberately use > rather than >=:
			// exact zero reproduced NaN >= 0 == false in the original path.
			outFaces[1].Face =
				uAxis * 2 + int32_t(u > 0.0f);
			outFaces[1].Weight = weight;

			outFaces[2].Face =
				vAxis * 2 + int32_t(v > 0.0f);
			outFaces[2].Weight = weight;

			return 3;
		}

		const float invHalf = 1.0f / half;

		u = std::clamp(u * invHalf, -1.0f, 1.0f);
		v = std::clamp(v * invHalf, -1.0f, 1.0f);

		const float uWeight = u * u;
		const float vWeight = v * v;
		const float invSum = 1.0f / (1.0f + uWeight + vWeight);

		int32_t count = 0;

		outFaces[count].Face =
			mainAxis * 2 + int32_t(mainPositive);
		outFaces[count].Weight = invSum;
		++count;

		if (uWeight > 0.000001f)
		{
			outFaces[count].Face =
				uAxis * 2 + int32_t(u >= 0.0f);
			outFaces[count].Weight =
				uWeight * invSum;
			++count;
		}

		if (vWeight > 0.000001f)
		{
			outFaces[count].Face =
				vAxis * 2 + int32_t(v >= 0.0f);
			outFaces[count].Weight =
				vWeight * invSum;
			++count;
		}

		return count;
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
		float angleRad,
		int32_t index,
		int32_t count)
	{
		if (count <= 1 || angleRad <= Epsilon)
			return center;

		Vec3 up = std::fabs(center.Y) < 0.9f
			? Vec3{ 0.0f, 1.0f, 0.0f }
		: Vec3{ 1.0f, 0.0f, 0.0f };

		Vec3 tangent = Cross(up, center).Normalized();

		if (Dot(tangent, tangent) <= Epsilon)
			return center;

		Vec3 bitangent = Cross(center, tangent).Normalized();

		float angle = 2.0f * Pi * float(index) / float(count - 1);
		Vec3 radial =
			tangent * std::cos(angle) +
			bitangent * std::sin(angle);

		return (
			center * std::cos(angleRad) +
			radial * std::sin(angleRad)).Normalized();
	}



	MergeEnergyFn SelectMergeEnergy(
		VoxelLightMergeMode mode,
		bool normalMode)
	{
		if (normalMode)
		{
			switch (mode)
			{
			case VoxelLightMergeMode::Add:
				return &MergeEnergy<VoxelLightMergeMode::Add, true>;

			case VoxelLightMergeMode::AddPreserveDir:
				return &MergeEnergy<VoxelLightMergeMode::AddPreserveDir, true>;

			case VoxelLightMergeMode::MaxSample:
				return &MergeEnergy<VoxelLightMergeMode::MaxSample, true>;
			}
		}
		else
		{
			switch (mode)
			{
			case VoxelLightMergeMode::Add:
				return &MergeEnergy<VoxelLightMergeMode::Add, false>;

			case VoxelLightMergeMode::AddPreserveDir:
				return &MergeEnergy<VoxelLightMergeMode::AddPreserveDir, false>;

			case VoxelLightMergeMode::MaxSample:
				return &MergeEnergy<VoxelLightMergeMode::MaxSample, false>;
			}
		}

		return nullptr;
	}

	MergeVoxelLightDataFn SelectMergeVoxelLightData(
		VoxelLightMergeMode mode,
		bool normalMode)
	{
		if (normalMode)
		{
			switch (mode)
			{
			case VoxelLightMergeMode::Add:
				return &MergeVoxelLightData<VoxelLightMergeMode::Add, true>;

			case VoxelLightMergeMode::AddPreserveDir:
				return &MergeVoxelLightData<VoxelLightMergeMode::AddPreserveDir, true>;

			case VoxelLightMergeMode::MaxSample:
				return &MergeVoxelLightData<VoxelLightMergeMode::MaxSample, true>;
			}
		}
		else
		{
			switch (mode)
			{
			case VoxelLightMergeMode::Add:
				return &MergeVoxelLightData<VoxelLightMergeMode::Add, false>;

			case VoxelLightMergeMode::AddPreserveDir:
				return &MergeVoxelLightData<VoxelLightMergeMode::AddPreserveDir, false>;

			case VoxelLightMergeMode::MaxSample:
				return &MergeVoxelLightData<VoxelLightMergeMode::MaxSample, false>;
			}
		}

		return nullptr;
	}

}

VoxelLightBakeParams::VoxelLightBakeParams() {

	Mode = LightTrackMode::Full;

	EnergyThreshold = 0.0001f;

	ThreadCount = 0;
	RaySubsample = 1;
	InitiateLightField = false;
	NormalizeDir = false;
	IntersectMode = RayIntersectionMode::Geometry;

	Blur.Passes = 0;
	Blur.Strength = 0.35f;
	Blur.ColorOnly = true;

	Bounce.MaxCount = 4;
	Bounce.RayCount = 4;
	Bounce.RayDecay = 0.5f;
	Bounce.CenterWeight = 0.7f;
	Bounce.NormalWeight = 0.75f;
	Bounce.ConeMaxAngle = 70.0f;

	SmoothDir.Iterations = 32;
	SmoothDir.Smoothness = 0.05f;
	SmoothDir.Relaxation = 0.75f;
	SmoothDir.MaxSlope = 1.0f;

	DirCollapseMode = DirectionCollapseMode::Add;

	RayMergeMode = VoxelLightMergeMode::MaxSample;
	GenMergeMode = VoxelLightMergeMode::AddPreserveDir;
	LightMergeMode = VoxelLightMergeMode::Add;

	Recovery = { LightCurveType::Quadratic, 2, 1 };
}

VoxelRayMarcher::VoxelRayMarcher() {
	_baker = nullptr; _workerIndex = -1;

	_ray = {};
	_ray.LastHitVoxel = -1;
	_ray.BounceCount = 0;
	_step = nullptr;
}

void VoxelRayMarcher::SetContext(VoxelLightBaker* baker, int32_t workerIndex)
{
	_baker = baker;
	_workerIndex = workerIndex;	
	
	const VoxelLightBakeParams& params = baker->_params;

	_step = SelectStep(
		params.Mode,
		params.IntersectMode,
		params.DirCollapseMode == DirectionCollapseMode::Normal,
		params.RayMergeMode);
}

void VoxelRayMarcher::Prepare(int32_t voxelCount)
{
	_local.Contribution.Cells.clear();
	_local.CellSlots.assign(voxelCount, -1);
	_local.TouchedVoxels.clear();
	_local.Contribution.Cells.reserve(voxelCount);
	_ray.IsAlive = true;
}


bool VoxelRayMarcher::CreateRay(const VoxelLightRay& ray, int32_t generation)
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

	if (!WorldToVoxel(_baker->_grid, _ray.Origin, _ray.Cell))
	{
		Vec3 gridMax{
			_baker->_grid.Origin.X + float(_baker->_grid.Size.X) * _baker->_grid.VoxelSize,
			_baker->_grid.Origin.Y + float(_baker->_grid.Size.Y) * _baker->_grid.VoxelSize,
			_baker->_grid.Origin.Z + float(_baker->_grid.Size.Z) * _baker->_grid.VoxelSize
		};

		float enterT;
		float exitT;

		if (!RayAabbInterval(
			_baker->_grid.Origin,
			gridMax,
			_ray.Origin,
			_ray.Direction,
			enterT,
			exitT) ||
			exitT < 0.0f)
			return false;

		enterT = std::max(enterT, 0.0f);

		_ray.Position = _ray.Origin + _ray.Direction * (enterT + Epsilon);

		if (!WorldToVoxel(_baker->_grid, _ray.Position, _ray.Cell))
			return false;

		_ray.Distance = (_ray.Position - _ray.Origin).Length();
	}


	return true;
}

void VoxelRayMarcher::TraceRay(const VoxelLightRay& ray, int32_t generation) {

	if (!CreateRay(ray, generation))
		return;

	while (StepImpl()) {}
}

void VoxelRayMarcher::TraceRange(int32_t startRay, int32_t endRay, int32_t generation) {

	ClearContribution();

	const int32_t rayPrealloc = int32_t(_baker->_voxelCount * 0.005);

	_local.Contribution.Cells.reserve(std::min(_baker->_voxelCount, endRay - startRay));
	_local.TouchedVoxels.reserve(rayPrealloc);

	_nextRays.clear();

	for (int32_t i = startRay; i < endRay; ++i)
		TraceRay(_baker->_rays[i], generation);
}



void VoxelRayMarcher::GetDebugState(
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
	state.Position = _ray.Origin + _ray.Direction * _ray.Distance;
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

	const VoxelData& voxel = _baker->_scene[_ray.LastAffectedVoxel];

	state.LastVoxel = voxel;

	for (const VoxelLightCell& cell : _local.Contribution.Cells)
	{
		if (cell.Index != _ray.LastAffectedVoxel)
			continue;

		state.LastLightData = cell.Data;
		break;
	}
}


FORCE_INLINE bool VoxelRayMarcher::MoveToNextVoxel()
{
	Vec3I old = _ray.Cell;
	int32_t oldIndex = VoxelIndex(_baker->_grid, old);

	float voxelStep = _baker->_grid.VoxelSize * 0.5f;

	Vec3 origin = _ray.LightState == VoxelLightState::Occlusion
		? _ray.OcclusionOrigin
		: _ray.Origin;

	while (true)
	{
		float nextDistance = _ray.Distance + voxelStep;
		Vec3 position = origin + _ray.Direction * nextDistance;

		Vec3I next;
		if (!WorldToVoxel(_baker->_grid, position, next))
		{
			_ray.IsAlive = false;
			return false;
		}

		int32_t nextIndex = VoxelIndex(_baker->_grid, next);

		if (nextIndex == oldIndex)
		{
			_ray.Distance = nextDistance;
			continue;
		}

		_ray.Position = position;
		_ray.Distance = nextDistance;
		_ray.OriginStep++;
		_ray.Cell = next;

		return true;
	}
}

VoxelRayMarcher::StepFn VoxelRayMarcher::SelectStep(
	LightTrackMode mode,
	RayIntersectionMode intersectMode,
	bool normalMode,
	VoxelLightMergeMode rayMergeMode)
{
#define SELECT_STEP(MODE, INTERSECT, NORMAL)									\
	switch (rayMergeMode)														\
	{																			\
	case VoxelLightMergeMode::Add:												\
		return &VoxelRayMarcher::Step<											\
			MODE, INTERSECT, NORMAL, VoxelLightMergeMode::Add>;					\
	case VoxelLightMergeMode::AddPreserveDir:									\
		return &VoxelRayMarcher::Step<											\
			MODE, INTERSECT, NORMAL, VoxelLightMergeMode::AddPreserveDir>;		\
	case VoxelLightMergeMode::MaxSample:										\
		return &VoxelRayMarcher::Step<											\
			MODE, INTERSECT, NORMAL, VoxelLightMergeMode::MaxSample>;			\
	}

	if (mode == LightTrackMode::Full)
	{
		if (intersectMode == RayIntersectionMode::Direction)
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::Full, RayIntersectionMode::Direction, true)
			else
				SELECT_STEP(LightTrackMode::Full, RayIntersectionMode::Direction, false)
		}
		else
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::Full, RayIntersectionMode::Geometry, true)
			else
				SELECT_STEP(LightTrackMode::Full, RayIntersectionMode::Geometry, false)
		}
	}
	else if (mode == LightTrackMode::Occlusions)
	{
		if (intersectMode == RayIntersectionMode::Direction)
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::Occlusions, RayIntersectionMode::Direction, true)
			else
				SELECT_STEP(LightTrackMode::Occlusions, RayIntersectionMode::Direction, false)
		}
		else
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::Occlusions, RayIntersectionMode::Geometry, true)
			else
				SELECT_STEP(LightTrackMode::Occlusions, RayIntersectionMode::Geometry, false)
		}
	}
	else
	{
		if (intersectMode == RayIntersectionMode::Direction)
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::OcclusionsOnly, RayIntersectionMode::Direction, true)
			else
				SELECT_STEP(LightTrackMode::OcclusionsOnly, RayIntersectionMode::Direction, false)
		}
		else
		{
			if (normalMode)
				SELECT_STEP(LightTrackMode::OcclusionsOnly, RayIntersectionMode::Geometry, true)
			else
				SELECT_STEP(LightTrackMode::OcclusionsOnly, RayIntersectionMode::Geometry, false)
		}
	}

#undef SELECT_STEP

	return nullptr;
}

template<
	LightTrackMode Mode,
	RayIntersectionMode IntersectMode,
	bool NormalMode,
	VoxelLightMergeMode RayMergeMode>
bool VoxelRayMarcher::Step()
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
	const int32_t incomingFace = IncomingBucketFaceFromDirection(_ray.Direction);
	int32_t outgoingFace = incomingFace ^ 1;

	int32_t slot = -1;

	if (Mode != LightTrackMode::OcclusionsOnly ||
		_ray.LightState == VoxelLightState::Occlusion)
	{
		bool isNewCell;

		slot = ReserveCell(_local, index, isNewCell);

		_ray.LastAffectedVoxel = index;

#ifdef _DEBUG
		VoxelLightData& data = _local.Contribution.Cells[slot].Data;

		if (_ray.BounceCount > 0 || !params.InitiateLightField)
		{
			MergeEnergy<RayMergeMode, NormalMode>(
				data.Faces[incomingFace].Incoming,
				MakeEnergy<NormalMode>(stepEnergy, _ray.Direction),
				data.Faces[incomingFace].State,
				_ray.LightState);
		}
#endif
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

				if (!RayVoxelSurfacePoint(
					_ray.Position,
					grid.VoxelSize,
					_ray.Origin,
					_ray.Direction,
					bounceOrigin))
				{
					bounceOrigin = _ray.Position;
				}

				_ray.Position = bounceOrigin;

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

					for (int32_t i = 0; i < sideCount; ++i)
					{
						const Vec3 sideDir = ConeDirection(
							bounceDir,
							coneAngle,
							i,
							rayCount);

						pushBounceRay(sideDir, sideEnergy);
					}
				}
			}

			_ray.IsAlive = false;
		}
	}
	else
	{
		_ray.LastAffectedFace = outgoingFace;

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

		if (writeEnergy)
		{
			VoxelLightData& data =
				_local.Contribution.Cells[slot].Data;

			const Vec3 activeDir = NormalMode
				? _ray.DirectionNormal
				: _ray.Direction;

			if (NormalMode)
				outgoingFace =
				OutgoingBucketFaceFromDirection(_ray.DirectionNormal);

			if (IntersectMode == RayIntersectionMode::Direction)
			{
				MergeEnergy<RayMergeMode, NormalMode>(
					data.Faces[outgoingFace].Outgoing,
					MakeEnergy<NormalMode>(stepEnergy, activeDir),
					data.Faces[outgoingFace].State,
					_ray.LightState);

				data.Faces[outgoingFace].State =
					_ray.LightState;
			}
			else
			{
				VoxelFaceWeight faces[3] = { };

				int32_t intCount = RayVoxelExitFaceWeights(
					VoxelCenter(grid, _ray.Cell),
					_baker->GetVoxelSize(),
					_ray.Position,
					activeDir,
					faces,
					RayMergeMode == VoxelLightMergeMode::MaxSample);
			
				if (intCount == 0)
				{
					faces[0].Face = outgoingFace;
					faces[0].Weight = 1.0f;
					intCount = 1;
				}
				
				for (int32_t i = 0; i < intCount; ++i)
				{
					const int32_t face = faces[i].Face;
					const Vec3 faceEnergy = stepEnergy * faces[i].Weight;

					MergeEnergy<RayMergeMode, NormalMode>(
						data.Faces[face].Outgoing,
						MakeEnergy<NormalMode>(faceEnergy, activeDir),
						data.Faces[face].State,
						_ray.LightState);

					data.Faces[face].State = _ray.LightState;
				}
			}
		}
	}

	if (_ray.IsAlive)
		MoveToNextVoxel();

	return _ray.IsAlive;
}

void VoxelRayMarcher::ClearContribution() {

	for (int32_t index : _local.TouchedVoxels)
		_local.CellSlots[index] = -1;

	_local.TouchedVoxels.clear();
	_local.Contribution.Cells.clear();

}

VoxelLightBaker::VoxelLightBaker()
	: VoxelLightBaker(VoxelLightBakeParams())
{
}

VoxelLightBaker::VoxelLightBaker(const VoxelLightBakeParams& params)
{
	_grid = {};
	_voxelCount = 0;

	_mergeEnergyLight = nullptr;

	_mergeVoxelLightDataLight = nullptr;
	_mergeVoxelLightDataGen = nullptr;
	_mergeVoxelLightDataRay = nullptr;

	SetParams(params);
}

void VoxelLightBaker::SetParams(const VoxelLightBakeParams& params)
{
	_params = params;

	bool normalMode = _params.DirCollapseMode == DirectionCollapseMode::Normal;

	_mergeEnergyLight = SelectMergeEnergy(
		_params.LightMergeMode,
		normalMode);

	_mergeVoxelLightDataLight = SelectMergeVoxelLightData(
		_params.LightMergeMode,
		normalMode);

	_mergeVoxelLightDataGen = SelectMergeVoxelLightData(
		_params.GenMergeMode,
		normalMode);

	_mergeVoxelLightDataRay = SelectMergeVoxelLightData(
		_params.RayMergeMode,
		normalMode);

}

void VoxelLightBaker::SetGrid(const VoxelGridDesc& grid)
{
	_grid = grid;
	_voxelCount = grid.Size.X * grid.Size.Y * grid.Size.Z;

	_scene.assign(_voxelCount, VoxelData{});
	_lightData.assign(_voxelCount, VoxelLightData{});

	_currentMerge.CellSlots.assign(_voxelCount, -1);
	_currentMerge.TouchedVoxels.clear();

	_currentMerge.Contribution.Cells.reserve(_voxelCount);
	_currentMerge.TouchedVoxels.reserve(_voxelCount);

	int32_t threadCount = _params.ThreadCount;

	if (threadCount <= 0)
		threadCount = int32_t(std::max(1u, std::thread::hardware_concurrency()));

	_marchers.resize(threadCount);

}


void VoxelLightBaker::ClearScene()
{
	std::fill(_scene.begin(), _scene.end(), VoxelData{});
}

void VoxelLightBaker::AddMesh(const Vec3I& origin, const Vec3I& size, const VoxelData* voxels, const VoxelMeshResolvedFace* faces, int32_t faceCount) {

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

void VoxelLightBaker::AddGpuMeshFaces(
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

void VoxelLightBaker::BakeGeneratedRays(VoxelLightContribution& contribution)
{
	for (int32_t i = 0; i < _params.ThreadCount; ++i)
	{
		_marchers[i].SetContext(this, i);
		_marchers[i].Prepare(_voxelCount);
	}

	for (int32_t generation = 0; generation < _params.Bounce.MaxCount; ++generation)
	{
		if (_rays.empty())
			break;

		VoxelLightContribution generationContribution;

		TraceRays(
			generationContribution,
			_nextRays,
			generation);

		MergeContribution(
			contribution,
			_currentMerge,
			generationContribution,
			_mergeVoxelLightDataGen);

		_rays.swap(_nextRays);
		_nextRays.clear();
	}

	if (_params.InitiateLightField)
		CleanupUnvisitedFaces(contribution);
}

void VoxelLightBaker::BakePointLight(
	const PointLight& light,
	VoxelLightContribution& contribution)
{
	contribution.Cells.clear();
	ClearMergeState(_currentMerge);

	Vec3 energy = light.Color * light.Intensity;

	if (!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillPointLightContribution(light, contribution);

	if (_params.Bounce.MaxCount > 0)
	{
		GeneratePointLightRays(light, false);
		BakeGeneratedRays(contribution);

		if (contribution.Cells.size() < _voxelCount)
		{
			GeneratePointLightRays(light, true);
			BakeGeneratedRays(contribution);
		}
	}

	ClearMergeState(_currentMerge);
}

void VoxelLightBaker::BakeDirectionalLight(
	const DirectionalLight& light,
	VoxelLightContribution& contribution)
{
	contribution.Cells.clear();
	ClearMergeState(_currentMerge);

	Vec3 direction = light.Direction.Normalized();
	Vec3 energy = light.Color * light.Intensity;

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillDirectionalLightContribution(light, contribution);

	if (_params.Bounce.MaxCount > 0)
	{
		GenerateDirectionalLightRays(light);
		BakeGeneratedRays(contribution);
	}

	ClearMergeState(_currentMerge);
}

void VoxelLightBaker::BakeSpotLight(
	const SpotLight& light,
	VoxelLightContribution& contribution)
{
	contribution.Cells.clear();
	ClearMergeState(_currentMerge);

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
		PrefillSpotLightContribution(light, contribution);

	if (_params.Bounce.MaxCount > 0)
	{
		GenerateSpotLightRays(light);
		BakeGeneratedRays(contribution);
	}

	ClearMergeState(_currentMerge);
}

void VoxelLightBaker::ClearLightField()
{
	std::fill(_lightData.begin(), _lightData.end(), VoxelLightData{});
}

void VoxelLightBaker::AccumulateLight(const VoxelLightContribution& contribution) {

	for (const VoxelLightCell& cell : contribution.Cells) {

		if (cell.Index < 0 || cell.Index >= _voxelCount)
			continue;

		_mergeVoxelLightDataLight(_lightData[cell.Index], cell.Data);
	}
}

VoxelLightField& VoxelLightBaker::GetLightField()
{
	BuildLightField();
	return _field;
}

void VoxelLightBaker::PrefillPointLightContribution(
	const PointLight& light,
	VoxelLightContribution& contribution)
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	Vec3 lightEnergy = light.Color * light.Intensity;

	const bool normalMode = _params.DirCollapseMode == DirectionCollapseMode::Normal;

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

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				_mergeEnergyLight(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction, normalMode),
					VoxelLightState::Light,
					VoxelLightState::Light);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution, _mergeVoxelLightDataLight);
}

void VoxelLightBaker::PrefillDirectionalLightContribution(
	const DirectionalLight& light,
	VoxelLightContribution& contribution)
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	Vec3 direction = light.Direction.Normalized();
	Vec3 lightEnergy = DirectionalLightEnergy(light);

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(lightEnergy, _params.EnergyThreshold))
	{
		return;
	}

	Vec3 right;
	Vec3 up;
	DirectionBasis(direction, right, up);

	const bool normalMode = _params.DirCollapseMode == DirectionCollapseMode::Normal;

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

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				_mergeEnergyLight(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction, normalMode),
					VoxelLightState::Light,
					VoxelLightState::Light);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution, _mergeVoxelLightDataLight);
}

void VoxelLightBaker::PrefillSpotLightContribution(
	const SpotLight& light,
	VoxelLightContribution& contribution)
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	Vec3 lightEnergy = light.Color * light.Intensity;
	Vec3 lightDirection = light.Direction.Normalized();

	const bool normalMode = _params.DirCollapseMode == DirectionCollapseMode::Normal;

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

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				_mergeEnergyLight(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction, normalMode),
					VoxelLightState::Light,
					VoxelLightState::Light);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution, _mergeVoxelLightDataLight);
}

void VoxelLightBaker::GeneratePointLightRays(const PointLight& light, bool fillMode)
{
	_rays.clear();

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 rayEnergy = light.Color * light.Intensity;

	if (_params.RayMergeMode == VoxelLightMergeMode::Add)
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

void VoxelLightBaker::GenerateDirectionalLightRays(const DirectionalLight& light)
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

	if (_params.RayMergeMode == VoxelLightMergeMode::Add)
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

			Vec3 entry =
				emissionPoint + direction * entryDistance;

			Vec3 energy =
				rayEnergy *
				LightFalloffAtDistance(light.Falloff, entryDistance);

			if (!HasEnergy(energy, _params.EnergyThreshold))
				return;

			VoxelLightRay ray{};
			ray.Position = entry;
			ray.Direction = direction;
			ray.DirectionNormal = direction;
			ray.Energy = energy;
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

void VoxelLightBaker::GenerateSpotLightRays(const SpotLight& light)
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

			if (_params.RayMergeMode == VoxelLightMergeMode::Add)
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

void VoxelLightBaker::CleanupUnvisitedFaces(VoxelLightContribution& contribution)
{
	(void)contribution;
}

void VoxelLightBaker::TraceRays(
	VoxelLightContribution& contribution,
	std::vector<VoxelLightRay>& nextRays,
	int32_t generation)
{
	contribution.Cells.clear();
	nextRays.clear();

	ContributionMergeState mergeState;
	mergeState.CellSlots.assign(_voxelCount, -1);
	mergeState.TouchedVoxels.clear();

	int32_t rayCount = int32_t(_rays.size());

	if (rayCount == 0)
		return;

	int32_t threadCount = int32_t(_marchers.size());

	if (threadCount <= 1 || rayCount < threadCount)
	{
		_marchers[0].TraceRange(0, rayCount, generation);

		MergeContribution(
			contribution,
			mergeState,
			_marchers[0].Contribution(),
			_mergeVoxelLightDataRay);

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

		threads.emplace_back([this, &contribution, &mergeState, &nextRays, i, start, end, generation]()
			{
				_marchers[i].TraceRange(start, end, generation);

				std::lock_guard<std::mutex> lock(_mergeLock);

				MergeContribution(
					contribution,
					mergeState,
					_marchers[i].Contribution(),
					_mergeVoxelLightDataRay);

				const std::vector<VoxelLightRay>& workerNext = _marchers[i].NextRays();
				nextRays.insert(nextRays.end(), workerNext.begin(), workerNext.end());
			});

		rangeStart = rangeEnd;
	}

	for (std::thread& thread : threads)
		thread.join();
}

void VoxelLightBaker::MergeContribution(
	VoxelLightContribution& target,
	ContributionMergeState& mergeState,
	const VoxelLightContribution& source,
	MergeVoxelLightDataFn mergeVoxelLightData)
{
	for (const VoxelLightCell& sourceCell : source.Cells)
	{
		if (sourceCell.Index < 0 || sourceCell.Index >= _voxelCount)
			continue;

		int32_t slot = mergeState.CellSlots[sourceCell.Index];

		if (slot < 0)
		{
			slot = int32_t(target.Cells.size());

			mergeState.CellSlots[sourceCell.Index] = slot;
			mergeState.TouchedVoxels.push_back(sourceCell.Index);

			target.Cells.push_back(sourceCell);
		}
		else
		{
			mergeVoxelLightData(
				target.Cells[slot].Data,
				sourceCell.Data);
		}
	}
}

void VoxelLightBaker::ClearMergeState(ContributionMergeState& mergeState) {
	for (int32_t index : mergeState.TouchedVoxels)mergeState.CellSlots[index] = -1;

	mergeState.TouchedVoxels.clear();

}



int32_t BuildGaussianKernel3x3x3(BlurSample* samples)
{
	static const int32_t offsets[3] = { -1, 0, 1 };
	static const float weights[3] = { 1.0f, 2.0f, 1.0f };

	int32_t count = 0;

	for (int32_t z = 0; z < 3; ++z)
	{
		for (int32_t y = 0; y < 3; ++y)
		{
			for (int32_t x = 0; x < 3; ++x)
			{
				BlurSample& sample = samples[count++];

				sample.Dx = offsets[x];
				sample.Dy = offsets[y];
				sample.Dz = offsets[z];

				sample.Weight =
					weights[x] *
					weights[y] *
					weights[z] *
					(1.0f / 64.0f);
			}
		}
	}

	return count;
}

void VoxelLightBaker::BlurLightField()
{
	bool colorOnly = _params.Blur.ColorOnly;
	float strength = std::clamp(_params.Blur.Strength, 0.0f, 1.0f);
	int32_t passes = std::max(0, _params.Blur.Passes);

	if (strength <= 0.0f || passes <= 0)
		return;

	int32_t count = _field.Size.X * _field.Size.Y * _field.Size.Z;

	std::vector<Vec3> tempColor(count);
	std::vector<Vec3> tempDirection(count);

	BlurSample samples[27];
	int32_t sampleCount = BuildGaussianKernel3x3x3(samples);

	for (int32_t pass = 0; pass < passes; ++pass)
	{
		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			std::vector<Vec3>& colors = _field.Color[face];
			std::vector<Vec3>& directions = _field.Direction[face];

			for (int32_t z = 0; z < _field.Size.Z; ++z)
			{
				for (int32_t y = 0; y < _field.Size.Y; ++y)
				{
					for (int32_t x = 0; x < _field.Size.X; ++x)
					{
						int32_t index = FieldIndex(_field, { x, y, z });

						Vec3 colorSum = Vec3{};
						Vec3 dirSum = Vec3{};
						float weightSum = 0.0f;

						for (int32_t i = 0; i < sampleCount; ++i)
						{
							const BlurSample& sample = samples[i];

							int32_t nx = x + sample.Dx;
							int32_t ny = y + sample.Dy;
							int32_t nz = z + sample.Dz;

							if (nx < 0 || ny < 0 || nz < 0 ||
								nx >= _field.Size.X ||
								ny >= _field.Size.Y ||
								nz >= _field.Size.Z)
							{
								continue;
							}

							int32_t ni = FieldIndex(_field, { nx, ny, nz });

							colorSum += colors[ni] * sample.Weight;
							weightSum += sample.Weight;

							if (!colorOnly)
								dirSum += directions[ni] * sample.Weight;
						}

						if (weightSum > Epsilon)
						{
							Vec3 blurColor = colorSum / weightSum;
							tempColor[index] = Lerp(colors[index], blurColor, strength);

							if (!colorOnly)
							{
								Vec3 blurDir = dirSum / weightSum;
								tempDirection[index] = Lerp(directions[index], blurDir, strength);
							}

						}
						else
						{
							tempColor[index] = colors[index];

							if (!colorOnly)
								tempDirection[index] = directions[index];
						}
					}
				}
			}

			colors.swap(tempColor);

			if (!colorOnly)
				directions.swap(tempDirection);
		}
	}
}


void VoxelLightBaker::ReconstructDirectionSurfaceForFace(
	int32_t face)
{
	if (face < 0 || face >= VOXEL_LIGHT_FACE_COUNT)
		return;

	auto iterations = std::max(1, _params.SmoothDir.Iterations);
	auto smoothness = std::max(0.0f, _params.SmoothDir.Smoothness);
	auto relaxation = std::clamp(_params.SmoothDir.Relaxation, 0.0f, 1.0f);
	auto maxSlope = std::max(0.001f, _params.SmoothDir.MaxSlope);

	std::vector<Vec3>& colors = _field.Color[face];
	std::vector<Vec3>& directions = _field.Direction[face];

	Vec3 n;
	Vec3 t;
	Vec3 b;

	int32_t width;
	int32_t height;
	int32_t layers;

	auto indexOf = [&](int32_t u, int32_t v, int32_t layer) -> int32_t
		{
			switch (face)
			{
			case 0:
			case 1:
				return FieldIndex(_field, { layer, v, u }); // x = layer, y = v, z = u

			case 2:
			case 3:
				return FieldIndex(_field, { u, layer, v }); // x = u, y = layer, z = v

			default:
				return FieldIndex(_field, { u, v, layer }); // x = u, y = v, z = layer
			}
		};

	switch (face)
	{
	case 0:
		n = Vec3{ -1.0f, 0.0f, 0.0f };
		t = Vec3{ 0.0f, 0.0f, 1.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.Size.Z;
		height = _field.Size.Y;
		layers = _field.Size.X;
		break;

	case 1:
		n = Vec3{ 1.0f, 0.0f, 0.0f };
		t = Vec3{ 0.0f, 0.0f, 1.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.Size.Z;
		height = _field.Size.Y;
		layers = _field.Size.X;
		break;

	case 2:
		n = Vec3{ 0.0f, -1.0f, 0.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 0.0f, 1.0f };
		width = _field.Size.X;
		height = _field.Size.Z;
		layers = _field.Size.Y;
		break;

	case 3:
		n = Vec3{ 0.0f, 1.0f, 0.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 0.0f, 1.0f };
		width = _field.Size.X;
		height = _field.Size.Z;
		layers = _field.Size.Y;
		break;

	case 4:
		n = Vec3{ 0.0f, 0.0f, -1.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.Size.X;
		height = _field.Size.Y;
		layers = _field.Size.Z;
		break;

	default:
		n = Vec3{ 0.0f, 0.0f, 1.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.Size.X;
		height = _field.Size.Y;
		layers = _field.Size.Z;
		break;
	}

	int32_t sliceCount = width * height;

	std::vector<float> slopeU(sliceCount);
	std::vector<float> slopeV(sliceCount);
	std::vector<float> weight(sliceCount);
	std::vector<float> heightField(sliceCount);

	auto sliceIndex = [&](int32_t u, int32_t v) -> int32_t
		{
			return v * width + u;
		};

	for (int32_t layer = 0; layer < layers; ++layer)
	{
		std::fill(slopeU.begin(), slopeU.end(), 0.0f);
		std::fill(slopeV.begin(), slopeV.end(), 0.0f);
		std::fill(weight.begin(), weight.end(), 0.0f);
		std::fill(heightField.begin(), heightField.end(), 0.0f);

		for (int32_t v = 0; v < height; ++v)
		{
			for (int32_t u = 0; u < width; ++u)
			{
				int32_t si = sliceIndex(u, v);
				int32_t fi = indexOf(u, v, layer);

				Vec3 color = colors[fi];
				Vec3 dir = directions[fi];

				float energy = EnergyScore(color);
				float dirLenSq = Dot(dir, dir);

				if (energy <= Epsilon || dirLenSq <= Epsilon)
					continue;

				float dn = Dot(dir, n);

				if (std::fabs(dn) <= Epsilon)
					continue;

				float su = Dot(dir, t) / dn;
				float sv = Dot(dir, b) / dn;

				slopeU[si] = std::clamp(su, -maxSlope, maxSlope);
				slopeV[si] = std::clamp(sv, -maxSlope, maxSlope);
				weight[si] = energy;
			}
		}

		for (int32_t it = 0; it < iterations; ++it)
		{
			for (int32_t v = 0; v < height; ++v)
			{
				for (int32_t u = 0; u < width; ++u)
				{
					int32_t si = sliceIndex(u, v);

					float sum = 0.0f;
					float sumWeight = 0.0f;

					auto addCandidate = [&](int32_t ni, float candidate, float edgeWeight)
						{
							float w = smoothness + edgeWeight;

							if (w <= 0.0f)
								return;

							sum += candidate * w;
							sumWeight += w;
						};

					if (u + 1 < width)
					{
						int32_t ni = sliceIndex(u + 1, v);
						float edgeWeight = 0.5f * (weight[si] + weight[ni]);
						float edgeSlope = 0.5f * (slopeU[si] + slopeU[ni]);

						addCandidate(ni, heightField[ni] - edgeSlope, edgeWeight);
					}

					if (u > 0)
					{
						int32_t ni = sliceIndex(u - 1, v);
						float edgeWeight = 0.5f * (weight[si] + weight[ni]);
						float edgeSlope = 0.5f * (slopeU[si] + slopeU[ni]);

						addCandidate(ni, heightField[ni] + edgeSlope, edgeWeight);
					}

					if (v + 1 < height)
					{
						int32_t ni = sliceIndex(u, v + 1);
						float edgeWeight = 0.5f * (weight[si] + weight[ni]);
						float edgeSlope = 0.5f * (slopeV[si] + slopeV[ni]);

						addCandidate(ni, heightField[ni] - edgeSlope, edgeWeight);
					}

					if (v > 0)
					{
						int32_t ni = sliceIndex(u, v - 1);
						float edgeWeight = 0.5f * (weight[si] + weight[ni]);
						float edgeSlope = 0.5f * (slopeV[si] + slopeV[ni]);

						addCandidate(ni, heightField[ni] + edgeSlope, edgeWeight);
					}

					if (sumWeight > Epsilon)
					{
						float solved = sum / sumWeight;
						heightField[si] = heightField[si] + (solved - heightField[si]) * relaxation;
					}
				}
			}

			float mean = 0.0f;

			for (float h : heightField)
				mean += h;

			mean /= float(sliceCount);

			for (float& h : heightField)
				h -= mean;
		}

		for (int32_t v = 0; v < height; ++v)
		{
			for (int32_t u = 0; u < width; ++u)
			{
				int32_t si = sliceIndex(u, v);
				int32_t fi = indexOf(u, v, layer);

				float du;

				if (u > 0 && u + 1 < width)
					du = (heightField[sliceIndex(u + 1, v)] - heightField[sliceIndex(u - 1, v)]) * 0.5f;
				else if (u + 1 < width)
					du = heightField[sliceIndex(u + 1, v)] - heightField[si];
				else if (u > 0)
					du = heightField[si] - heightField[sliceIndex(u - 1, v)];
				else
					du = 0.0f;

				float dv;

				if (v > 0 && v + 1 < height)
					dv = (heightField[sliceIndex(u, v + 1)] - heightField[sliceIndex(u, v - 1)]) * 0.5f;
				else if (v + 1 < height)
					dv = heightField[sliceIndex(u, v + 1)] - heightField[si];
				else if (v > 0)
					dv = heightField[si] - heightField[sliceIndex(u, v - 1)];
				else
					dv = 0.0f;

				du = std::clamp(du, -maxSlope, maxSlope);
				dv = std::clamp(dv, -maxSlope, maxSlope);

				Vec3 solvedDir = (n + t * du + b * dv).Normalized();

				float oldLen = directions[fi].Length();
				float energy = EnergyScore(colors[fi]);
				float outLen = oldLen > Epsilon ? oldLen : energy;

				if (outLen > Epsilon)
					directions[fi] = solvedDir * outLen;
				else
					directions[fi] = Vec3{};
			}
		}
	}
}

void VoxelLightBaker::BuildLightField() {

	_field.Size = _grid.Size;

	for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
	{
		_field.Color[face].resize(_voxelCount);
		_field.Direction[face].resize(_voxelCount);
	}

	const auto colMode = _params.DirCollapseMode;
	const auto normDir = _params.NormalizeDir;

	for (int32_t i = 0; i < _voxelCount; ++i)
	{
		const VoxelLightData& src = _lightData[i];

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			const VoxelLightEnergy& outgoing = src.Faces[face].Outgoing;

			_field.Color[face][i] = outgoing.Energy;

			Vec3 outDir;

			if (colMode == DirectionCollapseMode::Add)
			{
				outDir = outgoing.DirectionR + outgoing.DirectionG + outgoing.DirectionB;
			}
			else if (colMode == DirectionCollapseMode::Luminance)
			{
				outDir =
					outgoing.DirectionR * 0.2126f +
					outgoing.DirectionG * 0.7152f +
					outgoing.DirectionB * 0.0722f;
			}
			else
			{
				outDir = outgoing.DirectionN;
			}


			if (normDir)
				outDir = outDir.Normalized();

			_field.Direction[face][i] = outDir;
		}
	}

	if (_params.SmoothDir.Iterations > 0)
	{
		for (int i = 0; i < 6; i++)
			ReconstructDirectionSurfaceForFace(i);
	};

	if (_params.Blur.Passes > 0)
		BlurLightField();

}
