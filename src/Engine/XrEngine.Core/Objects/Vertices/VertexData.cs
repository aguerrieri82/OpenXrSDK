using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexData : IVertexProvider
    {
        public static VertexData[] FromPos(float[] data)
        {
            var result = new VertexData[data.Length / 3];
            var di = 0;
            for (var i = 0; i < result.Length; i++)
            {
                ref var item = ref result[i];

                item.Pos.X = data[di++];
                item.Pos.Y = data[di++];
                item.Pos.Z = data[di++];
            }
            return result;
        }

        public static VertexData[] FromPosNormal(float[] data)
        {
            var result = new VertexData[data.Length / 6];
            var di = 0;
            for (var i = 0; i < result.Length; i++)
            {
                ref var item = ref result[i];

                item.Pos.X = data[di++];
                item.Pos.Y = data[di++];
                item.Pos.Z = data[di++];

                item.Normal.X = data[di++];
                item.Normal.Y = data[di++];
                item.Normal.Z = data[di++];
            }

            return result;
        }

        public static VertexData[] FromPosNormalUV(float[] data)
        {
            var result = new VertexData[data.Length / 8];
            var di = 0;
            for (var i = 0; i < result.Length; i++)
            {
                ref var item = ref result[i];

                item.Pos.X = data[di++];
                item.Pos.Y = data[di++];
                item.Pos.Z = data[di++];

                item.Normal.X = data[di++];
                item.Normal.Y = data[di++];
                item.Normal.Z = data[di++];

                item.UV.X = data[di++];
                item.UV.Y = data[di++];
            }

            return result;
        }

        [ShaderRef(0, "aPosition", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(1, "aNormal", VertexComponent.Normal)]
        public Vector3 Normal;

        [ShaderRef(2, "aUv0", VertexComponent.UV0)]
        public Vector2 UV;

        [ShaderRef(3, "aUv1", VertexComponent.UV1)]
        public Vector2 UV1;

        [ShaderRef(4, "aTangent", VertexComponent.Tangent)]
        public Vector4 Tangent;

        [UnscopedRef]
        ref VertexData IVertexProvider.Vertex => ref this;
    }
}
