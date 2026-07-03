#include "pch.h"



namespace {
	constexpr float Pi = 3.14159265358979323846f; constexpr float Epsilon = 1e-5f;

	Vec3 Zero3()
	{
		return { 0.0f, 0.0f, 0.0f };
	}

	Vec3 Add(const Vec3& a, const Vec3& b)
	{
		return { a.X + b.X, a.Y + b.Y, a.Z + b.Z };
	}

	Vec3 Sub(const Vec3& a, const Vec3& b)
	{
		return { a.X - b.X, a.Y - b.Y, a.Z - b.Z };
	}

	Vec3 Mul(const Vec3& v, float s)
	{
		return { v.X * s, v.Y * s, v.Z * s };
	}

	Vec3 Mul(const Vec3& a, const Vec3& b)
	{
		return { a.X * b.X, a.Y * b.Y, a.Z * b.Z };
	}

	Vec3 Lerp(const Vec3& a, const Vec3& b, float t)
	{
		return {
			a.X + (b.X - a.X) * t,
			a.Y + (b.Y - a.Y) * t,
			a.Z + (b.Z - a.Z) * t
		};
	}

	float Dot(const Vec3& a, const Vec3& b)
	{
		return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
	}

	float Length(const Vec3& v)
	{
		return std::sqrt(Dot(v, v));
	}

	Vec3 Normalize(const Vec3& v)
	{
		float len = Length(v);

		if (len <= Epsilon)
			return Zero3();

		return Mul(v, 1.0f / len);
	}

	Vec3 Reflect(const Vec3& dir, const Vec3& normal)
	{
		return Sub(dir, Mul(normal, 2.0f * Dot(dir, normal)));
	}

	bool HasEnergy(const Vec3& energy, float threshold)
	{
		return energy.X > threshold ||
			energy.Y > threshold ||
			energy.Z > threshold;
	}

	int32_t VoxelIndex(const VoxelGridDesc& grid, int32_t x, int32_t y, int32_t z)
	{
		return (z * grid.SizeY + y) * grid.SizeX + x;
	}

	Vec3 FaceNormal(int32_t face);

	Vec3 VoxelCenter(const VoxelGridDesc& grid, int32_t x, int32_t y, int32_t z)
	{
		float size = grid.VoxelSize;

		return {
			grid.Origin.X + (float(x) + 0.5f) * size,
			grid.Origin.Y + (float(y) + 0.5f) * size,
			grid.Origin.Z + (float(z) + 0.5f) * size
		};
	}

	Vec3 VoxelFaceCenter(const VoxelGridDesc& grid, int32_t x, int32_t y, int32_t z, int32_t face)
	{
		Vec3 center = VoxelCenter(grid, x, y, z);
		float halfSize = grid.VoxelSize * 0.5f;

		return Add(center, Mul(FaceNormal(face), halfSize));
	}

	float LightFalloffAtDistance(const LightFalloff& falloff, float distance)
	{
		if (falloff.Type == LightFalloffNone)
			return 1.0f;

		if (falloff.Range <= Epsilon)
			return 0.0f;

		float t = 1.0f - distance / falloff.Range;

		if (t <= 0.0f)
			return 0.0f;

		float factor = falloff.Factor;

		if (factor <= Epsilon)
			factor = 1.0f;

		switch (falloff.Type)
		{
		case LightFalloffLinear:
			return t * factor;

		case LightFalloffQuadratic:
			return t * t * factor;

		default:
			return factor;
		}
	}

	Vec3 LightEnergyAtDistance(const PointLight& light, float distance)
	{
		return Mul(Mul(light.Color, light.Intensity), LightFalloffAtDistance(light.Falloff, distance));
	}


	int32_t FieldIndex(const VoxelLightField& field, int32_t x, int32_t y, int32_t z)
	{
		return x + y * field.SizeX + z * field.SizeX * field.SizeY;
	}

	void FillDirectionLine(Vec3* values, int32_t count, int32_t stride)
	{
		int32_t firstValid = -1;

		for (int32_t i = 0; i < count; ++i)
		{
			Vec3& value = values[i * stride];

			if (Dot(value, value) > 1e-12f)
			{
				firstValid = i;
				break;
			}
		}

		if (firstValid < 0)
			return;

		Vec3& firstValue = values[firstValid * stride];

		for (int32_t i = 0; i < firstValid; ++i)
			values[i * stride] = firstValue;

		int32_t left = firstValid;

		while (left < count)
		{
			int32_t right = left + 1;

			while (right < count)
			{
				Vec3& rightValue = values[right * stride];

				if (Dot(rightValue, rightValue) > 1e-12f)
					break;

				right++;
			}

			if (right >= count)
			{
				Vec3& leftValue = values[left * stride];

				for (int32_t i = left + 1; i < count; ++i)
					values[i * stride] = leftValue;

				break;
			}

			Vec3& leftValue = values[left * stride];
			Vec3& rightValue = values[right * stride];

			for (int32_t i = left + 1; i < right; ++i)
			{
				float t = float(i - left) / float(right - left);
				values[i * stride] = Lerp(leftValue, rightValue, t);
			}

			left = right;
		}
	}

	bool IsInsideGrid(const VoxelGridDesc& grid, int32_t x, int32_t y, int32_t z)
	{
		return x >= 0 &&
			y >= 0 &&
			z >= 0 &&
			x < grid.SizeX &&
			y < grid.SizeY &&
			z < grid.SizeZ;
	}

