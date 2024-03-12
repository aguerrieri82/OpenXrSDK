using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Quad3D : Geometry3D
    {
        public Quad3D()
            : this(new Size2(1, 1))
        {
        }

        public Quad3D(Size2 size)
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
                0,1,2,
                0,2,3,
            ];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;
        }

        public Size2 Size { get; set; }

        public static readonly Quad3D Instance = new Quad3D();
    }
}
