#include "pch.h"



namespace {
	constexpr float Pi = 3.14159265358979323846f; constexpr float Epsilon = 1e-5f;

	static constexpr Vec3 FaceNormals[VOXEL_LIGHT_FACE_COUNT] =
	{
		{ -1.0f,  0.0f,  0.0f },
		{  1.0f,  0.0f,  0.0f },
		{  0.0f, -1.0f,  0.0f },
		{  0.0f,  1.0f,  0.0f },
		{  0.0f,  0.0f, -1.0f },
		{  0.0f,  0.0f,  1.0f }
	};

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

	Vec3 Cross(const Vec3& a, const Vec3& b)
	{
		return {
			a.Y * b.Z - a.Z * b.Y,
			a.Z * b.X - a.X * b.Z,
			a.X * b.Y - a.Y * b.X
		};
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


	Vec3 FaceNormal(int32_t face)
	{
		return FaceNormals[face];
	}

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

	float SpotConeAttenuation(
		const Vec3& lightDirection,
		float innerCos,
		float outerCos,
		const Vec3& rayDirection)
	{
		Vec3 axis = Normalize(lightDirection);
		Vec3 dir = Normalize(rayDirection);

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


	Vec3 DirectionalLightEnergy(const DirectionalLight& light)
	{
		return Mul(light.Color, light.Intensity);
	}

	Vec3 SpotLightEnergyAtDistance(
		const SpotLight& light,
		float distance,
		const Vec3& rayDirection)
	{
		float cone = SpotConeAttenuation(
			light.Direction,
			light.InnerCos,
			light.OuterCos,
			rayDirection);

		if (cone <= 0.0f)
			return Zero3();

		return Mul(
			Mul(light.Color, light.Intensity * cone),
			LightFalloffAtDistance(light.Falloff, distance));
	}

	void DirectionBasis(
		const Vec3& direction,
		Vec3& right,
		Vec3& up)
	{
		Vec3 axis = Normalize(direction);

		Vec3 ref = std::fabs(axis.Y) < 0.9f
			? Vec3{ 0.0f, 1.0f, 0.0f }
		: Vec3{ 1.0f, 0.0f, 0.0f };

		right = Normalize(Cross(ref, axis));

		if (Dot(right, right) <= Epsilon)
			right = { 1.0f, 0.0f, 0.0f };

		up = Normalize(Cross(axis, right));
	}

	bool IsInsideDirectionalPlane(
		const Vec3& point,
		const Vec3& planeCenter,
		const Vec3& direction,
		const Vec3& right,
		const Vec3& up,
		float width,
		float height)
	{
		Vec3 local = Sub(point, planeCenter);

		if (Dot(local, direction) < -Epsilon)
			return false;

		float halfWidth = width * 0.5f;
		float halfHeight = height * 0.5f;

		return std::fabs(Dot(local, right)) <= halfWidth &&
			std::fabs(Dot(local, up)) <= halfHeight;
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


	void AdjustCompatibleFaceEnergy(VoxelLightContribution& contribution)
	{
		for (VoxelLightCell& cell : contribution.Cells)
		{
			int32_t count = 0;

			Vec3 sumEnergy = Zero3();
			Vec3 sumDirectionR = Zero3();
			Vec3 sumDirectionG = Zero3();
			Vec3 sumDirectionB = Zero3();

			bool lit[VOXEL_LIGHT_FACE_COUNT]{};

			for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
			{
				VoxelLightEnergy& energy = cell.Data.Faces[face].Outgoing;

				if (EnergyScore(energy.Energy) <= Epsilon)
					continue;

				lit[face] = true;
				++count;

				sumEnergy = Add(sumEnergy, energy.Energy);
				sumDirectionR = Add(sumDirectionR, energy.DirectionR);
				sumDirectionG = Add(sumDirectionG, energy.DirectionG);
				sumDirectionB = Add(sumDirectionB, energy.DirectionB);
			}

			if (count <= 1)
				continue;

			float scale = 1.0f / float(count * count);

			Vec3 outEnergy = Mul(sumEnergy, scale);
			Vec3 outDirectionR = Mul(sumDirectionR, scale);
			Vec3 outDirectionG = Mul(sumDirectionG, scale);
			Vec3 outDirectionB = Mul(sumDirectionB, scale);

			for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
			{
				if (!lit[face])
					continue;

				VoxelLightEnergy& energy = cell.Data.Faces[face].Outgoing;

				energy.Energy = outEnergy;
				energy.DirectionR = outDirectionR;
				energy.DirectionG = outDirectionG;
				energy.DirectionB = outDirectionB;
			}
		}
	}

	void MergeEnergy(
		VoxelLightEnergy& target,
		const VoxelLightEnergy& source,
		VoxelLightMergeMode mode)
	{
		if (mode == VoxelLightMergeMode::Add) {

			target.Energy = Add(target.Energy, source.Energy);
			
			target.DirectionR = Add(target.DirectionR, source.DirectionR);
			target.DirectionG = Add(target.DirectionG, source.DirectionG);
			target.DirectionB = Add(target.DirectionB, source.DirectionB);
		}
		else if (mode == VoxelLightMergeMode::AddPreserveDir)
		{
			target.Energy = Add(target.Energy, source.Energy);
			
			if (Dot(target.DirectionR, target.DirectionR) < Epsilon)
				target.DirectionR = source.DirectionR;
			
			if (Dot(target.DirectionG, target.DirectionG) < Epsilon)
				target.DirectionG = source.DirectionG;

			if (Dot(target.DirectionB, target.DirectionB) < Epsilon)
				target.DirectionB = source.DirectionB;

		}
		else 
		{
			if (EnergyScore(source.Energy) > EnergyScore(target.Energy))
				target = source;
		}


	}

	void MergeFace(
		VoxelLightFace& target,
		const VoxelLightFace& source,
		VoxelLightMergeMode mode)
	{
		MergeEnergy(target.Incoming, source.Incoming, mode);
		MergeEnergy(target.Outgoing, source.Outgoing, mode);

		target.InVisitCount = std::max(target.InVisitCount, source.InVisitCount);
		target.OutVisitCount = std::max(target.OutVisitCount, source.OutVisitCount);
	}

	/*
	int32_t BucketFacesFromDirection(
		const Vec3& dir,
		float threshold,
		int32_t* faces,
		int32_t& dominantFace)
	{
		float amounts[VOXEL_LIGHT_FACE_COUNT];

		float bestAmount = 0.0f;
		dominantFace = 0;

		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
		{
			float amount = std::max(0.0f, -Dot(dir, FaceNormal(face)));

			amounts[face] = amount;

			if (amount > bestAmount)
			{
				bestAmount = amount;
				dominantFace = face;
			}
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
		bool outgoing,
		float threshold,
		VoxelLightMergeMode mode = VoxelLightMergeMode::Add)
	{
		int32_t faces[VOXEL_LIGHT_FACE_COUNT];
		int32_t dominantFace = -1;
		int32_t count = BucketFacesFromDirection(bucketDirection, threshold, faces, dominantFace);

		if (count <= 0)
			return;

		float weight = 1.0f / float(count);
		VoxelLightEnergy source = MakeEnergy(Mul(energy, weight), storedDirection);

		for (int32_t i = 0; i < count; ++i)
		{
			VoxelLightFace& face = data.Faces[faces[i]];

			VoxelLightEnergy& target = outgoing
				? face.Outgoing
				: face.Incoming;

			MergeEnergy(target, source, mode);
		}

		if (dominantFace >= 0)
		{
			if (outgoing)
				data.Faces[dominantFace].OutVisitCount++;
			else
				data.Faces[dominantFace].InVisitCount++;
		}
	}
	*/
	
	void MergeVoxelLightData(
		VoxelLightData& target,
		const VoxelLightData& source,
		VoxelLightMergeMode mode)
	{
		for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
			MergeFace(target.Faces[face], source.Faces[face], mode);
	}


	/*
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
	*/

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

			/*
			if (faceData.Side == VoxelTriangleSide::Back)
				normal = Mul(normal, -1.0f);
		
			normal = Normalize(normal);

			if (Dot(normal, normal) <= Epsilon)
				normal = FaceNormal(face);
			*/

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

	bool RayVoxelIntersectionPoint(
		const Vec3& voxelCenter,
		float voxelSize,
		const Vec3& origin,
		const Vec3& direction,
		Vec3& point)
	{
		float half = voxelSize * 0.5f;

		float minAxis[3] =
		{
			voxelCenter.X - half,
			voxelCenter.Y - half,
			voxelCenter.Z - half
		};

		float maxAxis[3] =
		{
			voxelCenter.X + half,
			voxelCenter.Y + half,
			voxelCenter.Z + half
		};

		float originAxis[3] =
		{
			origin.X,
			origin.Y,
			origin.Z
		};

		float dirAxis[3] =
		{
			direction.X,
			direction.Y,
			direction.Z
		};

		float tEnter = -FLT_MAX;
		float tExit = FLT_MAX;

		for (int32_t axis = 0; axis < 3; ++axis)
		{
			float o = originAxis[axis];
			float d = dirAxis[axis];

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

			tEnter = std::max(tEnter, t0);
			tExit = std::min(tExit, t1);

			if (tEnter > tExit)
				return false;
		}

		float t = tEnter >= 0.0f ? tEnter : tExit;

		if (t < 0.0f)
			return false;

		point = Add(origin, Mul(direction, t));
		return true;
	}

	/*
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
	*/

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

		Vec3 tangent = Normalize(Cross(up, center));

		if (Dot(tangent, tangent) <= Epsilon)
			return center;

		Vec3 bitangent = Normalize(Cross(center, tangent));

		float angle = 2.0f * Pi * float(index) / float(count - 1);
		Vec3 radial = Add(
			Mul(tangent, std::cos(angle)),
			Mul(bitangent, std::sin(angle)));

		return Normalize(Add(
			Mul(center, std::cos(angleRad)),
			Mul(radial, std::sin(angleRad))));
	}


	VoxelLightData SingleFaceData(
		int32_t face,
		const VoxelLightEnergy& incoming,
		const VoxelLightEnergy& outgoing)
	{
		VoxelLightData data{};
		data.Faces[face].Incoming = incoming;
		data.Faces[face].Outgoing = outgoing;
		data.Faces[face].InVisitCount = 1;
		data.Faces[face].OutVisitCount = 1;
		return data;
	}

}

VoxelLightBakeParams::VoxelLightBakeParams() {

	EnergyThreshold = 0.0001f;

	ThreadCount = 0;
	RaySubsample = 1;
	SnapBounceDirection = false;
	InitiateLightField = false;
	NormalizeDir = false;
	FillEmptyDir = true;
	BlurPasses = 0;
	BlurStrength = 0.35f;

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


bool VoxelRayMarcher::CreateRay(const VoxelLightRay& ray, int32_t generation)
{
	_ray.Origin = ray.Position;
	_ray.Position = ray.Position;
	_ray.Direction = ray.Direction;
	_ray.Energy = ray.Energy;
	_ray.Distance = 0.0f;
	_ray.OriginTotalDistance = ray.OriginTotalDistance;
	_ray.TotalDistance = ray.OriginTotalDistance;
	_ray.Falloff = ray.Falloff;
	_ray.OriginStep = 0;

	_ray.X = 0;
	_ray.Y = 0;
	_ray.Z = 0;

	_ray.LastHitVoxel = -1;
	_ray.LastAffectedVoxel = -1;
	_ray.LastAffectedFace = -1;

	_ray.BounceCount = generation;

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

void VoxelRayMarcher::TraceRay(const VoxelLightRay& ray, int32_t generation) {

	if (!CreateRay(ray, generation))
		return;

	while (Step()) { }
}

void VoxelRayMarcher::TraceRange(int32_t startRay, int32_t endRay, int32_t generation) {

	ClearContribution();
	_nextRays.clear();

	for (int32_t i = startRay; i < endRay; ++i)
		TraceRay(_baker->_rays[i], generation);
}



void VoxelRayMarcher::GetDebugState(
	VoxelRayDebugState& state) const
{
	float falloff = LightFalloffAtDistance(
		_ray.Falloff,
		_ray.TotalDistance);

	state.Origin = _ray.Origin;
	state.Position = Add(_ray.Origin, Mul(_ray.Direction, _ray.Distance));
	state.Direction = _ray.Direction;
	state.Energy = Mul(_ray.Energy, falloff);
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


bool VoxelRayMarcher::MoveToNextVoxel()
{

	int32_t oldX = _ray.X;
	int32_t oldY = _ray.Y;
	int32_t oldZ = _ray.Z;
	int32_t oldIndex = VoxelIndex(_baker->_grid, oldX, oldY, oldZ);

	Vec3 rayPosition = Add(_ray.Origin, Mul(_ray.Direction, _ray.Distance));

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
			return false;
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
		_ray.OriginStep++;

		_ray.X = nextX;
		_ray.Y = nextY;
		_ray.Z = nextZ;

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
		_ray.Falloff,
		_ray.TotalDistance);

	Vec3 stepEnergy = Mul(_ray.Energy, falloff);

	if (!HasEnergy(stepEnergy, _baker->_params.EnergyThreshold))
		_ray.IsAlive = false;
	else
		_ray.IsAlive = IsInsideGrid(_baker->_grid, _ray.X, _ray.Y, _ray.Z);

	if (!_ray.IsAlive)
		return false;

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

	int32_t incomingFace = IncomingBucketFaceFromDirection(_ray.Direction);
    int32_t outgoingFace = incomingFace ^ 1;

	int32_t hitFace;

	bool hasHit = SelectVoxelHitFace(
		voxelData,
		_ray.Direction,
		_ray.OriginStep,
		hitFace);

	if (!hasHit)
		hitFace = incomingFace;

	const VoxelFaceData& faceData = voxelData.Faces[hitFace];

#ifdef _DEBUG

	if (_ray.BounceCount > 0 || !_baker->_params.InitiateLightField)
	{
		MergeEnergy(
			data.Faces[incomingFace].Incoming,
			MakeEnergy(stepEnergy, _ray.Direction),
			_baker->_params.MergeMode);

	}
#endif

	if (hasHit)
	{
		_ray.LastHitVoxel = index;
		_ray.LastAffectedFace = hitFace;

		int32_t nextGeneration = _ray.BounceCount + 1;

		if (nextGeneration < _baker->_params.Bounce.MaxCount)
		{
			Vec3 normal = faceData.Normal;

			/*
			if (faceData.Side == VoxelTriangleSide::Back)
				normal = Mul(normal, -1.0f);
		
			if (Dot(normal, normal) <= Epsilon)
				normal = FaceNormal(hitFace);
		
			if (Dot(_ray.Direction, normal) > 0.0f)
				normal = Mul(normal, -1.0f);

			normal = Normalize(normal);
			*/

			Vec3 reflectDir = Normalize(Reflect(_ray.Direction, normal));

			float roughness = std::clamp(faceData.Roughness, 0.0f, 1.0f);
			float metallic = std::clamp(faceData.Metallic, 0.0f, 1.0f);

			float normalWeight = std::clamp(
				_baker->_params.Bounce.NormalWeight * (1.0f - metallic),
				0.0f,
				1.0f);

			Vec3 bounceDir = Normalize(Lerp(reflectDir, normal, normalWeight));

			if (Dot(bounceDir, bounceDir) <= Epsilon)
				bounceDir = reflectDir;

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
			{/*
				bounceOrigin = VoxelFaceRayIntersection(
					_baker->_grid,
					_ray.X,
					_ray.Y,
					_ray.Z,
					hitFace,
					_ray.Origin,
					_ray.Direction);
					*/

				if (!RayVoxelIntersectionPoint(_ray.Position, _baker->_grid.VoxelSize, _ray.Origin, _ray.Direction, bounceOrigin))
					bounceOrigin = _ray.Position;

				_ray.Position = bounceOrigin;
			}

			Vec3 bounceEnergy = SurfaceBounceEnergy(_ray.Energy, faceData);

			int32_t rayCount = BounceRayCountForGeneration(
				_ray.BounceCount,
				_baker->_params);

			rayCount = std::max(1, rayCount);

			float centerWeight = rayCount > 1
				? std::clamp(_baker->_params.Bounce.CenterWeight, 0.0f, 1.0f)
				: 1.0f;

			float coneAngle = _baker->_params.Bounce.ConeMaxAngle * roughness;

			auto pushBounceRay = [&](const Vec3& direction, const Vec3& energy)
				{
					if (!HasEnergy(energy, _baker->_params.EnergyThreshold))
						return;

					Vec3 dir = direction;

					if (_baker->_params.SnapBounceDirection)
					{
						dir = SnapDirectionToGridBoundary(
							_baker->_grid,
							bounceOrigin,
							dir);
					}

					if (Dot(dir, dir) <= Epsilon)
						return;

					VoxelLightRay ray{};
					ray.Position = bounceOrigin;
					ray.Direction = dir;
					ray.Energy = energy;
					ray.OriginTotalDistance = _ray.TotalDistance;
					ray.Falloff = _ray.Falloff;

					_nextRays.push_back(ray);
				};

			pushBounceRay(
				bounceDir,
				Mul(bounceEnergy, centerWeight));

			int32_t sideCount = rayCount - 1;

			if (sideCount > 0)
			{
				Vec3 sideEnergy = Mul(
					bounceEnergy,
					(1.0f - centerWeight) / float(sideCount));

				for (int32_t i = 0; i < sideCount; ++i)
				{
					Vec3 sideDir = ConeDirection(
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
	else {

		MergeEnergy(
			data.Faces[outgoingFace].Outgoing,
			MakeEnergy(stepEnergy, _ray.Direction),
			_baker->_params.RayMergeMode);

		MoveToNextVoxel();
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

}

VoxelLightBaker::VoxelLightBaker(const VoxelLightBakeParams& params)
{
	_params = params;
	_grid = {};
	_voxelCount = 0;

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

void VoxelLightBaker::AddGpuMeshFaces(
	const GpuVoxelFaceData* faces,
	int32_t faceCount)
{
	if (faces == nullptr || faceCount <= 0)
		return;

	for (int32_t i = 0; i < faceCount; ++i)
	{
		const GpuVoxelFaceData& src = faces[i];

		if (src.X < 0 || src.X >= _grid.SizeX)
			continue;

		if (src.Y < 0 || src.Y >= _grid.SizeY)
			continue;

		if (src.Z < 0 || src.Z >= _grid.SizeZ)
			continue;

		if (src.Face < 0 || src.Face >= VOXEL_LIGHT_FACE_COUNT)
			continue;

		int32_t voxelIndex = VoxelIndex(_grid, src.X, src.Y, src.Z);

		VoxelData& dst = _scene[voxelIndex];

		dst.Status = VoxelStatus::Occupied;
		dst.Occupancy = 1.0f;

		VoxelFaceData& face = dst.Faces[src.Face];

		face.Side = static_cast<VoxelTriangleSide>(src.Side);
		face.BaseColor = src.BaseColor;
		face.Normal = src.Normal;
		face.Roughness = src.Roughness;
		face.Metallic = src.Metallic;
	}
}

void VoxelLightBaker::BakeGeneratedRays(VoxelLightContribution& contribution)
{
	for (int32_t generation = 0; generation < _params.Bounce.MaxCount; ++generation)
	{
		if (_rays.empty())
			break;

		VoxelLightContribution generationContribution;

		TraceRays(
			generationContribution,
			_nextRays,
			generation);

		if (generation == 0)
			AdjustCompatibleFaceEnergy(generationContribution);

		MergeContribution(
			contribution,
			_currentMerge,
			generationContribution,
			_params.GenMergeMode);

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

	Vec3 energy = Mul(light.Color, light.Intensity);

	if (!HasEnergy(energy, _params.EnergyThreshold))
	{
		ClearMergeState(_currentMerge);
		return;
	}

	if (_params.InitiateLightField)
		PrefillPointLightContribution(light, contribution);

	if (_params.Bounce.MaxCount > 0)
	{
		GeneratePointLightRays(light);
		BakeGeneratedRays(contribution);
	}

	ClearMergeState(_currentMerge);
}

void VoxelLightBaker::BakeDirectionalLight(
	const DirectionalLight& light,
	VoxelLightContribution& contribution)
{
	contribution.Cells.clear();
	ClearMergeState(_currentMerge);

	Vec3 direction = Normalize(light.Direction);
	Vec3 energy = Mul(light.Color, light.Intensity);

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

	Vec3 direction = Normalize(light.Direction);
	Vec3 energy = Mul(light.Color, light.Intensity);

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

		MergeVoxelLightData(_lightData[cell.Index], cell.Data, _params.LightMergeMode);
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

	Vec3 lightEnergy = Mul(light.Color, light.Intensity);

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
		{
			for (int32_t x = 0; x < _grid.SizeX; ++x)
			{
				int32_t index = VoxelIndex(_grid, x, y, z);

				Vec3 center = VoxelCenter(_grid, x, y, z);
				Vec3 lightToVoxel = Sub(center, light.Position);

				float distance = Length(lightToVoxel);

				if (distance <= Epsilon)
					continue;

				float falloff = LightFalloffAtDistance(light.Falloff, distance);
				Vec3 energy = Mul(lightEnergy, falloff);

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				Vec3 direction = Mul(lightToVoxel, 1.0f / distance);

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				MergeEnergy(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction), _params.LightMergeMode);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBaker::PrefillDirectionalLightContribution(
	const DirectionalLight& light,
	VoxelLightContribution& contribution)
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	Vec3 direction = Normalize(light.Direction);
	Vec3 lightEnergy = DirectionalLightEnergy(light);

	if (Dot(direction, direction) <= Epsilon ||
		!HasEnergy(lightEnergy, _params.EnergyThreshold))
	{
		return;
	}

	Vec3 right;
	Vec3 up;
	DirectionBasis(direction, right, up);

	float width = std::max(0.0f, light.Width);
	float height = std::max(0.0f, light.Height);

	if (width <= Epsilon || height <= Epsilon)
	{
		width = float(std::max(_grid.SizeX, std::max(_grid.SizeY, _grid.SizeZ))) * _grid.VoxelSize;
		height = width;
	}

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
		{
			for (int32_t x = 0; x < _grid.SizeX; ++x)
			{
				int32_t index = VoxelIndex(_grid, x, y, z);
				Vec3 center = VoxelCenter(_grid, x, y, z);
				Vec3 local = Sub(center, light.Position);

				float distance = Dot(local, direction);

				if (distance < -Epsilon)
					continue;

				if (std::fabs(Dot(local, right)) > width * 0.5f ||
					std::fabs(Dot(local, up)) > height * 0.5f)
				{
					continue;
				}

				float falloff = LightFalloffAtDistance(light.Falloff, std::max(0.0f, distance));
				Vec3 energy = Mul(lightEnergy, falloff);

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				MergeEnergy(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction), _params.LightMergeMode);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBaker::PrefillSpotLightContribution(
	const SpotLight& light,
	VoxelLightContribution& contribution)
{
	VoxelLightContribution directContribution;
	directContribution.Cells.reserve(_voxelCount);

	Vec3 lightEnergy = Mul(light.Color, light.Intensity);
	Vec3 lightDirection = Normalize(light.Direction);

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
		{
			for (int32_t x = 0; x < _grid.SizeX; ++x)
			{
				int32_t index = VoxelIndex(_grid, x, y, z);

				Vec3 center = VoxelCenter(_grid, x, y, z);
				Vec3 lightToVoxel = Sub(center, light.Position);

				float distance = Length(lightToVoxel);

				if (distance <= Epsilon)
					continue;

				Vec3 direction = Mul(lightToVoxel, 1.0f / distance);

				float cone = SpotConeAttenuation(
					lightDirection,
					light.InnerCos,
					light.OuterCos,
					direction);

				if (cone <= 0.0f)
					continue;

				float falloff = LightFalloffAtDistance(light.Falloff, distance);
				Vec3 energy = Mul(lightEnergy, falloff * cone);

				if (!HasEnergy(energy, _params.EnergyThreshold))
					continue;

				int32_t incomingFace = IncomingBucketFaceFromDirection(direction);
				int32_t outgoingFace = incomingFace ^ 1;

				VoxelLightCell cell{};
				cell.Index = index;

				MergeEnergy(
					cell.Data.Faces[outgoingFace].Outgoing,
					MakeEnergy(energy, direction), _params.LightMergeMode);

				directContribution.Cells.push_back(cell);
			}
		}
	}

	MergeContribution(contribution, _currentMerge, directContribution);
}

void VoxelLightBaker::GeneratePointLightRays(const PointLight& light)
{
	_rays.clear();

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 rayEnergy = Mul(light.Color, light.Intensity);

	if (_params.RayMergeMode == VoxelLightMergeMode::Add)
		rayEnergy = Mul(rayEnergy, 1.0f / float(subSample * subSample));

	auto addRay = [this, &light, rayEnergy](const Vec3& origin)
		{
			Vec3 dir = Normalize(Sub(origin, light.Position));

			if (Dot(dir, dir) <= Epsilon)
				return;

			VoxelLightRay ray{};
			ray.Position = light.Position;
			ray.Direction = dir;
			ray.Energy = rayEnergy;
			ray.OriginTotalDistance = 0.0f;
			ray.Falloff = light.Falloff;

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

void VoxelLightBaker::GenerateDirectionalLightRays(const DirectionalLight& light)
{
	_rays.clear();

	Vec3 direction = Normalize(light.Direction);
	Vec3 lightEnergy = DirectionalLightEnergy(light);

	if (Dot(direction, direction) <= Epsilon)
		return;

	if (!HasEnergy(lightEnergy, _params.EnergyThreshold))
		return;

	Vec3 right;
	Vec3 up;
	DirectionBasis(direction, right, up);

	float width = std::max(0.0f, light.Width);
	float height = std::max(0.0f, light.Height);

	if (width <= Epsilon || height <= Epsilon)
	{
		width = float(std::max(_grid.SizeX, std::max(_grid.SizeY, _grid.SizeZ))) * _grid.VoxelSize;
		height = width;
	}

	int32_t subSample = std::max(1, _params.RaySubsample);
	float invSubSample = 1.0f / float(subSample);
	float size = _grid.VoxelSize;

	Vec3 rayEnergy = lightEnergy;

	if (_params.RayMergeMode == VoxelLightMergeMode::Add)
		rayEnergy = Mul(rayEnergy, 1.0f / float(subSample * subSample));

	auto tryAddRay = [&](int32_t face, const Vec3& entry)
		{
			Vec3 faceNormal = FaceNormal(face);

			if (Dot(direction, faceNormal) >= -Epsilon)
				return;

			float backDistance = Dot(Sub(entry, light.Position), direction);

			if (backDistance < -Epsilon)
				return;

			Vec3 planePoint = Sub(entry, Mul(direction, backDistance));
			Vec3 local = Sub(planePoint, light.Position);

			if (std::fabs(Dot(local, right)) > width * 0.5f)
				return;

			if (std::fabs(Dot(local, up)) > height * 0.5f)
				return;

			float falloff = LightFalloffAtDistance(light.Falloff, backDistance);
			Vec3 energy = Mul(rayEnergy, falloff);

			if (!HasEnergy(energy, _params.EnergyThreshold))
				return;

			VoxelLightRay ray{};
			ray.Position = entry;
			ray.Direction = direction;
			ray.Energy = energy;
			ray.OriginTotalDistance = 0.0f;
			ray.Falloff = LightFalloff{ LightFalloffNone, 0.0f, 1.0f };

			_rays.push_back(ray);
		};

	for (int32_t z = 0; z < _grid.SizeZ; ++z)
	{
		for (int32_t y = 0; y < _grid.SizeY; ++y)
		{
			for (int32_t sz = 0; sz < subSample; ++sz)
			{
				for (int32_t sy = 0; sy < subSample; ++sy)
				{
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					tryAddRay(
						0,
						{
							_grid.Origin.X,
							_grid.Origin.Y + fy * size,
							_grid.Origin.Z + fz * size
						});

					tryAddRay(
						1,
						{
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
			for (int32_t sz = 0; sz < subSample; ++sz)
			{
				for (int32_t sx = 0; sx < subSample; ++sx)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fz = float(z) + (float(sz) + 0.5f) * invSubSample;

					tryAddRay(
						2,
						{
							_grid.Origin.X + fx * size,
							_grid.Origin.Y,
							_grid.Origin.Z + fz * size
						});

					tryAddRay(
						3,
						{
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
			for (int32_t sy = 0; sy < subSample; ++sy)
			{
				for (int32_t sx = 0; sx < subSample; ++sx)
				{
					float fx = float(x) + (float(sx) + 0.5f) * invSubSample;
					float fy = float(y) + (float(sy) + 0.5f) * invSubSample;

					tryAddRay(
						4,
						{
							_grid.Origin.X + fx * size,
							_grid.Origin.Y + fy * size,
							_grid.Origin.Z
						});

					tryAddRay(
						5,
						{
							_grid.Origin.X + fx * size,
							_grid.Origin.Y + fy * size,
							_grid.Origin.Z + float(_grid.SizeZ) * size
						});
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
	Vec3 lightEnergy = Mul(light.Color, light.Intensity);
	Vec3 lightDirection = Normalize(light.Direction);

	auto addRay = [this, &light, lightEnergy, lightDirection, subSample](const Vec3& origin)
		{
			Vec3 dir = Normalize(Sub(origin, light.Position));

			if (Dot(dir, dir) <= Epsilon)
				return;

			float cone = SpotConeAttenuation(
				lightDirection,
				light.InnerCos,
				light.OuterCos,
				dir);

			if (cone <= 0.0f)
				return;

			Vec3 rayEnergy = Mul(lightEnergy, cone);

			if (_params.RayMergeMode == VoxelLightMergeMode::Add)
				rayEnergy = Mul(rayEnergy, 1.0f / float(subSample * subSample));

			if (!HasEnergy(rayEnergy, _params.EnergyThreshold))
				return;

			VoxelLightRay ray{};
			ray.Position = light.Position;
			ray.Direction = dir;
			ray.Energy = rayEnergy;
			ray.OriginTotalDistance = 0.0f;
			ray.Falloff = light.Falloff;

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
			_params.RayMergeMode);

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
					_params.RayMergeMode);

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
	VoxelLightMergeMode mode)
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

		MergeVoxelLightData(target.Cells[slot].Data, sourceCell.Data, mode);
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

struct BlurSample
{
	int32_t Dx;
	int32_t Dy;
	int32_t Dz;
	float Weight;
};

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

void VoxelLightBaker::BlurLightField(const bool colorOnly)
{
	float strength = std::clamp(_params.BlurStrength, 0.0f, 1.0f);
	int32_t passes = std::max(0, _params.BlurPasses);

	if (strength <= 0.0f || passes <= 0)
		return;

	int32_t count = _field.SizeX * _field.SizeY * _field.SizeZ;

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

			for (int32_t z = 0; z < _field.SizeZ; ++z)
			{
				for (int32_t y = 0; y < _field.SizeY; ++y)
				{
					for (int32_t x = 0; x < _field.SizeX; ++x)
					{
						int32_t index = FieldIndex(_field, x, y, z);

						Vec3 colorSum = Zero3();
						Vec3 dirSum = Zero3();
						float weightSum = 0.0f;

						for (int32_t i = 0; i < sampleCount; ++i)
						{
							const BlurSample& sample = samples[i];

							int32_t nx = x + sample.Dx;
							int32_t ny = y + sample.Dy;
							int32_t nz = z + sample.Dz;

							if (nx < 0 || ny < 0 || nz < 0 ||
								nx >= _field.SizeX ||
								ny >= _field.SizeY ||
								nz >= _field.SizeZ)
							{
								continue;
							}

							int32_t ni = FieldIndex(_field, nx, ny, nz);

							colorSum = Add(colorSum, Mul(colors[ni], sample.Weight));
							weightSum += sample.Weight;

							if (!colorOnly)
								dirSum = Add(dirSum, Mul(directions[ni], sample.Weight));
						}

						if (weightSum > Epsilon)
						{
							Vec3 blurColor = Mul(colorSum, 1.0f / weightSum);
							tempColor[index] = Lerp(colors[index], blurColor, strength);
							
							if (!colorOnly) 
							{
								Vec3 blurDir = Mul(dirSum, 1.0f / weightSum);
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
				return FieldIndex(_field, layer, v, u); // x = layer, y = v, z = u

			case 2:
			case 3:
				return FieldIndex(_field, u, layer, v); // x = u, y = layer, z = v

			default:
				return FieldIndex(_field, u, v, layer); // x = u, y = v, z = layer
			}
		};

	switch (face)
	{
	case 0:
		n = Vec3{ -1.0f, 0.0f, 0.0f };
		t = Vec3{ 0.0f, 0.0f, 1.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.SizeZ;
		height = _field.SizeY;
		layers = _field.SizeX;
		break;

	case 1:
		n = Vec3{ 1.0f, 0.0f, 0.0f };
		t = Vec3{ 0.0f, 0.0f, 1.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.SizeZ;
		height = _field.SizeY;
		layers = _field.SizeX;
		break;

	case 2:
		n = Vec3{ 0.0f, -1.0f, 0.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 0.0f, 1.0f };
		width = _field.SizeX;
		height = _field.SizeZ;
		layers = _field.SizeY;
		break;

	case 3:
		n = Vec3{ 0.0f, 1.0f, 0.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 0.0f, 1.0f };
		width = _field.SizeX;
		height = _field.SizeZ;
		layers = _field.SizeY;
		break;

	case 4:
		n = Vec3{ 0.0f, 0.0f, -1.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.SizeX;
		height = _field.SizeY;
		layers = _field.SizeZ;
		break;

	default:
		n = Vec3{ 0.0f, 0.0f, 1.0f };
		t = Vec3{ 1.0f, 0.0f, 0.0f };
		b = Vec3{ 0.0f, 1.0f, 0.0f };
		width = _field.SizeX;
		height = _field.SizeY;
		layers = _field.SizeZ;
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

				Vec3 solvedDir = Normalize(Add(n, Add(Mul(t, du), Mul(b, dv))));

				float oldLen = std::sqrt(Dot(directions[fi], directions[fi]));
				float energy = EnergyScore(colors[fi]);
				float outLen = oldLen > Epsilon ? oldLen : energy;

				if (outLen > Epsilon)
					directions[fi] = Mul(solvedDir, outLen);
				else
					directions[fi] = Zero3();
			}
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
				outDir = Add(Add(outgoing.DirectionR, outgoing.DirectionG), outgoing.DirectionB);
			else
				outDir = Add(
					Add(
						Mul(outgoing.DirectionR, 0.2126f),
						Mul(outgoing.DirectionG, 0.7152f)),
					Mul(outgoing.DirectionB, 0.0722f));

			if (normDir)
				outDir = Normalize(outDir);

			_field.Direction[face][i] = outDir;
		}
	}

	if (_params.SmoothDir.Iterations > 0)
	{
		for (int i = 0; i < 6; i++)
			ReconstructDirectionSurfaceForFace(i);
	};

	if (_params.BlurPasses > 0)
		BlurLightField(_params.SmoothDir.Iterations == 0);

	if (_params.FillEmptyDir)
		AdjustLightFieldDirections();

}
