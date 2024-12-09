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

void CopyMemory(uint8_t* src, uint8_t* dst, uint32_t size)
{
	memcpy(dst, src, size);
}


int CompareMemory(uint8_t* src, uint8_t* dst, uint32_t size)
{
    return memcmp(dst, src, size);
}


void ImageCopyChannel(uint8_t* src, uint8_t* dst, const uint32_t width, uint32_t height, const uint32_t rowSize, const  uint32_t srcOfs, const uint32_t dstOfs, const uint32_t cSize)
{
    uint8_t* curSrc = src + srcOfs;
	uint8_t* curDst = dst + dstOfs; 

	const uint32_t pixelSize = rowSize / width; 

    while (height > 0) {
        uint32_t curWidth = width;
		while (curWidth > 0) {

			for (uint32_t c = 0; c < cSize; c++)
				curDst[c] = curSrc[c];  

			curSrc += pixelSize;
			curDst += pixelSize;
			curWidth--;
		}   
        height--;
    }
}

void SleepUntil(uint64_t time)
{
	auto duration= std::chrono::nanoseconds(time);

	auto timePoint = std::chrono::time_point<std::chrono::high_resolution_clock>(duration);

	std::this_thread::sleep_until(timePoint);
}

void SleepFor(uint64_t time)
{
	auto duration = std::chrono::nanoseconds(time);

	std::this_thread::sleep_for(duration);
}

uint64_t Now() {

	auto now = std::chrono::high_resolution_clock::now();

	auto duration = now.time_since_epoch();

	return std::chrono::duration_cast<std::chrono::nanoseconds>(duration).count();
}