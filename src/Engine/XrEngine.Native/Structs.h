#pragma once

enum VertexComponent : uint32_t
{
    None = 0,
    Generic = 0x1,
    Position = 0x2,
    Normal = 0x4,
    Tangent = 0x8,
    Color3 = 0x10,
    Color4 = 0x20,
    UV0 = 0x40,
    UV1 = 0x80
};


struct VertexData
{
	Vec3 Pos;
	Vec3 Normal;
	Vec2 UV;
	Vec2 UV1;
	Vec4 Tangent;
};


struct CompVertexData
{
	uint16_t Pos[3];
	int16_t Normal[3];
	half  UV[2];
    half  UV1[2];
	int16_t Tangent[4];
};



enum class BCFormat : int32_t
{
    BC1 = 1,
    BC2 = 2,
    BC3 = 3,
    BC4 = 4,
    BC5 = 5,
    BC6H = 6,
    BC7 = 7
};


struct BasisImage
{
    void* Data;
    uint32_t Size;
    uint32_t Width;
    uint32_t Height;
    uint32_t Level;
    uint32_t Layer;
    uint32_t Face;
};

struct BasisTexture
{
    void* Memory;
    BasisImage* Images;
    uint32_t ImageCount;
    uint32_t Width;
    uint32_t Height;
    uint32_t Levels;
    uint32_t Layers;
    uint32_t Faces;
    uint32_t IsSrgb;
    uint32_t HasAlpha;
};