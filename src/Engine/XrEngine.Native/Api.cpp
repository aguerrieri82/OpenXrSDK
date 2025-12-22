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