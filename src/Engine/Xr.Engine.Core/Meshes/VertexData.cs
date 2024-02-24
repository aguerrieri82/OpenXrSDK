using System.Numerics;

namespace Xr.Engine
{
    public struct VertexData
    {
        public static VertexData[] FromPosNormal(float[] data)
        {
            var result = new VertexData[data.Length / 6];
            var di = 0;
            for (int i = 0; i < result.Length; i++)
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
            for (int i = 0; i < result.Length; i++)
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

        [ShaderRef(0, "a_position", VertexComponent.Position)]
        public Vector3 Pos;

        [ShaderRef(1, "a_normal", VertexComponent.Normal)]
        public Vector3 Normal;

        [ShaderRef(2, "a_texcoord_0", VertexComponent.UV0)]
        public Vector2 UV;

        [ShaderRef(3, "a_tangent", VertexComponent.Tangent)]
        public Vector4 Tangent;
    }
}
