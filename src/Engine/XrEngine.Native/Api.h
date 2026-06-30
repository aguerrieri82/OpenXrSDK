#pragma once

extern "C" {

	EXPORT void APIENTRY ImageFlipY(uint8_t* src, uint8_t* dst, uint32_t width, uint32_t height, uint32_t rowSize);

	EXPORT void APIENTRY ImageCopyChannel(uint8_t* src, uint8_t* dst, const uint32_t width, uint32_t height, const uint32_t srcRowSize, const uint32_t dstRowSize, const  uint32_t srcOfs, const uint32_t dstOfs, const uint32_t cSize);

	EXPORT void APIENTRY CopyMemory2(uint8_t* src, uint8_t* dst, uint32_t size);

	EXPORT int APIENTRY CompareMemory(uint8_t* src, uint8_t* dst, uint32_t size);

	EXPORT void APIENTRY ImagePack(uint32_t srcWidth, uint32_t srcHeight, char* srcData, uint32_t dstWidth, uint32_t dstHeight, char* dstData, uint32_t pixelSize);

	EXPORT void APIENTRY RgbToBgr(uint32_t width, uint32_t height, const char* srcData, char* dstData, uint32_t pixelSizeByte);

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