	bool WorldToVoxel(
		const VoxelGridDesc& grid,
		const Vec3& p,
		int32_t& x,
		int32_t& y,
		int32_t& z)
	{
		float invSize = 1.0f / grid.VoxelSize;

		x = int32_t(std::floor((p.X - grid.Origin.X) * invSize));
		y = int32_t(std::floor((p.Y - grid.Origin.Y) * invSize));
		z = int32_t(std::floor((p.Z - grid.Origin.Z) * invSize));

		return IsInsideGrid(grid, x, y, z);
	}

	VoxelLightEnergy MakeEnergy(const Vec3& energy, const Vec3& direction)
	{
		VoxelLightEnergy result{};

		result.Energy = energy;
		result.DirectionR = Mul(direction, energy.X);
		result.DirectionG = Mul(direction, energy.Y);
		result.DirectionB = Mul(direction, energy.Z);

		return result;
	}

	float EnergyScore(const Vec3& energy)
	{
		return energy.X + energy.Y + energy.Z;
	}

	void MergeEnergy(
		VoxelLightEnergy& target,
		const VoxelLightEnergy& source,
		VoxelLightMergeMode mode = VoxelLightMergeMode::Add)
	{
		if (mode == VoxelLightMergeMode::Add)
		{
			target.Energy = Add(target.Energy, source.Energy);
			target.DirectionR = Add(target.DirectionR, source.DirectionR);
			target.DirectionG = Add(target.DirectionG, source.DirectionG);
			target.DirectionB = Add(target.DirectionB, source.DirectionB);
			return;
		}

		if (EnergyScore(source.Energy) > EnergyScore(target.Energy))
			target = source;
	}

	void MergeEnergySlot(
		VoxelLightEnergy* target,
		int16_t slot,
		const VoxelLightEnergy& source,
		VoxelLightMergeMode mode)
	{
		slot = std::min<int32_t>(slot, MAX_BOUNCES - 1);

		MergeEnergy(target[slot], source, mode);
	}

	void MergeFace(
		VoxelLightFace& target,
		const VoxelLightFace& source,
		VoxelLightMergeMode mode = VoxelLightMergeMode::Add)
	{
		for (int32_t slot = 0; slot < MAX_BOUNCES; ++slot)
		{
			MergeEnergy(target.Incoming[slot], source.Incoming[slot], mode);
			MergeEnergy(target.Outgoing[slot], source.Outgoing[slot], mode);
		}

		target.InVisitCount = std::max(target.InVisitCount, source.InVisitCount);
		target.OutVisitCount = std::max(target.OutVisitCount, source.OutVisitCount);
	}

	int32_t BucketFacesFromDirection(
		const Vec3& dir,
		float threshold,
		int32_t* faces)
	{
		float amounts[VOXEL_LIGHT_FACE_COUNT];

		float bestAmount = 0.0f;

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			float amount = std::max(0.0f, -Dot(dir, FaceNormal(face)));

			amounts[face] = amount;
			bestAmount = std::max(bestAmount, amount);
		}

