#pragma once


extern "C" {

	EXPORT void APIENTRY ImageFlipY(uint8_t* src, uint8_t* dst, uint32_t width, uint32_t height, uint32_t rowSize);

	EXPORT void APIENTRY ImageCopyChannel(uint8_t* src, uint8_t* dst, const uint32_t width, uint32_t height, const uint32_t rowSize, const  uint32_t srcOfs, const uint32_t dstOfs, const uint32_t cSize);

}