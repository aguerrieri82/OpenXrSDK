#include "pch.h"



namespace
{
    constexpr float Pi = 3.14159265358979323846f;
    constexpr float Epsilon = 1e-5f;

    VoxelLightCell* FindContributionCell(
        std::vector<VoxelLightCell>& cells,
        int32_t voxelIndex)
    {
        for (VoxelLightCell& cell : cells)
        {
            if (cell.Index == voxelIndex)
                return &cell;
        }

        return nullptr;
    }

    int32_t BitmapWordCount(
        int32_t count)
    {
        return (count + 63) / 64;
    }

    bool BitmapGet(
        const std::vector<uint64_t>& bitmap,
        int32_t index)
    {
        int32_t word = index >> 6;
        int32_t bit = index & 63;

        return (bitmap[word] & (uint64_t(1) << bit)) != 0;
    }

    void BitmapSet(
        std::vector<uint64_t>& bitmap,
        int32_t index)
    {
        int32_t word = index >> 6;
        int32_t bit = index & 63;

        bitmap[word] |= uint64_t(1) << bit;
    }

    void BitmapClear(
        std::vector<uint64_t>& bitmap)
    {
        std::fill(
            bitmap.begin(),
            bitmap.end(),
            uint64_t(0));
    }

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
        VoxelLightEnergy result {};

        result.Energy = energy;
        result.DirectionR = Mul(direction, energy.X);
        result.DirectionG = Mul(direction, energy.Y);
        result.DirectionB = Mul(direction, energy.Z);

        return result;
    }

    void MergeEnergy(VoxelLightEnergy& target, const VoxelLightEnergy& source)
    {
        target.Energy = Add(target.Energy, source.Energy);
        target.DirectionR = Add(target.DirectionR, source.DirectionR);
        target.DirectionG = Add(target.DirectionG, source.DirectionG);
        target.DirectionB = Add(target.DirectionB, source.DirectionB);
    }

    void MergeFace(VoxelLightFace& target, const VoxelLightFace& source)
    {
        MergeEnergy(target.Incoming, source.Incoming);
        MergeEnergy(target.Outgoing, source.Outgoing);
    }

    void MergeVoxelLightData(VoxelLightData& target, const VoxelLightData& source)
    {
        for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
            MergeFace(target.Faces[face], source.Faces[face]);
    }

    Vec3 CollapseDirection(const VoxelLightEnergy& energy)
    {
        return Normalize(Add(Add(energy.DirectionR, energy.DirectionG), energy.DirectionB));
    }

    Vec3 FaceNormal(int32_t face)
    {
        switch (face)
        {
        case 0: return { -1.0f,  0.0f,  0.0f };
        case 1: return {  1.0f,  0.0f,  0.0f };
        case 2: return {  0.0f, -1.0f,  0.0f };
        case 3: return {  0.0f,  1.0f,  0.0f };
        case 4: return {  0.0f,  0.0f, -1.0f };
        default: return { 0.0f,  0.0f,  1.0f };
        }
    }

    bool RayBoxEnter(
        const VoxelGridDesc& grid,
        const Vec3& origin,
        const Vec3& dir,
        float& enterT)
    {
        float minAxis[3] {
            grid.Origin.X,
            grid.Origin.Y,
            grid.Origin.Z
        };

        float maxAxis[3] {
            grid.Origin.X + float(grid.SizeX) * grid.VoxelSize,
            grid.Origin.Y + float(grid.SizeY) * grid.VoxelSize,
            grid.Origin.Z + float(grid.SizeZ) * grid.VoxelSize
        };

        float originAxis[3] { origin.X, origin.Y, origin.Z };
        float dirAxis[3] { dir.X, dir.Y, dir.Z };

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
        const VoxelResolvedFace& resolved)
    {
        Vec3 albedo {
            resolved.BaseColor.X,
            resolved.BaseColor.Y,
            resolved.BaseColor.Z
        };

        float roughness = std::clamp(resolved.Roughness, 0.0f, 1.0f);
        float metallic = std::clamp(resolved.Metallic, 0.0f, 1.0f);

        Vec3 diffuse = Mul(incomingEnergy, albedo);
        Vec3 metal = incomingEnergy;

        return Mul(Lerp(diffuse, metal, metallic), roughness);
    }

    Vec3 SurfaceBounceDirection(
        const Vec3& incomingDirection,
        const VoxelResolvedFace& resolved,
        int32_t fallbackFace)
    {
        Vec3 normal = Normalize(resolved.Normal);

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
        VoxelLightData data {};
        data.Faces[face].Incoming = incoming;
        data.Faces[face].Outgoing = outgoing;
        return data;
    }
}

