#include "pch.h"



namespace
{
    constexpr float Epsilon = 1e-7f;

    inline Vec3 Sub(const Vec3& a, const Vec3& b)
    {
        return { a.X - b.X, a.Y - b.Y, a.Z - b.Z };
    }

    inline Vec3 Cross(const Vec3& a, const Vec3& b)
    {
        return
        {
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        };
    }

    inline float Dot(const Vec3& a, const Vec3& b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    inline Vec3 Normalize(const Vec3& v)
    {
        float lenSq = Dot(v, v);

        if (lenSq <= Epsilon)
            return { 0.0f, 0.0f, 0.0f };

        float invLen = 1.0f / std::sqrt(lenSq);

        return
        {
            v.X * invLen,
            v.Y * invLen,
            v.Z * invLen
        };
    }

    inline float AxisCoord(const Vec3& p, VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return p.X;

        case VoxelScanAxis::Y:
            return p.Y;

        default:
            return p.Z;
        }
    }

    inline float ProjectU(const Vec3& p, VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return p.Y;

        case VoxelScanAxis::Y:
            return p.X;

        default:
            return p.X;
        }
    }

    inline float ProjectV(const Vec3& p, VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return p.Z;

        case VoxelScanAxis::Y:
            return p.Z;

        default:
            return p.Y;
        }
    }

    inline Vec3 AxisDir(VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return { 1.0f, 0.0f, 0.0f };

        case VoxelScanAxis::Y:
            return { 0.0f, 1.0f, 0.0f };

        default:
            return { 0.0f, 0.0f, 1.0f };
        }
    }

    inline VoxelFace NegFace(VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return VoxelFace::NegX;

        case VoxelScanAxis::Y:
            return VoxelFace::NegY;

        default:
            return VoxelFace::NegZ;
        }
    }

    inline VoxelFace PosFace(VoxelScanAxis axis)
    {
        switch (axis)
        {
        case VoxelScanAxis::X:
            return VoxelFace::PosX;

        case VoxelScanAxis::Y:
            return VoxelFace::PosY;

        default:
            return VoxelFace::PosZ;
        }
    }

    inline VoxelTriangleSide OppositeSide(VoxelTriangleSide side)
    {
        if (side == VoxelTriangleSide::Front)
            return VoxelTriangleSide::Back;

        if (side == VoxelTriangleSide::Back)
            return VoxelTriangleSide::Front;

        return VoxelTriangleSide::None;
    }

    inline VoxelFaceData EmptyFace()
    {
        VoxelFaceData face{};
        face.TriangleId = -1;
        face.Side = VoxelTriangleSide::None;
        return face;
    }

    inline float Lerp3(float a, float b, float c, float wa, float wb, float wc)
    {
        return a * wa + b * wb + c * wc;
    }

    inline Vec2 Lerp3(const Vec2& a, const Vec2& b, const Vec2& c, float wa, float wb, float wc)
    {
        return
        {
            a.X * wa + b.X * wb + c.X * wc,
            a.Y * wa + b.Y * wb + c.Y * wc
        };
    }

    inline int32_t ClampInt(int32_t v, int32_t min, int32_t max)
    {
        if (v < min)
            return min;

        if (v > max)
            return max;

        return v;
    }
}

MeshVoxelizer::MeshVoxelizer()
{}

MeshVoxelGrid MeshVoxelizer::Voxelize(
    const VertexData* vertices,
    int32_t vertexCount,
    const uint32_t* indices,
    int32_t indexCount,
    const Bounds3& bounds,
    const VoxelGridDesc& grid,
    const VoxelizeMeshParams& params)
{
    _vertices = vertices;
    _vertexCount = vertexCount;

    _indices = indices;
    _indexCount = indexCount;

    _bounds = bounds;
    _grid = grid;
    _params = params;

    if (_params.ScanSubdiv <= 0)
        _params.ScanSubdiv = 1;

    _result = {};
    _triangles.clear();
    _indexX = {};
    _indexY = {};
    _indexZ = {};
    _faceBuild.clear();

    ComputeSubGrid();

    int32_t voxelCount =
        _result.Info.Size.X *
        _result.Info.Size.Y *
        _result.Info.Size.Z;

    _result.Voxels.resize(voxelCount);

    for (VoxelData& voxel : _result.Voxels)
    {
        voxel.Status = VoxelStatus::Free;
        voxel.Occupancy = 0.0f;

        for (int32_t i = 0; i < 6; ++i)
            voxel.Faces[i] = EmptyFace();
    }

    _faceBuild.resize(voxelCount * 6);

    for (VoxelFaceBuildData& face : _faceBuild)
    {
        face.HitCount = 0;
        face.LastHit = EmptyFace();
    }

    BuildTriangles();
    BuildProjectedIndices();

    SurfacePass(VoxelScanAxis::X);
    SurfacePass(VoxelScanAxis::Y);
    SurfacePass(VoxelScanAxis::Z);

    ResolveFaces();

    SolidPass(VoxelScanAxis::X);
    SolidPass(VoxelScanAxis::Y);
    SolidPass(VoxelScanAxis::Z);

    return std::move(_result);
}