		int32_t count = 0;

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			if (amounts[face] >= bestAmount - threshold)
				faces[count++] = face;
		}

		return count;
	}

	void MergeBucketedEnergy(
		VoxelLightData& data,
		const Vec3& bucketDirection,
		const Vec3& energy,
		const Vec3& storedDirection,
		int32_t generation,
		bool outgoing,
		float threshold,
		VoxelLightMergeMode mode = VoxelLightMergeMode::Add)
	{
		int32_t faces[VOXEL_LIGHT_FACE_COUNT];
		int32_t count = BucketFacesFromDirection(bucketDirection, threshold, faces);

		if (count <= 0)
			return;

		float weight = 1.0f / float(count);
		VoxelLightEnergy source = MakeEnergy(Mul(energy, weight), storedDirection);

		for (int32_t i = 0; i < count; ++i)
		{
			VoxelLightFace& face = data.Faces[faces[i]];

			VoxelLightEnergy* target = outgoing
				? face.Outgoing
				: face.Incoming;

			MergeEnergySlot(target, generation, source, mode);
		}
	}

	void MergeVoxelLightData(
		VoxelLightData& target,
		const VoxelLightData& source,
		VoxelLightMergeMode mode = VoxelLightMergeMode::Add)
	{
		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
			MergeFace(target.Faces[face], source.Faces[face], mode);
	}


	Vec3 FaceNormal(int32_t face)
	{
		switch (face)
		{
		case 0: return { -1.0f,  0.0f,  0.0f };
		case 1: return { 1.0f,  0.0f,  0.0f };
		case 2: return { 0.0f, -1.0f,  0.0f };
		case 3: return { 0.0f,  1.0f,  0.0f };
		case 4: return { 0.0f,  0.0f, -1.0f };
		default: return { 0.0f,  0.0f,  1.0f };
		}
	}

	Vec3 VoxelFaceRayIntersection(
		const VoxelGridDesc& grid,
		int32_t x,
		int32_t y,
		int32_t z,
		int32_t face,
		const Vec3& origin,
		const Vec3& direction)
	{
		Vec3 normal = FaceNormal(face);
		Vec3 faceCenter = VoxelFaceCenter(grid, x, y, z, face);

		float denom = Dot(direction, normal);

		if (std::fabs(denom) <= Epsilon)
			return origin;

		float t = Dot(Sub(faceCenter, origin), normal) / denom;

		if (t < 0.0f)
			t = 0.0f;

		return Add(origin, Mul(direction, t));
	}

	bool SelectVoxelHitFace(
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

			if (faceData.Side == VoxelTriangleSide::Back)
				normal = Mul(normal, -1.0f);

			normal = Normalize(normal);

			if (Dot(normal, normal) <= Epsilon)
				normal = FaceNormal(face);

			float score = Dot(Mul(rayDirection, -1.0f), normal);

			if (score <= 0.0f)
				continue;

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


	bool RayBoxEnter(
		const VoxelGridDesc& grid,
		const Vec3& origin,
		const Vec3& dir,
		float& enterT)
	{
		float minAxis[3]{
			grid.Origin.X,
			grid.Origin.Y,
			grid.Origin.Z
		};

		float maxAxis[3]{
			grid.Origin.X + float(grid.SizeX) * grid.VoxelSize,
			grid.Origin.Y + float(grid.SizeY) * grid.VoxelSize,
			grid.Origin.Z + float(grid.SizeZ) * grid.VoxelSize
		};

		float originAxis[3]{ origin.X, origin.Y, origin.Z };
		float dirAxis[3]{ dir.X, dir.Y, dir.Z };

		float tMin = 0.0f;
		float tMax = 3.402823466e+38f;

		for (int32_t axis = 0; axis < 3; ++axis)
		{
			float d = dirAxis[axis];
			float o = originAxis[axis];

			if (std::fabs(d) <= Epsilon)
			{
				if (o < minAxis[axis] || o > maxAxis[axis])
					return false;

				continue;
			}

			float inv = 1.0f / d;
			float t0 = (minAxis[axis] - o) * inv;
			float t1 = (maxAxis[axis] - o) * inv;

			if (t0 > t1)
				std::swap(t0, t1);

			tMin = std::max(tMin, t0);
			tMax = std::min(tMax, t1);

			if (tMin > tMax)
				return false;
		}

		enterT = tMin;
		return true;
	}

	Vec3 SnapDirectionToGridBoundary(
		const VoxelGridDesc& grid,
		const Vec3& origin,
		const Vec3& direction)
	{
		float bestT = FLT_MAX;

		float minX = grid.Origin.X;
		float minY = grid.Origin.Y;
		float minZ = grid.Origin.Z;

		float maxX = grid.Origin.X + float(grid.SizeX) * grid.VoxelSize;
		float maxY = grid.Origin.Y + float(grid.SizeY) * grid.VoxelSize;
		float maxZ = grid.Origin.Z + float(grid.SizeZ) * grid.VoxelSize;

		if (direction.X > Epsilon)
			bestT = std::min(bestT, (maxX - origin.X) / direction.X);
		else if (direction.X < -Epsilon)
			bestT = std::min(bestT, (minX - origin.X) / direction.X);

		if (direction.Y > Epsilon)
			bestT = std::min(bestT, (maxY - origin.Y) / direction.Y);
		else if (direction.Y < -Epsilon)
			bestT = std::min(bestT, (minY - origin.Y) / direction.Y);

		if (direction.Z > Epsilon)
			bestT = std::min(bestT, (maxZ - origin.Z) / direction.Z);
		else if (direction.Z < -Epsilon)
			bestT = std::min(bestT, (minZ - origin.Z) / direction.Z);

		if (bestT == FLT_MAX || bestT <= Epsilon)
			return direction;

		Vec3 exitPoint = Add(origin, Mul(direction, bestT));

		int32_t tx = std::clamp(
			int32_t(std::floor((exitPoint.X - grid.Origin.X) / grid.VoxelSize)),
			0,
			grid.SizeX - 1);

		int32_t ty = std::clamp(
			int32_t(std::floor((exitPoint.Y - grid.Origin.Y) / grid.VoxelSize)),
			0,
			grid.SizeY - 1);

		int32_t tz = std::clamp(
			int32_t(std::floor((exitPoint.Z - grid.Origin.Z) / grid.VoxelSize)),
			0,
			grid.SizeZ - 1);

		Vec3 target = VoxelCenter(grid, tx, ty, tz);
		Vec3 snapped = Normalize(Sub(target, origin));

		if (Dot(snapped, snapped) <= Epsilon)
			return direction;

		return snapped;
	}

	bool NextVoxelBoundary(
		const VoxelGridDesc& grid,
		const Vec3& p,
		const Vec3& dir,
		int32_t x,
		int32_t y,
		int32_t z,
		float& t,
		int32_t& face)
	{
		float size = grid.VoxelSize;

		float minX = grid.Origin.X + float(x) * size;
		float minY = grid.Origin.Y + float(y) * size;
		float minZ = grid.Origin.Z + float(z) * size;

		float maxX = minX + size;
		float maxY = minY + size;
		float maxZ = minZ + size;

		t = 3.402823466e+38f;
		face = -1;

		if (dir.X > Epsilon)
		{
			float tx = (maxX - p.X) / dir.X;

			if (tx >= 0.0f && tx < t)
			{
				t = tx;
				face = 1;
			}
		}
		else if (dir.X < -Epsilon)
		{
			float tx = (minX - p.X) / dir.X;

			if (tx >= 0.0f && tx < t)
			{
				t = tx;
				face = 0;
			}
		}

		if (dir.Y > Epsilon)
		{
			float ty = (maxY - p.Y) / dir.Y;

			if (ty >= 0.0f && ty < t)
			{
				t = ty;
				face = 3;
			}
		}
		else if (dir.Y < -Epsilon)
		{
			float ty = (minY - p.Y) / dir.Y;

			if (ty >= 0.0f && ty < t)
			{
				t = ty;
				face = 2;
			}
		}

		if (dir.Z > Epsilon)
		{
			float tz = (maxZ - p.Z) / dir.Z;

			if (tz >= 0.0f && tz < t)
			{
				t = tz;
				face = 5;
			}
		}
		else if (dir.Z < -Epsilon)
		{
			float tz = (minZ - p.Z) / dir.Z;

			if (tz >= 0.0f && tz < t)
			{
				t = tz;
				face = 4;
			}
		}

		return face >= 0;
	}

	void StepVoxelByFace(int32_t face, int32_t& x, int32_t& y, int32_t& z)
	{
		if (face == 0) --x;
		else if (face == 1) ++x;
		else if (face == 2) --y;
		else if (face == 3) ++y;
		else if (face == 4) --z;
		else ++z;
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

		Vec3 diffuse = Mul(incomingEnergy, albedo);
		Vec3 metal = incomingEnergy;

		return Mul(Lerp(diffuse, metal, metallic), roughness);
	}

	Vec3 SurfaceBounceDirection(
		const Vec3& incomingDirection,
		Vec3 normal,
		int32_t fallbackFace)
	{

		if (Dot(normal, normal) <= Epsilon)
			normal = FaceNormal(fallbackFace);

		if (Dot(incomingDirection, normal) > 0.0f)
			normal = Mul(normal, -1.0f);

		return Normalize(Reflect(incomingDirection, normal));
	}

	VoxelLightData SingleFaceData(
		int32_t face,
		const VoxelLightEnergy& incoming,
		const VoxelLightEnergy& outgoing)
	{
		VoxelLightData data{};
		data.Faces[face].Incoming[0] = incoming;
		data.Faces[face].Outgoing[0] = outgoing;
		data.Faces[face].InVisitCount = 1;
		data.Faces[face].OutVisitCount = 1;
		return data;
	}

}

