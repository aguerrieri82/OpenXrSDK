
uniform uint uDrawId;
uniform uint uSize;
uniform uint uFrame;

layout(early_fragment_tests) in;

layout(std430, binding = 12) buffer HitBuffer
{
    uvec4 Hits[];
};

void main()
{
    uint index = uint(gl_FragCoord.y) * uSize + uint(gl_FragCoord.x);

    Hits[index] = uvec4(
        uDrawId,
        uint(gl_PrimitiveID),
        uint(gl_FragCoord.z * 65535.0 + 0.5),
        uFrame
    );
}