#include "pch.h"
#include <algorithm>

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

void CopyMemory2(uint8_t* src, uint8_t* dst, uint32_t size)
{
	memcpy(dst, src, size);
} 


int CompareMemory(uint8_t* src, uint8_t* dst, uint32_t size)
{
    return memcmp(dst, src, size);
}


void ImagePack(uint32_t srcWidth, uint32_t srcHeight, char* srcData, uint32_t dstWidth, uint32_t dstHeight, char* dstData, uint32_t pixelSize)
{
    auto dstRowSize = pixelSize * dstWidth;
    auto srcRowSize = pixelSize * srcWidth;

    auto dstSize = dstRowSize * dstHeight;
    memset(dstData, 0, dstSize);

    int curY = 0;
    char* srcRow = srcData;
    char* dstRow = dstData;

    while (curY < srcHeight) {

        memcpy(dstRow, srcRow, srcRowSize);
        curY++;
        dstRow += dstRowSize;
        srcRow += srcRowSize;
    }
}

void RgbToBgr(uint32_t width, uint32_t height,
    const char* srcData, char* dstData,
    uint32_t pixelSizeByte)
{
    if (!srcData || !dstData)
        return;

    if (pixelSizeByte != 3 && pixelSizeByte != 4)
        return;

    const uint32_t rowSize = width * pixelSizeByte;

    int curY = 0;
    const char* srcRow = srcData;
    char* dstRow = dstData;

    while (curY < (int)height)
    {
        // In-place: swap bytes directly in destination row.
        if (srcRow == dstRow)
        {
            char* p = dstRow;
            uint32_t x = 0;
            while (x < width)
            {
                char tmp = p[0];  // R
                p[0] = p[2];      // B
                p[2] = tmp;       // R
                p += pixelSizeByte;
                x++;
            }
        }
        else
        {
            const char* s = srcRow;
            char* d = dstRow;

            uint32_t x = 0;
            while (x < width)
            {
                // RGB -> BGR
                d[0] = s[2];
                d[1] = s[1];
                d[2] = s[0];

                if (pixelSizeByte == 4)
                    d[3] = s[3]; // keep alpha

                s += pixelSizeByte;
                d += pixelSizeByte;
                x++;
            }
        }

        curY++;
        srcRow += rowSize;
        dstRow += rowSize;
    }
}

 void ImageResizeBilinearU8(
    uint32_t srcW, uint32_t srcH, const uint8_t* src,
    uint32_t dstW, uint32_t dstH, uint8_t* dst,
    uint32_t channels)
{
    if (!src || !dst || srcW == 0 || srcH == 0 || dstW == 0 || dstH == 0 || channels == 0)
        return;

    // Precompute X mapping: x0, x1 and weight wx in [0..256]
    struct XMap { uint32_t x0, x1; uint16_t wx; };
    std::vector<XMap> xmap(dstW);

    const float scaleX = static_cast<float>(srcW) / static_cast<float>(dstW);
    const float scaleY = static_cast<float>(srcH) / static_cast<float>(dstH);

    for (uint32_t x = 0; x < dstW; ++x)
    {
        float sx = (static_cast<float>(x) + 0.5f) * scaleX - 0.5f; // pixel-center mapping
        int x0 = static_cast<int>(std::floor(sx));
        float fx = sx - static_cast<float>(x0);

        if (x0 < 0) { x0 = 0; fx = 0.0f; }
        int x1 = x0 + 1;
        if (x1 >= static_cast<int>(srcW)) { x1 = x0; fx = 0.0f; }

        uint16_t wx = static_cast<uint16_t>(std::clamp<int>(static_cast<int>(fx * 256.0f + 0.5f), 0, 256));
        xmap[x] = { static_cast<uint32_t>(x0), static_cast<uint32_t>(x1), wx };
    }

    const uint32_t srcStride = srcW * channels;
    const uint32_t dstStride = dstW * channels;

    for (uint32_t y = 0; y < dstH; ++y)
    {
        float sy = (static_cast<float>(y) + 0.5f) * scaleY - 0.5f;
        int y0 = static_cast<int>(std::floor(sy));
        float fy = sy - static_cast<float>(y0);

        if (y0 < 0) { y0 = 0; fy = 0.0f; }
        int y1 = y0 + 1;
        if (y1 >= static_cast<int>(srcH)) { y1 = y0; fy = 0.0f; }

        const uint16_t wy = static_cast<uint16_t>(std::clamp<int>(static_cast<int>(fy * 256.0f + 0.5f), 0, 256));
        const uint32_t wy0 = 256u - wy;

        const uint8_t* row0 = src + static_cast<uint32_t>(y0) * srcStride;
        const uint8_t* row1 = src + static_cast<uint32_t>(y1) * srcStride;
        uint8_t* out = dst + y * dstStride;

        for (uint32_t x = 0; x < dstW; ++x)
        {
            const auto& xm = xmap[x];
            const uint32_t wx = xm.wx;
            const uint32_t wx0 = 256u - wx;

            const uint8_t* p00 = row0 + (xm.x0 * channels);
            const uint8_t* p01 = row0 + (xm.x1 * channels);
            const uint8_t* p10 = row1 + (xm.x0 * channels);
            const uint8_t* p11 = row1 + (xm.x1 * channels);

            // Per-channel bilinear: do X lerp on both rows, then Y lerp.
            for (uint32_t c = 0; c < channels; ++c)
            {
                const uint32_t top = p00[c] * wx0 + p01[c] * wx; // 0..(255*256)
                const uint32_t bottom = p10[c] * wx0 + p11[c] * wx;

                // Combine with Y. Add 0x8000 for rounding before >> 16.
                const uint32_t v = top * wy0 + bottom * wy;         // 0..(255*256*256)
                out[x * channels + c] = static_cast<uint8_t>((v + 0x8000u) >> 16);
            }
        }
    }
}

