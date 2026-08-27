using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CompVertexData 
    {

        [ShaderRef(AttributeSlots.Position, "aPosition", VertexComponent.Position, IsNormalized = true)]
        public Vector3<ushort> Pos;

        [ShaderRef(AttributeSlots.Normal, "aNormal", VertexComponent.Normal, IsNormalized = true)]
        public Vector3<short> Normal;

        [ShaderRef(AttributeSlots.UV0, "aUv0", VertexComponent.UV0)]
        public Vector2<Half> UV;

        [ShaderRef(AttributeSlots.UV1, "aUv1", VertexComponent.UV1)]
        public Vector2<Half> UV1;

        [ShaderRef(AttributeSlots.Tangent, "aTangent", VertexComponent.Tangent, IsNormalized = true)]
        public Vector4<short> Tangent;

    }
}
