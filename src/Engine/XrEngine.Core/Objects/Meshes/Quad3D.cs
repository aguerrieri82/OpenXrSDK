using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Quad3D : Geometry3D, IGeneratedContent
    {
        public Quad3D()
        {
            Flags |= EngineObjectFlags.Readonly;
        }

        public Quad3D(Size2 size)
            : this()
        {
            Size = size;
            Build();
        }

        public void Build()
        {
            var halfSize = new Vector2(Size.Width, Size.Height) / 2;

            Vertices = VertexData.FromPosNormalUV(
            [
               -halfSize.X,  halfSize.Y,  0f, 0f, 0f, 1f,  0f, 0f,
                halfSize.X, halfSize.Y,   0f, 0f, 0f, 1f,  1f, 0f,
                halfSize.X, -halfSize.Y,  0f, 0f, 0f, 1f,  1f, 1f,
                -halfSize.X, -halfSize.Y, 0f, 0f, 0f, 1f,  0f, 1f,
             ]);

            Indices =
            [
                2,1,0,
                3,2,0,
            ];


            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;
        }

        public void FlipYUV()
        {
            for (var i = 0; i < _vertices.Length; i++)
                _vertices[i].UV.Y = _vertices[i].UV.Y == 0 ? 1 : 0;
        }

        public Size2 Size { get; set; }

        public static readonly Quad3D Default = new(new Size2(1, 1));
    }
}