void MeshVoxelizer::ComputeSubGrid()
{
    const float invVoxelSize = 1.0f / _grid.VoxelSize;

    int32_t minX = static_cast<int32_t>(std::floor((_bounds.Min.X - _grid.Origin.X) * invVoxelSize));
    int32_t minY = static_cast<int32_t>(std::floor((_bounds.Min.Y - _grid.Origin.Y) * invVoxelSize));
    int32_t minZ = static_cast<int32_t>(std::floor((_bounds.Min.Z - _grid.Origin.Z) * invVoxelSize));

    int32_t maxX = static_cast<int32_t>(std::floor((_bounds.Max.X - _grid.Origin.X) * invVoxelSize)) + 1;
    int32_t maxY = static_cast<int32_t>(std::floor((_bounds.Max.Y - _grid.Origin.Y) * invVoxelSize)) + 1;
    int32_t maxZ = static_cast<int32_t>(std::floor((_bounds.Max.Z - _grid.Origin.Z) * invVoxelSize)) + 1;

    minX = ClampInt(minX, 0, _grid.Size.X);
    minY = ClampInt(minY, 0, _grid.Size.Y);
    minZ = ClampInt(minZ, 0, _grid.Size.Z);

    maxX = ClampInt(maxX, 0, _grid.Size.X);
    maxY = ClampInt(maxY, 0, _grid.Size.Y);
    maxZ = ClampInt(maxZ, 0, _grid.Size.Z);

    _result.Info.Origin =
    {
        minX,
        minY,
        minZ
    };

    _result.Info.Size =
    {
        std::max(0, maxX - minX),
        std::max(0, maxY - minY),
        std::max(0, maxZ - minZ)
    };
}

void MeshVoxelizer::BuildTriangles()
{
    int32_t triCount = _indexCount / 3;

    _triangles.reserve(triCount);

    for (int32_t triId = 0; triId < triCount; ++triId)
    {
        uint32_t i0 = _indices[triId * 3 + 0];
        uint32_t i1 = _indices[triId * 3 + 1];
        uint32_t i2 = _indices[triId * 3 + 2];

        if (i0 >= static_cast<uint32_t>(_vertexCount) ||
            i1 >= static_cast<uint32_t>(_vertexCount) ||
            i2 >= static_cast<uint32_t>(_vertexCount))
            continue;

        const VertexData& v0 = _vertices[i0];
        const VertexData& v1 = _vertices[i1];
        const VertexData& v2 = _vertices[i2];

        Vec3 e0 = Sub(v1.Pos, v0.Pos);
        Vec3 e1 = Sub(v2.Pos, v0.Pos);

        Vec3 n = Normalize(Cross(e0, e1));

        if (Dot(n, n) <= Epsilon)
            continue;

        VoxelTriangle tri{};
        tri.P0 = v0.Pos;
        tri.P1 = v1.Pos;
        tri.P2 = v2.Pos;

        tri.UV0 = v0.UV;
        tri.UV1 = v1.UV;
        tri.UV2 = v2.UV;

        tri.Normal = n;
        tri.Id = triId;

        _triangles.push_back(tri);
    }
}

void MeshVoxelizer::BuildProjectedIndices()
{
    BuildProjectedIndex(_indexX, VoxelScanAxis::X);
    BuildProjectedIndex(_indexY, VoxelScanAxis::Y);
    BuildProjectedIndex(_indexZ, VoxelScanAxis::Z);
}

