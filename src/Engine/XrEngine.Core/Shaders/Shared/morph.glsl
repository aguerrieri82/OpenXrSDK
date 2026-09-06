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
        float morphData[];
    };

    void morphInit()
    {
    }

    vec3 morphFetch(uint ofs)
    {
        uint index = (ofs + uint(gl_VertexID)) * 3u;

        return vec3(
            morphData[index],
            morphData[index + 1u],
            morphData[index + 2u]
        );
    }

#elif defined(USE_MORPH_TEXTURE)

    layout(binding = 9) uniform sampler2D morphTexture;

    ivec2 morphCoord;

    void morphInit()
    {
        int width = textureSize(morphTexture, 0).x;

        morphCoord.x = gl_VertexID % width;
        morphCoord.y = gl_VertexID / width;
    }

    vec3 morphFetch(uint baseRow)
    {
        return texelFetch(
            morphTexture,
            ivec2(morphCoord.x, int(baseRow) + morphCoord.y),
            0
        ).xyz;
    }

#endif

#slot APPLY_MORPH