VoxelLightBakeParams::VoxelLightBakeParams()
{
    RaySpacingFactor = 0.5f;
    EmptyDissipation = 0.02f;
    EnergyThreshold = 0.001f;

    MaxBounceCount = 4;
    ThreadCount = 0;
}

VoxelRayMarcher::VoxelRayMarcher()
{
    _baker = nullptr;
    _workerIndex = -1;

    _ray = {};
    _ray.LastHitVoxel = -1;
    _ray.BounceCount = 0;
}

void VoxelRayMarcher::SetContext(
    VoxelLightBaker* baker,
    int32_t workerIndex)
{
    _baker = baker;
    _workerIndex = workerIndex;
}

void VoxelRayMarcher::Prepare(
    int32_t voxelCount)
{
    _local.Contribution.Cells.clear();
    _local.CellSlots.assign(voxelCount, -1);
    _local.Occupancy.assign(BitmapWordCount(voxelCount), 0);
}

void VoxelRayMarcher::TraceRange(
    int32_t startRay,
    int32_t endRay)
{
    ClearContribution();

    for (int32_t i = startRay; i < endRay; ++i)
        TraceRay(_baker->_rays[i]);
}

const VoxelLightContribution& VoxelRayMarcher::Contribution() const
{
    return _local.Contribution;
}

bool VoxelRayMarcher::CreateRay(const VoxelLightRay& ray)
{
    _ray.Position = ray.Position;
    _ray.Direction = ray.Direction;
    _ray.Energy = ray.Energy;

    _ray.X = 0;
    _ray.Y = 0;
    _ray.Z = 0;

    _ray.LastHitVoxel = -1;
    _ray.LastAffectedVoxel = -1;
    _ray.LastAffectedFace = -1;

    _ray.BounceCount = 0;

    float enterT = 0.0f;

    if (!RayBoxEnter(_baker->_grid, _ray.Position, _ray.Direction, enterT))
        return false;

    if (enterT > 0.0f)
        _ray.Position = Add(_ray.Position, Mul(_ray.Direction, enterT + Epsilon));

    return WorldToVoxel(_baker->_grid, _ray.Position, _ray.X, _ray.Y, _ray.Z);
}

void VoxelRayMarcher::TraceRay(const VoxelLightRay& ray)
{
    if (!CreateRay(ray))
        return;

    while (Step())
    {
    }
}


void VoxelRayMarcher::GetDebugState(VoxelRayDebugState& state) const
{
    state.Position = _ray.Position;
    state.Direction = _ray.Direction;
    state.Energy = _ray.Energy;

    state.X = _ray.X;
    state.Y = _ray.Y;
    state.Z = _ray.Z;

    state.LastHitVoxel = _ray.LastHitVoxel;
    state.LastAffectedVoxel = _ray.LastAffectedVoxel;
    state.LastAffectedFace = _ray.LastAffectedFace;

    state.BounceCount = _ray.BounceCount;

    state.LastVoxel = {};

    for (int32_t i = 0; i < VOXEL_LIGHT_FACE_COUNT; ++i)
        state.LastResolvedFaces[i] = {};

    if (_ray.LastAffectedVoxel < 0 || _ray.LastAffectedVoxel >= _baker->_voxelCount)
        return;

    const VoxelLightBaker::SceneVoxel& voxel = _baker->_scene[_ray.LastAffectedVoxel];

    state.LastVoxel = voxel.Voxel;

    for (int32_t i = 0; i < VOXEL_LIGHT_FACE_COUNT; ++i)
        state.LastResolvedFaces[i] = voxel.ResolvedFaces[i];
}

