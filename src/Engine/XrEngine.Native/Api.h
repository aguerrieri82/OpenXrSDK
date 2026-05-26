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

	EXPORT void SleepUntil(uint64_t timeNs);

	EXPORT void SleepFor(uint64_t timeNs);

	EXPORT uint64_t Now();
}