void ImageCopyChannel(uint8_t* src, uint8_t* dst, const uint32_t width, uint32_t height, const uint32_t srcRowSize, const uint32_t dstRowSize, const  uint32_t srcOfs, const uint32_t dstOfs, const uint32_t cSize)
{
    uint8_t* curSrc = src + srcOfs;
	uint8_t* curDst = dst + dstOfs; 

	const uint32_t srcPixelSize = srcRowSize / width; 
	const uint32_t dstPixelSize = dstRowSize / width;

    while (height > 0) {
        uint32_t curWidth = width;
		while (curWidth > 0) {

			for (uint32_t c = 0; c < cSize; c++)
				curDst[c] = curSrc[c];  

			curSrc += srcPixelSize;
			curDst += dstPixelSize;
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


#ifdef _WINDOWS

    void SleepFor(uint64_t time) {

        uint32_t ms = static_cast<uint32_t>((time + 999999) / 1000000);
        if (ms == 0)
            ms = 1;

        HANDLE evt = CreateEvent(nullptr, FALSE, FALSE, nullptr);
        if (!evt)
        {
            Sleep(ms);
            return;
        }

        MMRESULT id = timeSetEvent(
            ms,
            1,                 
            (LPTIMECALLBACK)evt,
            0,
            TIME_ONESHOT | TIME_CALLBACK_EVENT_SET
        );

        if (id == 0)
        {
            CloseHandle(evt);
            Sleep(ms);             
            return;
        }


        WaitForSingleObject(evt, INFINITE);

        timeKillEvent(id);
        CloseHandle(evt);
    }

#else

    void SleepFor(uint64_t time)
    {

	    auto duration = std::chrono::nanoseconds(time);

	    std::this_thread::sleep_for(duration);

    }

#endif

uint64_t Now() {

	auto now = std::chrono::high_resolution_clock::now();

	auto duration = now.time_since_epoch();

	return std::chrono::duration_cast<std::chrono::nanoseconds>(duration).count();
}