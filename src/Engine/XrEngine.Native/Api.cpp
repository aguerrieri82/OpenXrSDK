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

/*
void ParseHDR() {
    
    std::istream& stream;

    std::string line;
    if (!std::getline(stream, line) || line != "#?RADIANCE") {
        throw std::runtime_error("Invalid format");
    }

    std::string format;
    float exposure;
    int width = 0, height = 0;

    while (std::getline(stream, line) && !line.empty()) {
        if (line.compare(0, 7, "FORMAT=") == 0) {
            format = line.substr(7);
        }
        else if (line.compare(0, 9, "EXPOSURE=") == 0) {
            exposure = std::stof(line.substr(9));
        }
    }

    std::getline(stream, line);
    std::istringstream iss(line);
    std::string token;
    while (iss >> token) {
        int value;
        iss >> value;
        if (token == "-Y" || token == "+Y") {
            height = value;
        }
        else if (token == "+X") {
            width = value;
        }
        else {
            throw std::runtime_error("Unsupported format");
        }
    }

    if (format != "32-bit_rle_rgbe") {
        throw std::runtime_error("Unsupported format");
    }

    auto blockImg = std::make_unique<uint8_t[]>(width * height * 4);
    auto scanline = std::make_unique<uint8_t[]>(width * 4);

    for (int j = 0; j < height; ++j) {
        uint8_t rgbe[4];
        stream.read(reinterpret_cast<char*>(rgbe), 4);

        bool isNewRLE = (rgbe[0] == 2 && rgbe[1] == 2 &&
            rgbe[2] == ((width >> 8) & 0xFF) && rgbe[3] == (width & 0xFF));

        if (isNewRLE && width >= 8 && width < 32768) {
            for (int i = 0; i < 4; ++i) {
                int ptr = i * width;
                int ptr_end = (i + 1) * width;
                while (ptr < ptr_end) {
                    uint8_t buf2[2];
                    stream.read(reinterpret_cast<char*>(buf2), 2);
                    if (buf2[0] > 128) {
                        int count = buf2[0] - 128;
                        std::memset(scanline.get() + ptr, buf2[1], count);
                        ptr += count;
                    }
                    else {
                        int count = buf2[0] - 1;
                        scanline[ptr++] = buf2[1];
                        stream.read(reinterpret_cast<char*>(scanline.get() + ptr), count);
                        ptr += count;
                    }
                }
            }
            for (int i = 0; i < width; ++i) {
                for (int k = 0; k < 4; ++k) {
                    blockImg[(j * width + i) * 4 + k] = scanline[i + k * width];
                }
            }
        }
        else {
            std::memcpy(&blockImg[j * width * 4], rgbe, 4);
            stream.read(reinterpret_cast<char*>(&blockImg[j * width * 4 + 4]), (width - 1) * 4);
        }
    }

    auto dst = std::make_unique<float[]>(width * height * 3);

    for (int i = 0, j = 0; i < width * height * 3; i += 3, j += 4) {
        float s = std::pow(2.0f, blockImg[j + 3] - 136);
        dst[i] = blockImg[j] * s;
        dst[i + 1] = blockImg[j + 1] * s;
        dst[i + 2] = blockImg[j + 2] * s;
    }

    auto textureData = std::make_unique<TextureData>();
    textureData->compression = TextureData::CompressionFormat::Uncompressed;
    textureData->data = std::move(dst);
    textureData->dataSize = width * height * 3 * sizeof(float);
    textureData->format = TextureData::Format::RgbFloat32;
    textureData->height = height;
    textureData->width = width;

    return textureData;
}
*/