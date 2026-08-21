#include "pch.h"

#define BCDEC_IMPLEMENTATION
#define BCDEC_BC4BC5_PRECISE

#include "..\..\..\third-party\bcdec\bcdec.h"

static inline size_t AlignUp(size_t value, size_t alignment)
{
    return alignment <= 1 ? value : (value + alignment - 1) & ~(alignment - 1);
}


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



void ImagePack(uint32_t srcWidth, uint32_t srcHeight, char* srcData, uint32_t dstWidth, uint32_t dstHeight, char* dstData, uint32_t pixelSize)
{
    auto dstRowSize = pixelSize * dstWidth;
    auto srcRowSize = pixelSize * srcWidth;

    auto dstSize = dstRowSize * dstHeight;
    memset(dstData, 0, dstSize);

    uint32_t curY = 0;
    char* srcRow = srcData;
    char* dstRow = dstData;

    while (curY < srcHeight) {

        memcpy(dstRow, srcRow, srcRowSize);
        curY++;
        dstRow += dstRowSize;
        srcRow += srcRowSize;
    }
}

bool ImagePackToRgba8(
    const uint8_t* src,
    uint8_t* dst,
    unsigned int width,
    unsigned int height,
    unsigned int srcChannels,
    unsigned int srcRowAlignment)
{
    if (!src || !dst || width == 0 || height == 0)
        return false;

    if (srcChannels < 1 || srcChannels > 4)
        return false;

    size_t srcPixelBytes = srcChannels;
    size_t srcTightRowBytes = (size_t)width * srcPixelBytes;
    size_t srcRowBytes = AlignUp(srcTightRowBytes, srcRowAlignment);
    size_t dstRowBytes = (size_t)width * 4;

    if (srcChannels == 4)
    {
        if (srcRowBytes == dstRowBytes)
        {
            memcpy(dst, src, dstRowBytes * height);
            return true;
        }

        for (unsigned int y = 0; y < height; y++)
        {
            memcpy(
                dst + (size_t)y * dstRowBytes,
                src + (size_t)y * srcRowBytes,
                dstRowBytes);
        }

        return true;
    }

    if (srcChannels == 3)
    {
        for (unsigned int y = 0; y < height; y++)
        {
            const uint8_t* s = src + (size_t)y * srcRowBytes;
            uint8_t* d = dst + (size_t)y * dstRowBytes;

            for (unsigned int x = 0; x < width; x++)
            {
                d[0] = s[0];
                d[1] = s[1];
                d[2] = s[2];
                d[3] = 255;

                s += 3;
                d += 4;
            }
        }

        return true;
    }

    if (srcChannels == 2)
    {
        for (unsigned int y = 0; y < height; y++)
        {
            const uint8_t* s = src + (size_t)y * srcRowBytes;
            uint8_t* d = dst + (size_t)y * dstRowBytes;

            for (unsigned int x = 0; x < width; x++)
            {
                d[0] = s[0];
                d[1] = s[1];
                d[2] = 0;
                d[3] = 255;

                s += 2;
                d += 4;
            }
        }

        return true;
    }

    // srcChannels == 1
    for (unsigned int y = 0; y < height; y++)
    {
        const uint8_t* s = src + (size_t)y * srcRowBytes;
        uint8_t* d = dst + (size_t)y * dstRowBytes;

        for (unsigned int x = 0; x < width; x++)
        {
            uint8_t v = *s++;

            d[0] = v;
            d[1] = v;
            d[2] = v;
            d[3] = 255;

            d += 4;
        }
    }

    return true;
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


void ConvertRgbToBgr(uint32_t width, uint32_t height,
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

bool ConvertRgba16ToRgba32F(
    const uint16_t* src,
    float* dst,
    uint32_t width,
    uint32_t height,
    uint32_t srcRowBytes)
{
    if (!src || !dst || width == 0 || height == 0)
        return false;

    const size_t valuesPerRow = (size_t)width * 4;
    const float scale = 1.0f / 65535.0f;

    if (srcRowBytes == 0)
        srcRowBytes = (uint32_t)(valuesPerRow * sizeof(uint16_t));

    for (uint32_t y = 0; y < height; y++)
    {
        const uint16_t* s = (const uint16_t*)((const uint8_t*)src + (size_t)y * srcRowBytes);
        float* d = dst + (size_t)y * valuesPerRow;

        size_t i = 0;

#if HAS_NEON
        const float32x4_t vscale = vdupq_n_f32(scale);

        for (; i + 8 <= valuesPerRow; i += 8)
        {
            uint16x8_t v = vld1q_u16(s + i);

            uint32x4_t lo = vmovl_u16(vget_low_u16(v));
            uint32x4_t hi = vmovl_u16(vget_high_u16(v));

            float32x4_t flo = vmulq_f32(vcvtq_f32_u32(lo), vscale);
            float32x4_t fhi = vmulq_f32(vcvtq_f32_u32(hi), vscale);

            vst1q_f32(d + i + 0, flo);
            vst1q_f32(d + i + 4, fhi);
        }

#elif HAS_SSE2
        const __m128 vscale = _mm_set1_ps(scale);
        const __m128i zero = _mm_setzero_si128();

        for (; i + 8 <= valuesPerRow; i += 8)
        {
            __m128i v = _mm_loadu_si128((const __m128i*)(s + i));

            __m128i lo = _mm_unpacklo_epi16(v, zero);
            __m128i hi = _mm_unpackhi_epi16(v, zero);

            __m128 flo = _mm_mul_ps(_mm_cvtepi32_ps(lo), vscale);
            __m128 fhi = _mm_mul_ps(_mm_cvtepi32_ps(hi), vscale);

            _mm_storeu_ps(d + i + 0, flo);
            _mm_storeu_ps(d + i + 4, fhi);
        }
#endif

        for (; i < valuesPerRow; i++)
            d[i] = (float)s[i] * scale;
    }

    return true;
}



bool ConvertRgb32FToRgba16F(
    const float* src,
    uint16_t* dst,
    uint32_t srcFloatCount)
{
    if (!src || !dst)
        return false;

    if ((srcFloatCount % 3) != 0)
        return false;

    uint32_t pixelCount = srcFloatCount / 3;

#if HAS_F16C
    for (uint32_t i = 0; i < pixelCount; i++)
    {
        const float* s = src + (size_t)i * 3;
        uint16_t* d = dst + (size_t)i * 4;

        __m128 v = _mm_set_ps(1.0f, s[2], s[1], s[0]);
        __m128i h = _mm_cvtps_ph(v, _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC);

        uint64_t packed = (uint64_t)_mm_cvtsi128_si64(h);
        memcpy(d, &packed, sizeof(uint64_t));
    }

    return true;

#elif HAS_NEON_FP16
    const float32x4_t alpha = { 0.0f, 0.0f, 0.0f, 1.0f };

    for (uint32_t i = 0; i < pixelCount; i++)
    {
        const float* s = src + (size_t)i * 3;
        uint16_t* d = dst + (size_t)i * 4;

        float32x4_t v = alpha;
        v = vsetq_lane_f32(s[0], v, 0);
        v = vsetq_lane_f32(s[1], v, 1);
        v = vsetq_lane_f32(s[2], v, 2);

        float16x4_t h = vcvt_f16_f32(v);
        vst1_u16(d, vreinterpret_u16_f16(h));
    }

    return true;

#else
    for (uint32_t i = 0; i < pixelCount; i++)
    {
        const float* s = src + (size_t)i * 3;
        uint16_t* d = dst + (size_t)i * 4;

        for (int c = 0; c < 4; c++)
        {
            float f = c == 3 ? 1.0f : s[c];

            uint32_t x;
            memcpy(&x, &f, sizeof(uint32_t));

            uint32_t sign = (x >> 16) & 0x8000;
            uint32_t mantissa = x & 0x007FFFFF;
            int exp = (int)((x >> 23) & 0xFF) - 127 + 15;

            if (exp <= 0)
            {
                if (exp < -10)
                {
                    d[c] = (uint16_t)sign;
                    continue;
                }

                mantissa |= 0x00800000;

                uint32_t shift = (uint32_t)(14 - exp);
                uint32_t round = (1u << (shift - 1)) - 1u;
                uint32_t sticky = (mantissa >> shift) & 1u;

                d[c] = (uint16_t)(sign | ((mantissa + round + sticky) >> shift));
                continue;
            }

            if (exp >= 31)
            {
                d[c] = (uint16_t)(sign | 0x7C00 | (mantissa ? 0x0200 : 0));
                continue;
            }

            mantissa = mantissa + 0x00000FFF + ((mantissa >> 13) & 1u);

            if (mantissa & 0x00800000)
            {
                mantissa = 0;
                exp++;

                if (exp >= 31)
                {
                    d[c] = (uint16_t)(sign | 0x7C00);
                    continue;
                }
            }

            d[c] = (uint16_t)(sign | ((uint32_t)exp << 10) | (mantissa >> 13));
        }
    }

    return true;
#endif
}

bool ImageDecodeBC(const uint8_t* src, int width, int height, BCFormat format, uint8_t* dst)
{
    if (!src || !dst || width <= 0 || height <= 0)
        return false;

    const int blocksX = (width + 3) / 4;
    const int blocksY = (height + 3) / 4;

    int blockSize;
    void (*decode)(const void*, void*, int);

    switch (format)
    {
    case BCFormat::BC1:
        blockSize = BCDEC_BC1_BLOCK_SIZE;
        decode = bcdec_bc1;
        break;

    case BCFormat::BC3:
        blockSize = BCDEC_BC3_BLOCK_SIZE;
        decode = bcdec_bc3;
        break;

    case BCFormat::BC7:
        blockSize = BCDEC_BC7_BLOCK_SIZE;
        decode = bcdec_bc7;
        break;

    default:
        return false;
    }

    for (int by = 0; by < blocksY; ++by)
    {
        for (int bx = 0; bx < blocksX; ++bx, src += blockSize)
        {
            const int x = bx * 4;
            const int y = by * 4;
            const bool fullBlock = x + 4 <= width && y + 4 <= height;

            uint8_t temp[64];
            uint8_t* out = fullBlock ? dst + (y * width + x) * 4 : temp;
            const int pitch = fullBlock ? width * 4 : 16;

            decode(src, out, pitch);

            if (!fullBlock)
            {
                const int copyWidth = std::min(4, width - x);
                const int copyHeight = std::min(4, height - y);

                for (int row = 0; row < copyHeight; ++row)
                    memcpy(dst + ((y + row) * width + x) * 4, temp + row * 16, copyWidth * 4);
            }
        }
    }

    return true;
}