bool VoxelRayMarcher::Step()
{
    _ray.LastAffectedVoxel = -1;
    _ray.LastAffectedFace = -1;

    if (!HasEnergy(_ray.Energy, _baker->_params.EnergyThreshold))
        return false;

    int32_t index = VoxelIndex(_baker->_grid, _ray.X, _ray.Y, _ray.Z);

    float exitT;
    int32_t exitFace;

    if (!NextVoxelBoundary(_baker->_grid, _ray.Position, _ray.Direction, _ray.X, _ray.Y, _ray.Z, exitT, exitFace))
        return false;

    _ray.LastAffectedVoxel = index;
    _ray.LastAffectedFace = exitFace;

    const VoxelLightBaker::SceneVoxel& sceneVoxel = _baker->_scene[index];
    const VoxelFaceData& faceData = sceneVoxel.Voxel.Faces[exitFace];

    if (faceData.Side == VoxelTriangleSide::Back)
    {
        VoxelLightData data{};
        data.Faces[exitFace].Incoming = MakeEnergy(_ray.Energy, Mul(_ray.Direction, -1.0f));
        data.Faces[exitFace].Outgoing = MakeEnergy(_ray.Energy, _ray.Direction);

        AddContribution(index, data);

        return false;
    }

    if (faceData.Side == VoxelTriangleSide::Front)
    {
        if (_ray.LastHitVoxel == index)
            return false;

        const VoxelResolvedFace& resolved = sceneVoxel.ResolvedFaces[exitFace];

        Vec3 hitPosition = Add(_ray.Position, Mul(_ray.Direction, exitT));
        Vec3 incomingDirection = Mul(_ray.Direction, -1.0f);
        Vec3 outgoingDirection = SurfaceBounceDirection(_ray.Direction, resolved, exitFace);
        Vec3 outgoingEnergy = SurfaceBounceEnergy(_ray.Energy, resolved);

        VoxelLightData incomingData{};
        incomingData.Faces[exitFace].Incoming = MakeEnergy(_ray.Energy, incomingDirection);

        AddContribution(index, incomingData);

        if (!HasEnergy(outgoingEnergy, _baker->_params.EnergyThreshold))
            return false;

        if (_ray.BounceCount + 1 >= _baker->_params.MaxBounceCount)
            return false;

        Vec3 nextPosition = Add(hitPosition, Mul(outgoingDirection, Epsilon));

        int32_t outX;
        int32_t outY;
        int32_t outZ;

        if (!WorldToVoxel(_baker->_grid, nextPosition, outX, outY, outZ))
        {
            VoxelLightData outgoingData{};
            outgoingData.Faces[exitFace].Outgoing = MakeEnergy(outgoingEnergy, outgoingDirection);

            AddContribution(index, outgoingData);
            return false;
        }

        int32_t outIndex = VoxelIndex(_baker->_grid, outX, outY, outZ);

        if (outIndex != index)
        {
            VoxelLightData outgoingData{};
            outgoingData.Faces[exitFace].Outgoing = MakeEnergy(outgoingEnergy, outgoingDirection);

            AddContribution(index, outgoingData);

            _ray.Position = nextPosition;
            _ray.Direction = outgoingDirection;
            _ray.Energy = outgoingEnergy;

            _ray.X = outX;
            _ray.Y = outY;
            _ray.Z = outZ;

            _ray.LastHitVoxel = index;
            ++_ray.BounceCount;

            return true;
        }

        float outExitT;
        int32_t outExitFace;

        if (!NextVoxelBoundary(_baker->_grid, nextPosition, outgoingDirection, _ray.X, _ray.Y, _ray.Z, outExitT, outExitFace))
            return false;

        _ray.LastAffectedFace = outExitFace;

        const VoxelFaceData& outFaceData = sceneVoxel.Voxel.Faces[outExitFace];

        if (outFaceData.Side != VoxelTriangleSide::None)
            return false;

        VoxelLightData outgoingData{};
        outgoingData.Faces[outExitFace].Outgoing = MakeEnergy(outgoingEnergy, outgoingDirection);

        AddContribution(index, outgoingData);

        _ray.Position = Add(nextPosition, Mul(outgoingDirection, outExitT + Epsilon));
        _ray.Direction = outgoingDirection;
        _ray.Energy = outgoingEnergy;

        _ray.LastHitVoxel = index;
        ++_ray.BounceCount;

        StepVoxelByFace(outExitFace, _ray.X, _ray.Y, _ray.Z);

        return IsInsideGrid(_baker->_grid, _ray.X, _ray.Y, _ray.Z);
    }

    VoxelLightData data{};
    data.Faces[exitFace].Incoming = MakeEnergy(_ray.Energy, Mul(_ray.Direction, -1.0f));
    data.Faces[exitFace].Outgoing = MakeEnergy(_ray.Energy, _ray.Direction);

    AddContribution(index, data);

    float keep = std::clamp(1.0f - _baker->_params.EmptyDissipation, 0.0f, 1.0f);

    _ray.Energy = Mul(_ray.Energy, keep);
    _ray.Position = Add(_ray.Position, Mul(_ray.Direction, exitT + Epsilon));

    StepVoxelByFace(exitFace, _ray.X, _ray.Y, _ray.Z);

    return IsInsideGrid(_baker->_grid, _ray.X, _ray.Y, _ray.Z);
}