VoxelLightBakeParams::VoxelLightBakeParams() {

	EnergyThreshold = 0.0001f;
	MaxBounceCount = 4;
	ThreadCount = 0;
	RaySubsample = 1;
	SnapBounceDirection = false;
	InitiateLightField = false;
	NormalizeDir = false;
	FillEmptyDir = true;
	BlurPasses = 0;
	BlurStrength = 0.35f;
	BucketSplitThreshold = 0.04f; 
	MergeMode = VoxelLightMergeMode::MaxSample;
}

VoxelRayMarcher::VoxelRayMarcher() {
	_baker = nullptr; _workerIndex = -1;

	_ray = {};
	_ray.LastHitVoxel = -1;
	_ray.BounceCount = 0;

}

void VoxelRayMarcher::SetContext(VoxelLightBaker* baker, int32_t workerIndex)
{
	_baker = baker;
	_workerIndex = workerIndex;
}

void VoxelRayMarcher::Prepare(int32_t voxelCount)
{
	_local.Contribution.Cells.clear();
	_local.CellSlots.assign(voxelCount, -1);
	_local.TouchedVoxels.clear();
	_ray.IsAlive = true;
}

void VoxelRayMarcher::TraceRange(int32_t startRay, int32_t endRay) {

	ClearContribution();

	for (int32_t i = startRay; i < endRay; ++i)
		TraceRay(_baker->_rays[i]);
}


bool VoxelRayMarcher::CreateRay(const VoxelLightRay& ray)
{
	_ray.Origin = ray.Position;
	_ray.Position = ray.Position;
	_ray.Direction = ray.Direction;
	_ray.Energy = ray.Energy;
	_ray.Distance = 0.0f;
	_ray.OriginTotalDistance = 0.0f;
	_ray.TotalDistance = 0.0f;
	_ray.OriginStep = 0;

	_ray.X = 0;
	_ray.Y = 0;
	_ray.Z = 0;

	_ray.LastHitVoxel = -1;
	_ray.LastAffectedVoxel = -1;
	_ray.LastAffectedFace = -1;
	_ray.EnterFace = -1;

	_ray.BounceCount = 0;

	float enterT = 0.0f;

	if (!RayBoxEnter(_baker->_grid, _ray.Origin, _ray.Direction, enterT))
		return false;

	Vec3 position = Add(_ray.Origin, Mul(_ray.Direction, enterT + Epsilon));

	if (!WorldToVoxel(_baker->_grid, position, _ray.X, _ray.Y, _ray.Z))
		return false;

	_ray.Position = VoxelCenter(_baker->_grid, _ray.X, _ray.Y, _ray.Z);
	_ray.Distance = Length(Sub(_ray.Position, _ray.Origin));
	_ray.TotalDistance = _ray.OriginTotalDistance + _ray.Distance;
	_ray.IsAlive = true;

	return true;
}

void VoxelRayMarcher::TraceRay(const VoxelLightRay& ray) {
	if (!CreateRay(ray))return;

	while (Step())
	{
	}

}

