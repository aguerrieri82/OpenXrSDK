using System.Numerics;

namespace OpenXr.Engine
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

        [ShaderRef(0, "vPos")]
        public Vector3 Pos;
        [ShaderRef(1, "vForm")]
        public Vector3 Normal;
        [ShaderRef(2, "vUv")]
        public Vector2 UV;

    }
}
