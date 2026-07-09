#include "pch.h"

MeshVoxelizer* APIENTRY MeshVoxelizerCreate()
{
    return new MeshVoxelizer();
}

void APIENTRY MeshVoxelizerDestroy(MeshVoxelizer* voxelizer)
{
    delete voxelizer;
}

MeshVoxelGrid* APIENTRY MeshVoxelizerVoxelize(
    MeshVoxelizer* voxelizer,
    const VertexData* vertices,
    int32_t vertexCount,
    const uint32_t* indices,
    int32_t indexCount,
    const Bounds3* bounds,
    const VoxelGridDesc* grid,
    const VoxelizeMeshParams* params)
{
    if (voxelizer == nullptr ||
        vertices == nullptr ||
        indices == nullptr ||
        bounds == nullptr ||
        grid == nullptr ||
        params == nullptr)
        return nullptr;

    MeshVoxelGrid result = voxelizer->Voxelize(
        vertices,
        vertexCount,
        indices,
        indexCount,
        *bounds,
        *grid,
        *params);

    return new MeshVoxelGrid(std::move(result));
}

void APIENTRY MeshVoxelGridDestroy(MeshVoxelGrid* voxelGrid)
{
    delete voxelGrid;
}

MeshVoxelGridView APIENTRY MeshVoxelGridGetView(const MeshVoxelGrid* voxelGrid)
{
    int size = sizeof(VoxelData);

    MeshVoxelGridView view{};

    if (voxelGrid == nullptr)
        return view;

    view.Info = voxelGrid->Info;
    view.Voxels = voxelGrid->Voxels.data();
    view.VoxelCount = static_cast<int32_t>(voxelGrid->Voxels.size());

    return view;
}

static int32_t CopyContributionToView(
    const VoxelLightContribution& source,
    VoxelLightContributionView* view)
{
    if (view == nullptr)
        return 0;

    int32_t count = static_cast<int32_t>(source.Cells.size());

    if (view->Cells == nullptr || view->CellCount <= 0)
    {
        view->CellCount = count;
        return count;
    }

    int32_t copyCount = std::min(view->CellCount, count);

    if (copyCount > 0)
    {
        std::memcpy(
            view->Cells,
            source.Cells.data(),
            sizeof(VoxelLightCell) * copyCount);
    }

    view->CellCount = copyCount;

    return count;
}

static VoxelLightContribution CopyContributionFromView(
    const VoxelLightContributionView* view)
{
    VoxelLightContribution result;

    if (view == nullptr || view->Cells == nullptr || view->CellCount <= 0)
        return result;

    result.Cells.assign(
        view->Cells,
        view->Cells + view->CellCount);

    return result;
}




static int32_t CopyLightFieldToView(
    const VoxelLightField& source,
    VoxelLightFieldView* view)
{
    if (view == nullptr)
        return 0;

    int32_t count = 0;

    for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
    {
        count = std::max(
            count,
            static_cast<int32_t>(source.Color[face].size()));

        count = std::max(
            count,
            static_cast<int32_t>(source.Direction[face].size()));
    }

    if (count > view->CellCapacity)
    {
        for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
        {
            std::free(view->Color[face]);
            std::free(view->Direction[face]);

            view->Color[face] = nullptr;
            view->Direction[face] = nullptr;
        }

        view->CellCapacity = 0;

        if (count > 0)
        {
            size_t bytes = sizeof(Vec3) * static_cast<size_t>(count);

            for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
            {
                view->Color[face] = static_cast<Vec3*>(std::malloc(bytes));
                view->Direction[face] = static_cast<Vec3*>(std::malloc(bytes));

                if (view->Color[face] == nullptr || view->Direction[face] == nullptr)
                {
                    FreeLightFieldView(view);
                    return 0;
                }
            }

            view->CellCapacity = count;
        }
    }

    view->Size = source.Size;

    view->CellCount = count;

    for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
    {
        const auto& color = source.Color[face];
        const auto& direction = source.Direction[face];

        if (!color.empty())
        {
            std::memcpy(
                view->Color[face],
                color.data(),
                sizeof(Vec3) * color.size());
        }

        if (!direction.empty())
        {
            std::memcpy(
                view->Direction[face],
                direction.data(),
                sizeof(Vec3) * direction.size());
        }
    }

    return count;
}


EXPORT VoxelLightBaker* APIENTRY VoxelLightBakerCreate()
{
    return new VoxelLightBaker();
}

EXPORT void APIENTRY VoxelLightBakerDestroy(
    VoxelLightBaker* baker)
{
    delete baker;
}

EXPORT void APIENTRY VoxelLightBakerSetParams(
    VoxelLightBaker* baker,
    const VoxelLightBakeParams* params)
{
    if (baker == nullptr || params == nullptr)
        return;

    baker->SetParams(*params);
}

EXPORT void APIENTRY VoxelLightBakerSetGrid(
    VoxelLightBaker* baker,
    const VoxelGridDesc* grid)
{
    if (baker == nullptr || grid == nullptr)
        return;

    baker->SetGrid(*grid);
}

EXPORT void APIENTRY VoxelLightBakerClearScene(
    VoxelLightBaker* baker)
{
    if (baker == nullptr)
        return;

    baker->ClearScene();
}