void MeshVoxelizer::BuildProjectedIndex(VoxelProjectedIndex& index, VoxelScanAxis axis)
{
    index = {};
    index.ScanAxis = axis;
    index.CellSize = _grid.VoxelSize;

    switch (axis)
    {
    case VoxelScanAxis::X:
        index.SizeU = _result.Info.Size.Y;
        index.SizeV = _result.Info.Size.Z;
        index.OriginU = _grid.Origin.Y + _result.Info.Origin.Y * _grid.VoxelSize;
        index.OriginV = _grid.Origin.Z + _result.Info.Origin.Z * _grid.VoxelSize;
        break;

    case VoxelScanAxis::Y:
        index.SizeU = _result.Info.Size.X;
        index.SizeV = _result.Info.Size.Z;
        index.OriginU = _grid.Origin.X + _result.Info.Origin.X * _grid.VoxelSize;
        index.OriginV = _grid.Origin.Z + _result.Info.Origin.Z * _grid.VoxelSize;
        break;

    default:
        index.SizeU = _result.Info.Size.X;
        index.SizeV = _result.Info.Size.Y;
        index.OriginU = _grid.Origin.X + _result.Info.Origin.X * _grid.VoxelSize;
        index.OriginV = _grid.Origin.Y + _result.Info.Origin.Y * _grid.VoxelSize;
        break;
    }

    index.Cells.resize(index.SizeU * index.SizeV);

    const float invCellSize = 1.0f / index.CellSize;

    for (int32_t triIndex = 0; triIndex < static_cast<int32_t>(_triangles.size()); ++triIndex)
    {
        const VoxelTriangle& tri = _triangles[triIndex];

        float u0 = ProjectU(tri.P0, axis);
        float u1 = ProjectU(tri.P1, axis);
        float u2 = ProjectU(tri.P2, axis);

        float v0 = ProjectV(tri.P0, axis);
        float v1 = ProjectV(tri.P1, axis);
        float v2 = ProjectV(tri.P2, axis);

        float minU = std::min(u0, std::min(u1, u2));
        float maxU = std::max(u0, std::max(u1, u2));

        float minV = std::min(v0, std::min(v1, v2));
        float maxV = std::max(v0, std::max(v1, v2));

        int32_t cellMinU = static_cast<int32_t>(std::floor((minU - index.OriginU) * invCellSize));
        int32_t cellMaxU = static_cast<int32_t>(std::floor((maxU - index.OriginU) * invCellSize));

        int32_t cellMinV = static_cast<int32_t>(std::floor((minV - index.OriginV) * invCellSize));
        int32_t cellMaxV = static_cast<int32_t>(std::floor((maxV - index.OriginV) * invCellSize));

        cellMinU = ClampInt(cellMinU, 0, index.SizeU - 1);
        cellMaxU = ClampInt(cellMaxU, 0, index.SizeU - 1);

        cellMinV = ClampInt(cellMinV, 0, index.SizeV - 1);
        cellMaxV = ClampInt(cellMaxV, 0, index.SizeV - 1);

        for (int32_t v = cellMinV; v <= cellMaxV; ++v)
        {
            for (int32_t u = cellMinU; u <= cellMaxU; ++u)
                index.Cells[v * index.SizeU + u].push_back(triIndex);
        }
    }
}