void VoxelRayMarcher::GetDebugState(
	VoxelRayDebugState& state) const
{
	state.Origin = _ray.Origin;
	state.Position = Add(_ray.Origin, Mul(_ray.Direction, _ray.Distance));
	state.Direction = _ray.Direction;
	state.Energy = _ray.Energy;
	state.Distance = _ray.Distance;
	state.OriginTotalDistance = _ray.OriginTotalDistance;
	state.TotalDistance = _ray.TotalDistance;

	state.X = _ray.X;
	state.Y = _ray.Y;
	state.Z = _ray.Z;

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


bool VoxelRayMarcher::MoveToNextVoxel(int32_t& exitFace)
{
	exitFace = -1;

	int32_t oldX = _ray.X;
	int32_t oldY = _ray.Y;
	int32_t oldZ = _ray.Z;
	int32_t oldIndex = VoxelIndex(_baker->_grid, oldX, oldY, oldZ);

	Vec3 rayPosition = Add(_ray.Origin, Mul(_ray.Direction, _ray.Distance));

	float exitT;

	if (!NextVoxelBoundary(
		_baker->_grid,
		rayPosition,
		_ray.Direction,
		oldX,
		oldY,
		oldZ,
		exitT,
		exitFace))
	{
		_ray.IsAlive = false;
		return false;
	}

	float voxelStep = _baker->_grid.VoxelSize * 0.5f + Epsilon;

	while (true)
	{
		float nextDistance = _ray.Distance + voxelStep;
		Vec3 position = Add(_ray.Origin, Mul(_ray.Direction, nextDistance));

		int32_t nextX;
		int32_t nextY;
		int32_t nextZ;

		if (!WorldToVoxel(_baker->_grid, position, nextX, nextY, nextZ))
		{
			_ray.Distance = nextDistance;
			_ray.TotalDistance = _ray.OriginTotalDistance + _ray.Distance;
			_ray.IsAlive = false;
			return true;
		}

		int32_t nextIndex = VoxelIndex(_baker->_grid, nextX, nextY, nextZ);

		if (nextIndex == oldIndex)
		{
			_ray.Distance = nextDistance;
			_ray.TotalDistance = _ray.OriginTotalDistance + _ray.Distance;
			continue;
		}

		Vec3 center = VoxelCenter(_baker->_grid, nextX, nextY, nextZ);

		_ray.Position = center;
		_ray.Distance = Length(Sub(center, _ray.Origin));
		_ray.TotalDistance = _ray.OriginTotalDistance + _ray.Distance;

		_ray.X = nextX;
		_ray.Y = nextY;
		_ray.Z = nextZ;
		_ray.EnterFace = exitFace ^ 1;

		return true;
	}
}

bool VoxelRayMarcher::Step()
{
	if (!_ray.IsAlive)
		return false;

	_ray.LastAffectedVoxel = -1;
	_ray.LastAffectedFace = -1;

	float falloff = LightFalloffAtDistance(
		_baker->_currentLightFalloff,
		_ray.TotalDistance);

	Vec3 stepEnergy = Mul(_ray.Energy, falloff);

	int32_t index = VoxelIndex(_baker->_grid, _ray.X, _ray.Y, _ray.Z);

	int32_t slot = _local.CellSlots[index];

	if (slot < 0)
	{
		slot = int32_t(_local.Contribution.Cells.size());

		_local.CellSlots[index] = slot;
		_local.TouchedVoxels.push_back(index);

		VoxelLightCell cell{};
		cell.Index = index;

		_local.Contribution.Cells.push_back(cell);
	}

	VoxelLightData& data = _local.Contribution.Cells[slot].Data;

	const VoxelData& voxelData = _baker->_scene[index];

	int32_t outgoingFace = IncomingBucketFaceFromDirection(Mul(_ray.Direction, -1.0f));
	int32_t incomingFace = outgoingFace ^ 1;

	int32_t hitFace;

	bool hasHit = SelectVoxelHitFace(
		voxelData,
		_ray.Direction,
		_ray.OriginStep,
		hitFace);

	if (!hasHit)
		hitFace = incomingFace;

	const VoxelFaceData& faceData = voxelData.Faces[hitFace];

	if (_ray.BounceCount > 0 || !_baker->_params.InitiateLightField)
	{
		MergeBucketedEnergy(
			data,
			Mul(_ray.Direction, -1.0f),
			stepEnergy,
			Mul(_ray.Direction, -1.0f),
			_ray.BounceCount,
			false,
			_baker->_params.BucketSplitThreshold,
			_baker->_params.MergeMode);

		data.Faces[incomingFace].InVisitCount++;
	}

	if (hasHit)
	{
		_ray.LastHitVoxel = index;
		_ray.LastAffectedFace = hitFace;
		_ray.BounceCount++;

		if (_ray.BounceCount >= _baker->_params.MaxBounceCount)
		{
			_ray.IsAlive = false;
		}
		else
		{
			auto normal = faceData.Normal;

			if (faceData.Side == VoxelTriangleSide::Back)
				normal = Mul(normal, -1.0f);

			Vec3 bounceDir = SurfaceBounceDirection(
				_ray.Direction,
				normal,
				hitFace);

			Vec3 bounceOrigin;

			if (_baker->_params.SnapBounceDirection)
			{
				bounceDir = SnapDirectionToGridBoundary(
					_baker->_grid,
					_ray.Position,
					bounceDir);

				bounceOrigin = _ray.Position;
			}
			else
			{
				bounceOrigin = VoxelFaceRayIntersection(
					_baker->_grid,
					_ray.X,
					_ray.Y,
					_ray.Z,
					hitFace,
					_ray.Origin,
					_ray.Direction);

				_ray.Position = bounceOrigin;
			}

			Vec3 bounceEnergy = SurfaceBounceEnergy(_ray.Energy, faceData);
			Vec3 bounceStepEnergy = Mul(bounceEnergy, falloff);

			_ray.Origin = bounceOrigin;
			_ray.Direction = bounceDir;
			_ray.Energy = bounceEnergy;
			_ray.OriginTotalDistance = _ray.TotalDistance;
			_ray.Distance = 0.0f;
			_ray.OriginStep = 0;
		}
	}

	Vec3 currentEnergy = stepEnergy;

	if (_ray.IsAlive)
	{
		int32_t exitFace;

		if (!MoveToNextVoxel(exitFace))
		{
			_ray.IsAlive = false;
		}
		else
		{
			_ray.OriginStep++;

			float exitFalloff = LightFalloffAtDistance(
				_baker->_currentLightFalloff,
				_ray.TotalDistance);

			Vec3 exitEnergy = Mul(_ray.Energy, exitFalloff);

			_ray.LastAffectedFace = exitFace;

			if (_ray.BounceCount > 0 || !_baker->_params.InitiateLightField)
			{
				int32_t exitOutgoingFace = IncomingBucketFaceFromDirection(Mul(_ray.Direction, -1.0f));

				MergeBucketedEnergy(
					data,
					Mul(_ray.Direction, -1.0f),      // bucket direction
					exitEnergy,
					_ray.Direction,                 // stored outgoing direction
					_ray.BounceCount,
					true,                           // Outgoing
					_baker->_params.BucketSplitThreshold,
					_baker->_params.MergeMode);

				data.Faces[exitOutgoingFace].OutVisitCount++;
			}

			currentEnergy = exitEnergy;
		}
	}

	_ray.LastAffectedVoxel = index;

	if (_ray.IsAlive)
	{
		if (!HasEnergy(currentEnergy, _baker->_params.EnergyThreshold))
			_ray.IsAlive = false;
		else
			_ray.IsAlive = IsInsideGrid(_baker->_grid, _ray.X, _ray.Y, _ray.Z);
	}

	return _ray.IsAlive;
}

void VoxelRayMarcher::ClearContribution() {
	for (int32_t index : _local.TouchedVoxels)_local.CellSlots[index] = -1;

	_local.TouchedVoxels.clear();
	_local.Contribution.Cells.clear();

}

VoxelLightBaker::VoxelLightBaker()
{
	_params = VoxelLightBakeParams();
	_grid = {};
	_voxelCount = 0;
	_currentLightCenter = {};
	_currentLightVoxel = -1;
	_currentContribution = nullptr;
	_currentLightEnergy = {};
	_currentLightFalloff = {};
}

VoxelLightBaker::VoxelLightBaker(const VoxelLightBakeParams& params)
{
	_params = params;
	_grid = {};
	_voxelCount = 0;
	_currentLightCenter = {};
	_currentLightVoxel = -1;
	_currentContribution = nullptr;
	_currentLightEnergy = {};
	_currentLightFalloff = {};
}

void VoxelLightBaker::SetParams(const VoxelLightBakeParams& params) { _params = params; }

void VoxelLightBaker::SetGrid(const VoxelGridDesc& grid)
{
	_grid = grid;
	_voxelCount = grid.SizeX * grid.SizeY * grid.SizeZ;

	_scene.assign(_voxelCount, VoxelData{});
	_lightData.assign(_voxelCount, VoxelLightData{});

	_currentMerge.CellSlots.assign(_voxelCount, -1);
	_currentMerge.TouchedVoxels.clear();

	int32_t threadCount = _params.ThreadCount;

	if (threadCount <= 0)
		threadCount = int32_t(std::max(1u, std::thread::hardware_concurrency()));

	_marchers.resize(threadCount);

	for (int32_t i = 0; i < threadCount; ++i)
	{
		_marchers[i].SetContext(this, i);
		_marchers[i].Prepare(_voxelCount);
	}
}

void VoxelLightBaker::ClearScene()
{
	std::fill(_scene.begin(), _scene.end(), VoxelData{});
}

void VoxelLightBaker::AddMesh(const Int3& origin, const Int3& size, const VoxelData* voxels, const VoxelMeshResolvedFace* faces, int32_t faceCount) {
	for (int32_t z = 0; z < size.Z; ++z) {
		int32_t dstZ = origin.Z + z;

		if (dstZ < 0 || dstZ >= _grid.SizeZ)
			continue;

		for (int32_t y = 0; y < size.Y; ++y)
		{
			int32_t dstY = origin.Y + y;

			if (dstY < 0 || dstY >= _grid.SizeY)
				continue;

			for (int32_t x = 0; x < size.X; ++x)
			{
				int32_t dstX = origin.X + x;

				if (dstX < 0 || dstX >= _grid.SizeX)
					continue;

				int32_t srcIndex = (z * size.Y + y) * size.X + x;
				int32_t dstIndex = VoxelIndex(_grid, dstX, dstY, dstZ);

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

void VoxelLightBaker::BakePointLight(const PointLight& light, VoxelLightContribution& contribution)
{
	_currentLightEnergy = Mul(light.Color, light.Intensity);
	_currentLightFalloff = light.Falloff;
	_currentContribution = &contribution;

	contribution.Cells.clear();
	ClearMergeState(_currentMerge);

	int32_t lx;
	int32_t ly;
	int32_t lz;

	if (!WorldToVoxel(_grid, light.Position, lx, ly, lz))
	{
		float invSize = 1.0f / _grid.VoxelSize;

		lx = int32_t(std::floor((light.Position.X - _grid.Origin.X) * invSize));
		ly = int32_t(std::floor((light.Position.Y - _grid.Origin.Y) * invSize));
		lz = int32_t(std::floor((light.Position.Z - _grid.Origin.Z) * invSize));

		lx = std::clamp(lx, 0, _grid.SizeX - 1);
		ly = std::clamp(ly, 0, _grid.SizeY - 1);
		lz = std::clamp(lz, 0, _grid.SizeZ - 1);
	}

	_currentLightCenter = VoxelCenter(_grid, lx, ly, lz);
	_currentLightVoxel = VoxelIndex(_grid, lx, ly, lz);

	if (_params.InitiateLightField)
		PrefillPointLightContribution();

	if (_params.MaxBounceCount > 0) {

		GeneratePointLightRays();

		TraceRays(contribution);

		if (_params.InitiateLightField)
			CleanupUnvisitedFaces(contribution);
	}

	ClearMergeState(_currentMerge);
	_currentContribution = nullptr;
}

void VoxelLightBaker::ClearLightField()
{
	std::fill(_lightData.begin(), _lightData.end(), VoxelLightData{});
}

void VoxelLightBaker::AccumulateLight(const VoxelLightContribution& contribution) {

	for (const VoxelLightCell& cell : contribution.Cells) {

		if (cell.Index < 0 || cell.Index >= _voxelCount)
			continue;
		MergeVoxelLightData(_lightData[cell.Index], cell.Data);
	}
}

VoxelLightField& VoxelLightBaker::GetLightField()
{
	BuildLightField();
	return _field;
}

void VoxelLightBaker::PrefillPointLightContribution()
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
		{
			for (int32_t x = 0; x < _grid.SizeX; ++x)
			{
				int32_t index = VoxelIndex(_grid, x, y, z);

				Vec3 center = VoxelCenter(_grid, x, y, z);
				Vec3 lightToVoxel = Sub(center, _currentLightCenter);

				float distance = Length(lightToVoxel);

				if (distance <= Epsilon)
					continue;

				float falloff = LightFalloffAtDistance(_currentLightFalloff, distance);
				Vec3 energy = Mul(_currentLightEnergy, falloff);

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				Vec3 direction = Mul(lightToVoxel, 1.0f / distance);

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				MergeEnergy(
					cell.Data.Faces[outgoingFace].Outgoing[0],
					MakeEnergy(energy, direction));

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(*_currentContribution, _currentMerge, directContribution);
}

void VoxelLightBaker::GeneratePointLightRays()
{
	_rays.clear();

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 rayEnergy = _currentLightEnergy;

	if (_params.MergeMode == VoxelLightMergeMode::Add)
		rayEnergy = Mul(rayEnergy, 1.0f / float(subSample * subSample));

	auto addRay = [this, rayEnergy](const Vec3& origin)
		{
			Vec3 dir = Normalize(Sub(origin, _currentLightCenter));

			if (Dot(dir, dir) <= Epsilon)
				return;

			VoxelLightRay ray{};
			ray.Position = _currentLightCenter;
			ray.Direction = dir;
			ray.Energy = rayEnergy;

			_rays.push_back(ray);
		};

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
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
						_grid.Origin.X + float(_grid.SizeX) * size,
						_grid.Origin.Y + fy * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t x = 0; x < _grid.SizeX; ++x)
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
						_grid.Origin.Y + float(_grid.SizeY) * size,
						_grid.Origin.Z + fz * size
						});
				}
			}
		}
	}

	for (int32_t y = 0; y < _grid.SizeY; ++y)
	{
		for (int32_t x = 0; x < _grid.SizeX; ++x)
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
						_grid.Origin.Z + float(_grid.SizeZ) * size
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

void VoxelLightBaker::TraceRays(VoxelLightContribution& contribution) {
	int32_t rayCount = int32_t(_rays.size());

	if (rayCount == 0)
		return;

	int32_t threadCount = int32_t(_marchers.size());

	if (threadCount <= 1 || rayCount < threadCount)
	{
		_marchers[0].TraceRange(0, rayCount);
		MergeWorkerContribution(_marchers[0].Contribution());
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

		threads.emplace_back([this, i, start, end]()
			{
				_marchers[i].TraceRange(start, end);

				std::lock_guard<std::mutex> lock(_mergeLock);
				MergeWorkerContribution(_marchers[i].Contribution());
			});

		rangeStart = rangeEnd;
	}

	for (std::thread& thread : threads)
		thread.join();

}

void VoxelLightBaker::MergeWorkerContribution(const VoxelLightContribution& workerContribution)
{
	MergeContribution(*_currentContribution, _currentMerge, workerContribution);
}

void VoxelLightBaker::MergeContribution(
	VoxelLightContribution& target,
	ContributionMergeState& mergeState,
	const VoxelLightContribution& source)
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
			continue;
		}

		MergeVoxelLightData(target.Cells[slot].Data, sourceCell.Data, _params.MergeMode);
	}
}

