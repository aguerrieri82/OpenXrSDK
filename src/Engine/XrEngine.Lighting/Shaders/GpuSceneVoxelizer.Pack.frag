#version 310 es
precision highp float;

uniform sampler2DArray uSource;
uniform vec2 uTileSize;
uniform float uAtlasColumns;
uniform float uLayerCount;

layout(location = 0) out vec4 outValue;

void main()
{
    ivec2 p = ivec2(gl_FragCoord.xy);
    ivec2 tileSize = ivec2(uTileSize);

    int tileX = p.x / tileSize.x;
    int tileY = p.y / tileSize.y;
    int layer = tileX + tileY * int(uAtlasColumns);

    if (float(layer) >= uLayerCount)
    {
        outValue = vec4(0.0);
        return;
    }

    ivec2 local = ivec2(
        p.x - tileX * tileSize.x,
        p.y - tileY * tileSize.y);

    vec2 uv = (vec2(local) + vec2(0.5)) / uTileSize;

    outValue = texture(uSource, vec3(uv, float(layer)));
}
