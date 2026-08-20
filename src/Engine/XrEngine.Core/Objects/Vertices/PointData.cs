using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct PointData
    {
        [ShaderRef(AttributeSlots.Position, "aPosition", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(AttributeSlots.Color, "aColor", VertexComponent.Color4)]
        public Color Color;

        [ShaderRef(AttributeSlots.Size, "aSize", VertexComponent.Size)]
        public float Size;
    }
}