void VoxelLightBaker::ClearMergeState(ContributionMergeState& mergeState) {
	for (int32_t index : mergeState.TouchedVoxels)mergeState.CellSlots[index] = -1;

	mergeState.TouchedVoxels.clear();

}

void VoxelLightBaker::AdjustLightFieldDirections()
{
	int32_t strideX = 1;
	int32_t strideY = _field.SizeX;
	int32_t strideZ = _field.SizeX * _field.SizeY;

	for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
	{
		std::vector<Vec3>& directions = _field.Direction[face];

		Vec3 normal = FaceNormal(face);

		if (std::fabs(normal.X) > 0.5f)
		{
			for (int32_t z = 0; z < _field.SizeZ; ++z)
			{
				for (int32_t y = 0; y < _field.SizeY; ++y)
				{
					Vec3* values = &directions[FieldIndex(_field, 0, y, z)];
					FillDirectionLine(values, _field.SizeX, strideX);
				}
			}
		}
		else if (std::fabs(normal.Y) > 0.5f)
		{
			for (int32_t z = 0; z < _field.SizeZ; ++z)
			{
				for (int32_t x = 0; x < _field.SizeX; ++x)
				{
					Vec3* values = &directions[FieldIndex(_field, x, 0, z)];
					FillDirectionLine(values, _field.SizeY, strideY);
				}
			}
		}
		else
		{
			for (int32_t y = 0; y < _field.SizeY; ++y)
			{
				for (int32_t x = 0; x < _field.SizeX; ++x)
				{
					Vec3* values = &directions[FieldIndex(_field, x, y, 0)];
					FillDirectionLine(values, _field.SizeZ, strideZ);
				}
			}
		}
	}
}

