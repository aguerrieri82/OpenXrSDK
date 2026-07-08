#pragma once

extern "C" {

	EXPORT void APIENTRY ImageFlipY(
        uint8_t* src, uint8_t* dst, uint32_t width, 
        uint32_t height, uint32_t rowSize);

	EXPORT void APIENTRY ImageCopyChannel(
        uint8_t* src, uint8_t* dst, const uint32_t width, uint32_t height, 
        const uint32_t srcRowSize, const uint32_t dstRowSize, 
        const uint32_t srcOfs, const uint32_t dstOfs, const uint32_t cSize);

	EXPORT void APIENTRY CopyMemory2(uint8_t* src, uint8_t* dst, uint32_t size);

	EXPORT int APIENTRY CompareMemory(uint8_t* src, uint8_t* dst, uint32_t size);

    EXPORT bool APIENTRY ImagePackToRgba8(
        const uint8_t* src, uint8_t* dst,
        unsigned int width, unsigned int height,
        unsigned int srcChannels, unsigned int srcRowAlignment);

	EXPORT void APIENTRY ImagePack(
        uint32_t srcWidth, uint32_t srcHeight, char* srcData, 
        uint32_t dstWidth, uint32_t dstHeight, char* dstData,  uint32_t pixelSize);

	EXPORT void APIENTRY RgbToBgr(
        uint32_t width, uint32_t height, 
        const char* srcData, char* dstData, uint32_t pixelSizeByte);

	EXPORT void APIENTRY ImageResizeBilinearU8(
		uint32_t srcW, uint32_t srcH, const uint8_t* src,
		uint32_t dstW, uint32_t dstH, uint8_t* dst,
		uint32_t channels);

	EXPORT void APIENTRY SleepUntil(uint64_t timeNs);

	EXPORT void APIENTRY SleepFor(uint64_t timeNs);

	EXPORT uint64_t APIENTRY Now();

	EXPORT int APIENTRY RdcTriggerCapture();

	EXPORT int APIENTRY RdcEndFrameCapture(bool launchReplay);

	EXPORT int APIENTRY RdcStartFrameCapture();

}


extern "C" {

    EXPORT MeshVoxelizer* APIENTRY MeshVoxelizerCreate();

    EXPORT void APIENTRY MeshVoxelizerDestroy(
        MeshVoxelizer* voxelizer);

    EXPORT MeshVoxelGrid* APIENTRY MeshVoxelizerVoxelize(
        MeshVoxelizer* voxelizer,
        const VertexData* vertices,
        int32_t vertexCount,
        const uint32_t* indices,
        int32_t indexCount,
        const Bounds3* bounds,
        const VoxelGridDesc* grid,
        const VoxelizeMeshParams* params);

    EXPORT void APIENTRY MeshVoxelGridDestroy(
        MeshVoxelGrid* voxelGrid);

	EXPORT MeshVoxelGridView APIENTRY MeshVoxelGridGetView(
		const MeshVoxelGrid* voxelGrid);
}


extern "C"
{
    EXPORT VoxelLightBaker* APIENTRY VoxelLightBakerCreate();

    EXPORT void APIENTRY VoxelLightBakerDestroy(
        VoxelLightBaker* baker);

    EXPORT void APIENTRY VoxelLightBakerSetParams(
        VoxelLightBaker* baker,
        const VoxelLightBakeParams* params);

    EXPORT void APIENTRY VoxelLightBakerSetGrid(
        VoxelLightBaker* baker,
        const VoxelGridDesc* grid);

    EXPORT void APIENTRY VoxelLightBakerClearScene(
        VoxelLightBaker* baker);

    EXPORT void APIENTRY VoxelLightBakerAddMesh(
        VoxelLightBaker* baker,
        const Int3* origin,
        const Int3* size,
        const VoxelData* voxels,
        const VoxelMeshResolvedFace* faces,
        int32_t faceCount);

    EXPORT int32_t APIENTRY VoxelLightBakerBakeSpotLight(
        VoxelLightBaker* baker,
        const SpotLight* light,
        VoxelLightContributionView* contribution);

    EXPORT int32_t APIENTRY VoxelLightBakerBakeDirectionalLight(
        VoxelLightBaker* baker,
        const DirectionalLight* light,
        VoxelLightContributionView* contribution);

    EXPORT int32_t APIENTRY VoxelLightBakerBakePointLight(
        VoxelLightBaker* baker,
        const PointLight* light,
        VoxelLightContributionView* contribution);

    EXPORT void APIENTRY VoxelLightBakerClearLightField(
        VoxelLightBaker* baker);

    EXPORT void APIENTRY VoxelLightBakerAccumulateLight(
        VoxelLightBaker* baker,
        const VoxelLightContributionView* contribution);

    EXPORT int32_t APIENTRY VoxelLightBakerGetLightField(
        VoxelLightBaker* baker,
        VoxelLightFieldView* field);

    EXPORT void APIENTRY VoxelLightBakerAddGpuMeshFaces(
        VoxelLightBaker* baker,
        const GpuVoxelFaceData* faces,
        int32_t faceCount);


    EXPORT void APIENTRY FreeLightFieldView(VoxelLightFieldView* view);


    EXPORT VoxelData* APIENTRY VoxelLightBakerGetScene(VoxelLightBaker* baker, int32_t* count);


    EXPORT VoxelRayMarcher* APIENTRY VoxelRayMarcherCreate(
        VoxelLightBaker* baker);

    EXPORT void APIENTRY VoxelRayMarcherDestroy(
        VoxelRayMarcher* marcher);

    EXPORT bool APIENTRY VoxelRayMarcherCreateRay(
        VoxelRayMarcher* marcher,
        const VoxelLightRay* ray);

    EXPORT bool APIENTRY VoxelRayMarcherStep(
        VoxelRayMarcher* marcher);

    EXPORT void APIENTRY VoxelRayMarcherGetState(
        VoxelRayMarcher* marcher,
        VoxelRayDebugState* state);

    EXPORT int32_t APIENTRY VoxelRayMarcherGetContribution(
        VoxelRayMarcher* marcher,
        VoxelLightContributionView* contribution);

}