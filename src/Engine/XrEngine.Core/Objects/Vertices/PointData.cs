using System.Numerics;
using XrMath;

namespace XrEngine
{
    public struct PointData
    {
        [ShaderRef(0, "aPosition", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(1, "aColor", VertexComponent.Color4)]
        public Color Color;

        [ShaderRef(2, "aSize", VertexComponent.Size)]
        public float Size;
    }
}
