using System.Numerics;

namespace XrEngine
{
    public class Quad3D : SimpleGeometry3D, IGeneratedContent
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

            _vertices = VertexData.FromPosNormalUV(
            [
               -halfSize.X,  halfSize.Y,  0f, 0f, 0f, 1f,  0f, 0f,
                halfSize.X, halfSize.Y,   0f, 0f, 0f, 1f,  1f, 0f,
                halfSize.X, -halfSize.Y,  0f, 0f, 0f, 1f,  1f, 1f,
                -halfSize.X, -halfSize.Y, 0f, 0f, 0f, 1f,  0f, 1f,
             ]);

            _indices =
            [
                2,1,0,
                3,2,0,
            ];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;

            this.ComputeTangents();
        }

        public Vector2 Size { get; set; }

        public static readonly Quad3D Default = new(Vector2.One);
    }
}