void VoxelLightBaker::BlurLightField()
{
	float strength = std::clamp(_params.BlurStrength, 0.0f, 1.0f);
	int32_t passes = std::max(0, _params.BlurPasses);

	if (strength <= 0.0f || passes <= 0)
		return;

	int32_t count = _field.SizeX * _field.SizeY * _field.SizeZ;

	std::vector<Vec3> tempColor(count);
	std::vector<Vec3> tempDirection(count);

	for (int32_t pass = 0; pass < passes; ++pass)
	{
		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			std::vector<Vec3>& colors = _field.Color[face];
			std::vector<Vec3>& directions = _field.Direction[face];

			for (int32_t z = 0; z < _field.SizeZ; ++z)
			{
				for (int32_t y = 0; y < _field.SizeY; ++y)
				{
					for (int32_t x = 0; x < _field.SizeX; ++x)
					{
						int32_t index = FieldIndex(_field, x, y, z);

						Vec3 colorSum = colors[index];
						Vec3 dirSum = directions[index];
						float weightSum = 1.0f;

						auto addNeighbor = [&](int32_t nx, int32_t ny, int32_t nz)
							{
								if (nx < 0 || ny < 0 || nz < 0 ||
									nx >= _field.SizeX ||
									ny >= _field.SizeY ||
									nz >= _field.SizeZ)
								{
									return;
								}

								int32_t ni = FieldIndex(_field, nx, ny, nz);

								colorSum = Add(colorSum, colors[ni]);
								dirSum = Add(dirSum, directions[ni]);
								weightSum += 1.0f;
							};

						addNeighbor(x - 1, y, z);
						addNeighbor(x + 1, y, z);
						addNeighbor(x, y - 1, z);
						addNeighbor(x, y + 1, z);
						addNeighbor(x, y, z - 1);
						addNeighbor(x, y, z + 1);

						Vec3 avgColor = Mul(colorSum, 1.0f / weightSum);
						Vec3 avgDir = Mul(dirSum, 1.0f / weightSum);

						tempColor[index] = Lerp(colors[index], avgColor, strength);
						tempDirection[index] = Lerp(directions[index], avgDir, strength);
					}
				}
			}

			colors.swap(tempColor);
			directions.swap(tempDirection);
		}
	}
}

void VoxelLightBaker::BuildLightField() {

	_field.SizeX = _grid.SizeX; _field.SizeY = _grid.SizeY; _field.SizeZ = _grid.SizeZ;


	for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
	{
		_field.Color[face].resize(_voxelCount);
		_field.Direction[face].resize(_voxelCount);
	}

	for (int32_t i = 0; i < _voxelCount; ++i)
	{
		const VoxelLightData& src = _lightData[i];

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			VoxelLightEnergy outgoing{};

			for (int32_t slot = 0; slot < MAX_BOUNCES; ++slot)
				MergeEnergy(
					outgoing,
					src.Faces[face].Outgoing[slot],
					VoxelLightMergeMode::Add);

			_field.Color[face][i] = outgoing.Energy;

			auto outDir = Add(Add(outgoing.DirectionR, outgoing.DirectionG), outgoing.DirectionB);

			if (_params.NormalizeDir)
				outDir = Normalize(outDir);

			_field.Direction[face][i] = outDir;
		}
	}

	if (_params.BlurPasses > 0)
		BlurLightField();

	if (_params.FillEmptyDir)
		AdjustLightFieldDirections();

}