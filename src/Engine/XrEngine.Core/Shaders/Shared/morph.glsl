struct MorphTarget
{
    float weight;
    uint positionOfs;
    uint normalOfs;
    uint tangentOfs;
};

layout(std140, binding = 6) uniform MorphUniformBuffer
{
    MorphTarget uMorphTargets[MAX_MORPH_TARGETS];
};

#if defined(USE_MORPH_SSBO)

    layout(std430, binding = 20) readonly buffer MorphDataBuffer
    {
        float uMorphData[];
    };

    void morphInit()
    {
    }

    vec3 morphFetch(uint ofs)
    {
        uint index = (ofs + uint(gl_VertexID)) * 3u;

        return vec3(
            uMorphData[index],
            uMorphData[index + 1u],
            uMorphData[index + 2u]
        );
    }

#elif defined(USE_MORPH_TEXTURE)

    layout(binding = 9) uniform sampler2D uMorphTexture;

    ivec2 morphCoord;

    void morphInit()
    {
        int width = textureSize(uMorphTexture, 0).x;

        morphCoord.x = gl_VertexID % width;
        morphCoord.y = gl_VertexID / width;
    }

    vec3 morphFetch(uint baseRow)
    {
        return texelFetch(
            uMorphTexture,
            ivec2(morphCoord.x, int(baseRow) + morphCoord.y),
            0
        ).xyz;
    }

#endif

APPLY_MORPH