EXPORT void APIENTRY VoxelLightBakerAddMesh(
    VoxelLightBaker* baker,
    const Vec3I* origin,
    const Vec3I* size,
    const VoxelData* voxels,
    const VoxelMeshResolvedFace* faces,
    int32_t faceCount)
{
    if (baker == nullptr || origin == nullptr || size == nullptr)
        return;

    baker->AddMesh(
        *origin,
        *size,
        voxels,
        faces,
        faceCount);
}

EXPORT void APIENTRY VoxelLightBakerAddGpuMeshFaces(
    VoxelLightBaker* baker,
    const GpuVoxelFaceData* faces,
    int32_t faceCount)
{
    if (baker == nullptr)
        return;

    baker->AddGpuMeshFaces(
        faces,
        faceCount);
}


EXPORT VoxelData* APIENTRY VoxelLightBakerGetScene(VoxelLightBaker* baker, int32_t* count) {

    auto scene = baker->GetScene();
    *count = scene->size();
    return scene->data();
}

EXPORT int32_t APIENTRY VoxelLightBakerBakePointLight(
    VoxelLightBaker* baker,
    const PointLight* light,
    VoxelLightContributionView* contribution)
{
    if (baker == nullptr || light == nullptr)
        return 0;

    VoxelLightContribution result;

    baker->BakePointLight(
        *light,
        result);

    return CopyContributionToView(
        result,
        contribution);
}

EXPORT int32_t APIENTRY VoxelLightBakerBakeDirectionalLight(
    VoxelLightBaker* baker,
    const DirectionalLight* light,
    VoxelLightContributionView* contribution)
{
    if (baker == nullptr || light == nullptr)
        return 0;

    VoxelLightContribution result;

    baker->BakeDirectionalLight(
        *light,
        result);

    return CopyContributionToView(
        result,
        contribution);
}

EXPORT int32_t APIENTRY VoxelLightBakerBakeSpotLight(
    VoxelLightBaker* baker,
    const SpotLight* light,
    VoxelLightContributionView* contribution)
{
    if (baker == nullptr || light == nullptr)
        return 0;

    VoxelLightContribution result;

    baker->BakeSpotLight(
        *light,
        result);

    return CopyContributionToView(
        result,
        contribution);
}

EXPORT void APIENTRY VoxelLightBakerClearLightField(
    VoxelLightBaker* baker)
{
    if (baker == nullptr)
        return;

    baker->ClearLightField();
}

EXPORT void APIENTRY VoxelLightBakerAccumulateLight(
    VoxelLightBaker* baker,
    const VoxelLightContributionView* contribution)
{
    if (baker == nullptr)
        return;

    VoxelLightContribution value = CopyContributionFromView(
        contribution);

    baker->AccumulateLight(value);
}

EXPORT int32_t APIENTRY VoxelLightBakerGetLightField(
    VoxelLightBaker* baker,
    VoxelLightFieldView* field)
{
    if (baker == nullptr || field == nullptr)
        return 0;

    const auto& curField = baker->GetLightField();

    return CopyLightFieldToView(curField, field);
}


EXPORT VoxelRayMarcher* APIENTRY VoxelRayMarcherCreate(
    VoxelLightBaker* baker)
{
    if (baker == nullptr)
        return nullptr;

    auto* marcher = new VoxelRayMarcher();

    marcher->SetContext(
        baker,
        -1);

    marcher->Prepare(baker->GetVoxelCount());

    return marcher;
}

EXPORT void APIENTRY VoxelRayMarcherDestroy(
    VoxelRayMarcher* marcher)
{
    delete marcher;
}

EXPORT bool APIENTRY VoxelRayMarcherCreateRay(
    VoxelRayMarcher* marcher,
    const VoxelLightRay* ray)
{
    if (marcher == nullptr || ray == nullptr)
        return false;

    marcher->ClearContribution();

    return marcher->CreateRay(*ray, 0);
}

EXPORT bool APIENTRY VoxelRayMarcherStep(
    VoxelRayMarcher* marcher)
{
    if (marcher == nullptr)
        return false;

    if (!marcher->Step()) {

        auto& nextRays = marcher->NextRays();

        if (nextRays.size() > 0)
        {
            nextRays.clear();

            marcher->ClearContribution();
            marcher->CreateRay(nextRays[0], marcher->Ray().BounceCount + 1);
            return true;
        }

        return false;
    }
}

EXPORT void APIENTRY VoxelRayMarcherGetState(
    VoxelRayMarcher* marcher,
    VoxelRayDebugState* state)
{
    if (marcher == nullptr || state == nullptr)
        return;

    marcher->GetDebugState(*state);
}

EXPORT int32_t APIENTRY VoxelRayMarcherGetContribution(
    VoxelRayMarcher* marcher,
    VoxelLightContributionView* contribution)
{
    if (marcher == nullptr)
        return 0;

    return CopyContributionToView(
        marcher->Contribution(),
        contribution);
}

EXPORT void APIENTRY FreeLightFieldView(
    VoxelLightFieldView* view)
{
    if (view == nullptr)
        return;

    for (int32_t face = 0; face < VOXEL_LIGHT_FACE_COUNT; ++face)
    {
        std::free(view->Color[face]);
        std::free(view->Direction[face]);

        view->Color[face] = nullptr;
        view->Direction[face] = nullptr;
    }

    view->Size = { 0 };
    view->CellCount = 0;
    view->CellCapacity = 0;
}