void VoxelRayMarcher::ClearContribution()
{
    _local.Contribution.Cells.clear();
    BitmapClear(_local.Occupancy);
}

void VoxelRayMarcher::AddContribution(
    int32_t voxelIndex,
    const VoxelLightData& data)
{
    if (!BitmapGet(_local.Occupancy, voxelIndex))
    {
        BitmapSet(
            _local.Occupancy,
            voxelIndex);

        VoxelLightCell cell{};
        cell.Index = voxelIndex;
        cell.Data = data;

        _local.Contribution.Cells.push_back(cell);

        return;
    }

    VoxelLightCell* cell = FindContributionCell(
        _local.Contribution.Cells,
        voxelIndex);

    if (cell == nullptr)
    {
        VoxelLightCell newCell{};
        newCell.Index = voxelIndex;
        newCell.Data = data;

        _local.Contribution.Cells.push_back(newCell);

        return;
    }

    MergeVoxelLightData(
        cell->Data,
        data);
}

VoxelLightBaker::VoxelLightBaker()
{
    _params = VoxelLightBakeParams();
    _grid = {};
    _voxelCount = 0;
    _currentPointLight = {};
    _currentContribution = nullptr;
}

VoxelLightBaker::VoxelLightBaker(const VoxelLightBakeParams& params)
{
    _params = params;
    _grid = {};
    _voxelCount = 0;
    _currentPointLight = {};
    _currentContribution = nullptr;
}

void VoxelLightBaker::SetParams(const VoxelLightBakeParams& params)
{
    _params = params;
}

void VoxelLightBaker::SetGrid(const VoxelGridDesc& grid)
{
    _grid = grid;
    _voxelCount = grid.SizeX * grid.SizeY * grid.SizeZ;

    _scene.clear();
    _scene.resize(_voxelCount);

    _lightField.clear();
    _lightField.resize(_voxelCount);

    _currentMerge.Occupancy.assign(BitmapWordCount(_voxelCount), 0);

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
    std::fill(_scene.begin(), _scene.end(), SceneVoxel {});
}

void VoxelLightBaker::AddMesh(
    const Int3& origin,
    const Int3& size,
    const VoxelData* voxels,
    const VoxelMeshResolvedFace* faces,
    int32_t faceCount)
{
    for (int32_t z = 0; z < size.Z; ++z)
    {
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

                if (src.Status != VoxelStatus::Occupied)
                    continue;

                SceneVoxel& dst = _scene[dstIndex];

                if (dst.Voxel.Status == VoxelStatus::Occupied)
                    continue;

                dst.Voxel = src;
            }
        }
    }

    for (int32_t i = 0; i < faceCount; ++i)
    {
        const VoxelMeshResolvedFace& srcFace = faces[i];

        if (srcFace.VoxelIndex < 0)
            continue;

        if (srcFace.Face < 0 || srcFace.Face >= VOXEL_LIGHT_FACE_COUNT)
            continue;

        int32_t localZ = srcFace.VoxelIndex / (size.X * size.Y);
        int32_t localRem = srcFace.VoxelIndex - localZ * size.X * size.Y;
        int32_t localY = localRem / size.X;
        int32_t localX = localRem - localY * size.X;

        int32_t dstX = origin.X + localX;
        int32_t dstY = origin.Y + localY;
        int32_t dstZ = origin.Z + localZ;

        if (!IsInsideGrid(_grid, dstX, dstY, dstZ))
            continue;

        int32_t dstIndex = VoxelIndex(_grid, dstX, dstY, dstZ);
        SceneVoxel& dst = _scene[dstIndex];

        if (dst.Voxel.Faces[srcFace.Face].Side != VoxelTriangleSide::None)
            continue;

        dst.Voxel.Faces[srcFace.Face] = srcFace.Data;
        dst.ResolvedFaces[srcFace.Face] = srcFace.Resolved;
    }
}