void MeshVoxelizer::SurfacePass(VoxelScanAxis axis)
{
    const VoxelProjectedIndex* index = nullptr;

    switch (axis)
    {
    case VoxelScanAxis::X:
        index = &_indexX;
        break;

    case VoxelScanAxis::Y:
        index = &_indexY;
        break;

    default:
        index = &_indexZ;
        break;
    }

    const int32_t subdiv = _params.ScanSubdiv;
    const float voxelSize = _grid.VoxelSize;
    const float invVoxelSize = 1.0f / voxelSize;

    const int32_t sizeX = _result.Info.Size.X;
    const int32_t sizeY = _result.Info.Size.Y;
    const int32_t sizeZ = _result.Info.Size.Z;

    for (int32_t cellV = 0; cellV < index->SizeV; ++cellV)
    {
        for (int32_t cellU = 0; cellU < index->SizeU; ++cellU)
        {
            const std::vector<int32_t>& candidates = index->Cells[cellV * index->SizeU + cellU];

            if (candidates.empty())
                continue;

            for (int32_t sv = 0; sv < subdiv; ++sv)
            {
                float v = index->OriginV + (static_cast<float>(cellV) + (static_cast<float>(sv) + 0.5f) / subdiv) * voxelSize;

                for (int32_t su = 0; su < subdiv; ++su)
                {
                    float u = index->OriginU + (static_cast<float>(cellU) + (static_cast<float>(su) + 0.5f) / subdiv) * voxelSize;

                    for (int32_t candidate : candidates)
                    {
                        const VoxelTriangle& tri = _triangles[candidate];

                        float axisCoord = 0.0f;
                        Vec2 uv{};
                        VoxelTriangleSide side = VoxelTriangleSide::None;

                        if (!IntersectScanLineTriangle(axis, u, v, tri, axisCoord, uv, side))
                            continue;

                        int32_t globalAxisVoxel;

                        switch (axis)
                        {
                        case VoxelScanAxis::X:
                            globalAxisVoxel = static_cast<int32_t>(std::floor((axisCoord - _grid.Origin.X) * invVoxelSize));
                            break;

                        case VoxelScanAxis::Y:
                            globalAxisVoxel = static_cast<int32_t>(std::floor((axisCoord - _grid.Origin.Y) * invVoxelSize));
                            break;

                        default:
                            globalAxisVoxel = static_cast<int32_t>(std::floor((axisCoord - _grid.Origin.Z) * invVoxelSize));
                            break;
                        }

                        int32_t localX;
                        int32_t localY;
                        int32_t localZ;

                        float voxelMinU;
                        float voxelMinV;

                        switch (axis)
                        {
                        case VoxelScanAxis::X:
                            localX = globalAxisVoxel - _result.Info.Origin.X;
                            localY = cellU;
                            localZ = cellV;

                            voxelMinU = _grid.Origin.Y + (_result.Info.Origin.Y + localY) * voxelSize;
                            voxelMinV = _grid.Origin.Z + (_result.Info.Origin.Z + localZ) * voxelSize;
                            break;

                        case VoxelScanAxis::Y:
                            localX = cellU;
                            localY = globalAxisVoxel - _result.Info.Origin.Y;
                            localZ = cellV;

                            voxelMinU = _grid.Origin.X + (_result.Info.Origin.X + localX) * voxelSize;
                            voxelMinV = _grid.Origin.Z + (_result.Info.Origin.Z + localZ) * voxelSize;
                            break;

                        default:
                            localX = cellU;
                            localY = cellV;
                            localZ = globalAxisVoxel - _result.Info.Origin.Z;

                            voxelMinU = _grid.Origin.X + (_result.Info.Origin.X + localX) * voxelSize;
                            voxelMinV = _grid.Origin.Y + (_result.Info.Origin.Y + localY) * voxelSize;
                            break;
                        }

                        if (localX < 0 || localX >= sizeX ||
                            localY < 0 || localY >= sizeY ||
                            localZ < 0 || localZ >= sizeZ)
                            continue;

                        Vec2 hitPosition
                        {
                            (u - voxelMinU) * invVoxelSize,
                            (v - voxelMinV) * invVoxelSize
                        };

                        VoxelFaceData negHit{};
                        negHit.TriangleId = tri.Id;
                        negHit.UV = uv;
                        negHit.HitPosition = hitPosition;
                        negHit.Side = side;

                        VoxelFaceData posHit{};
                        posHit.TriangleId = tri.Id;
                        posHit.UV = uv;
                        posHit.HitPosition = hitPosition;
                        posHit.Side = OppositeSide(side);
               

                        AddFaceHit(localX, localY, localZ, NegFace(axis), negHit);
                        AddFaceHit(localX, localY, localZ, PosFace(axis), posHit);
                    }
                }
            }
        }
    }
}

bool MeshVoxelizer::IntersectScanLineTriangle(
    VoxelScanAxis axis,
    float u,
    float v,
    const VoxelTriangle& tri,
    float& axisCoord,
    Vec2& uv,
    VoxelTriangleSide& side) const
{
    float u0 = ProjectU(tri.P0, axis);
    float u1 = ProjectU(tri.P1, axis);
    float u2 = ProjectU(tri.P2, axis);

    float v0 = ProjectV(tri.P0, axis);
    float v1 = ProjectV(tri.P1, axis);
    float v2 = ProjectV(tri.P2, axis);

    float denom =
        (v1 - v2) * (u0 - u2) +
        (u2 - u1) * (v0 - v2);

    if (std::fabs(denom) <= Epsilon)
        return false;

    float b0 =
        ((v1 - v2) * (u - u2) +
            (u2 - u1) * (v - v2)) / denom;

    float b1 =
        ((v2 - v0) * (u - u2) +
            (u0 - u2) * (v - v2)) / denom;

    float b2 = 1.0f - b0 - b1;

    constexpr float baryEps = -1e-5f;

    if (b0 < baryEps || b1 < baryEps || b2 < baryEps)
        return false;

    float a0 = AxisCoord(tri.P0, axis);
    float a1 = AxisCoord(tri.P1, axis);
    float a2 = AxisCoord(tri.P2, axis);

    axisCoord = Lerp3(a0, a1, a2, b0, b1, b2);
    uv = Lerp3(tri.UV0, tri.UV1, tri.UV2, b0, b1, b2);

    float d = Dot(tri.Normal, AxisDir(axis));

    if (std::fabs(d) <= Epsilon)
        return false;

    side = d < 0.0f
        ? VoxelTriangleSide::Front
        : VoxelTriangleSide::Back;

    return true;
}

