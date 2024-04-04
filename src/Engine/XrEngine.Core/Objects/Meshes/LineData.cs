using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct LineData
    {
        [ShaderRef(0, "a_position", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(1, "a_color_0", VertexComponent.Color4)]
        public Color Color;

        [ShaderRef(2, "a_size", VertexComponent.Generic)]
        public float Size;
    }
}