void VoxelLightBaker::BakePointLight(
    const PointLight& light,
    VoxelLightContribution& contribution)
{
    _currentPointLight = light;
    _currentContribution = &contribution;

    contribution.Cells.clear();
    ClearMergeState(_currentMerge);

    GeneratePointLightRays();
    TraceRays(contribution);

    ClearMergeState(_currentMerge);
    _currentContribution = nullptr;
}

void VoxelLightBaker::ClearLightField()
{
    std::fill(_lightField.begin(), _lightField.end(), VoxelLightData {});
}

void VoxelLightBaker::AccumulateLight(
    const VoxelLightContribution& contribution)
{
    for (const VoxelLightCell& cell : contribution.Cells)
    {
        if (cell.Index < 0 || cell.Index >= _voxelCount)
            continue;

        MergeVoxelLightData(_lightField[cell.Index], cell.Data);
    }
}

void VoxelLightBaker::GetLightField(
    VoxelLightField& field) const
{
    BuildLightField(field);
}

void VoxelLightBaker::GeneratePointLightRays()
{
    _rays.clear();

    float radius = std::max(_currentPointLight.FalloffDistance, _grid.VoxelSize);
    float spacing = std::max(_grid.VoxelSize * _params.RaySpacingFactor, Epsilon);

    float polarStep = std::clamp(spacing / radius, 0.01f, Pi);

    int32_t ringCount = std::max(2, int32_t(std::ceil(Pi / polarStep)));
    float actualPolarStep = Pi / float(ringCount);

    Vec3 baseEnergy = Mul(
        _currentPointLight.Color,
        _currentPointLight.Intensity);

    for (int32_t ring = 0; ring <= ringCount; ++ring)
    {
        float theta = float(ring) * actualPolarStep;
        float sinTheta = std::sin(theta);
        float cosTheta = std::cos(theta);

        float ringRadius = radius * sinTheta;
        float circumference = 2.0f * Pi * ringRadius;

        int32_t segmentCount = 1;

        if (circumference > spacing)
            segmentCount = std::max(1, int32_t(std::ceil(circumference / spacing)));

        for (int32_t segment = 0; segment < segmentCount; ++segment)
        {
            float phi = 2.0f * Pi * float(segment) / float(segmentCount);

            VoxelLightRay ray {};
            ray.Position = _currentPointLight.Position;

            ray.Direction = Normalize({
                sinTheta * std::cos(phi),
                cosTheta,
                sinTheta * std::sin(phi)
            });

            ray.Energy = baseEnergy;

            _rays.push_back(ray);
        }
    }

    if (_rays.empty())
        return;

    float invCount = 1.0f / float(_rays.size());

    for (VoxelLightRay& ray : _rays)
        ray.Energy = Mul(ray.Energy, invCount);
}

void VoxelLightBaker::TraceRays(
    VoxelLightContribution& contribution)
{
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

void VoxelLightBaker::MergeWorkerContribution(
    const VoxelLightContribution& workerContribution)
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

        if (!BitmapGet(mergeState.Occupancy, sourceCell.Index))
        {
            BitmapSet(
                mergeState.Occupancy,
                sourceCell.Index);

            target.Cells.push_back(sourceCell);

            continue;
        }

        VoxelLightCell* targetCell = FindContributionCell(
            target.Cells,
            sourceCell.Index);

        if (targetCell == nullptr)
        {
            target.Cells.push_back(sourceCell);
            continue;
        }

        MergeVoxelLightData(
            targetCell->Data,
            sourceCell.Data);
    }
}

void VoxelLightBaker::ClearMergeState(
    ContributionMergeState& mergeState)
{
    BitmapClear(mergeState.Occupancy);
}

void VoxelLightBaker::BuildLightField(
    VoxelLightField& field) const
{
    field.SizeX = _grid.SizeX;
    field.SizeY = _grid.SizeY;
    field.SizeZ = _grid.SizeZ;

    for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
    {
        field.Color[face].resize(_voxelCount);
        field.Direction[face].resize(_voxelCount);
    }

    for (int32_t i = 0; i < _voxelCount; ++i)
    {
        const VoxelLightData& src = _lightField[i];

        for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
        {
            const VoxelLightEnergy& outgoing = src.Faces[face].Outgoing;

            field.Color[face][i] = outgoing.Energy;
            field.Direction[face][i] = CollapseDirection(outgoing);
        }
    }
}


