using System.Numerics;

namespace XrEngine
{
    public class Quad3D : Geometry3D, IGeneratedContent
    {
        public Quad3D()
            : this(Vector2.One)
        {

        }

        public Quad3D(Vector2 size)
        {
            Flags |= EngineObjectFlags.Readonly;
            Size = size;
            Build();
        }

        public void Build()
        {
            var halfSize = new Vector2(Size.X, Size.Y) / 2;

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

            this.ComputeTangents();
        }

        public void FlipYUV()
        {
            for (var i = 0; i < _vertices.Length; i++)
                _vertices[i].UV.Y = _vertices[i].UV.Y == 0 ? 1 : 0;
        }

        public Vector2 Size { get; set; }

        public static readonly Quad3D Default = new(Vector2.One);
    }
}