void MeshVoxelizer::ResolveFaces()
{
    int32_t voxelCount =
        _result.Info.Size.X *
        _result.Info.Size.Y *
        _result.Info.Size.Z;

    for (int32_t voxelIndex = 0; voxelIndex < voxelCount; ++voxelIndex)
    {
        VoxelData& voxel = _result.Voxels[voxelIndex];

        for (int32_t face = 0; face < 6; ++face)
        {
            const VoxelFaceBuildData& build = _faceBuild[voxelIndex * 6 + face];

            if (build.HitCount > 0)
                voxel.Faces[face] = build.LastHit;
        }
    }
}

void MeshVoxelizer::SolidPass(VoxelScanAxis axis)
{
    const float axisWeight = 1.0f / 3.0f;

    switch (axis)
    {
    case VoxelScanAxis::X:
    {
        for (int32_t z = 0; z < _result.Info.Size.Z; ++z)
        {
            for (int32_t y = 0; y < _result.Info.Size.Y; ++y)
            {
                bool inside = false;

                for (int32_t x = 0; x < _result.Info.Size.X; ++x)
                {
                    VoxelData& voxel = _result.Voxels[VoxelIndex(x, y, z)];
                    VoxelTriangleSide side = voxel.Faces[static_cast<int32_t>(VoxelFace::NegX)].Side;

                    if (side == VoxelTriangleSide::Front)
                    {
                        inside = true;
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                    else if (side == VoxelTriangleSide::Back)
                    {
                        if (inside)
                        {
                            voxel.Status = VoxelStatus::Occupied;
                            voxel.Occupancy += axisWeight;
                        }

                        inside = false;
                    }
                    else if (inside)
                    {
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                }
            }
        }

        break;
    }

    case VoxelScanAxis::Y:
    {
        for (int32_t z = 0; z < _result.Info.Size.Z; ++z)
        {
            for (int32_t x = 0; x < _result.Info.Size.X; ++x)
            {
                bool inside = false;

                for (int32_t y = 0; y < _result.Info.Size.Y; ++y)
                {
                    VoxelData& voxel = _result.Voxels[VoxelIndex(x, y, z)];
                    VoxelTriangleSide side = voxel.Faces[static_cast<int32_t>(VoxelFace::NegY)].Side;

                    if (side == VoxelTriangleSide::Front)
                    {
                        inside = true;
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                    else if (side == VoxelTriangleSide::Back)
                    {
                        if (inside)
                        {
                            voxel.Status = VoxelStatus::Occupied;
                            voxel.Occupancy += axisWeight;
                        }

                        inside = false;
                    }
                    else if (inside)
                    {
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                }
            }
        }

        break;
    }

    default:
    {
        for (int32_t y = 0; y < _result.Info.Size.Y; ++y)
        {
            for (int32_t x = 0; x < _result.Info.Size.X; ++x)
            {
                bool inside = false;

                for (int32_t z = 0; z < _result.Info.Size.Z; ++z)
                {
                    VoxelData& voxel = _result.Voxels[VoxelIndex(x, y, z)];
                    VoxelTriangleSide side = voxel.Faces[static_cast<int32_t>(VoxelFace::NegZ)].Side;

                    if (side == VoxelTriangleSide::Front)
                    {
                        inside = true;
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                    else if (side == VoxelTriangleSide::Back)
                    {
                        if (inside)
                        {
                            voxel.Status = VoxelStatus::Occupied;
                            voxel.Occupancy += axisWeight;
                        }

                        inside = false;
                    }
                    else if (inside)
                    {
                        voxel.Status = VoxelStatus::Occupied;
                        voxel.Occupancy += axisWeight;
                    }
                }
            }
        }

        break;
    }
    }
}

void MeshVoxelizer::AddFaceHit(
    int32_t localX,
    int32_t localY,
    int32_t localZ,
    VoxelFace face,
    const VoxelFaceData& hit)
{
    int32_t voxelIndex = VoxelIndex(localX, localY, localZ);
    int32_t buildIndex = FaceBuildIndex(voxelIndex, face);

    VoxelFaceBuildData& build = _faceBuild[buildIndex];

    build.HitCount++;
    build.LastHit = hit;
}

int32_t MeshVoxelizer::VoxelIndex(int32_t x, int32_t y, int32_t z) const
{
    return
        x +
        y * _result.Info.Size.X +
        z * _result.Info.Size.X * _result.Info.Size.Y;
}

int32_t MeshVoxelizer::FaceBuildIndex(int32_t voxelIndex, VoxelFace face) const
{
    return voxelIndex * 6 + static_cast<int32_t>(face);
}