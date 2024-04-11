using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Quad3D : Geometry3D, IGeneratedContent
    {
        public Quad3D()
        {
        }

        public Quad3D(Size2 size)
        {
            Size = size;
            Build();
        }

        public override void GetState(IStateContainer container)
        {
            container.Write(nameof(Size), Size);
            container.Write(nameof(Id), Id);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            Size = container.Read<Size2>(nameof(Size));
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

        public Size2 Size { get; set; }

        public static readonly Quad3D Instance = new(new Size2(1, 1));
    }
}
