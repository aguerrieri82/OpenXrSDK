#include "pch.h"

void ImageFlipY(uint8_t* src, uint8_t* dst, uint32_t width, uint32_t height, uint32_t rowSize)
{
	uint8_t* curSrc = src;
	uint8_t* curDst = dst + (height - 1) * rowSize;

	while (height > 0) {
		memcpy(curDst, curSrc, rowSize);
		curSrc += rowSize;
		curDst -= rowSize;
		height--;
	}
}
