using System.Numerics;

namespace XrEngine
{
    public class Cube3D : Geometry3D, IGeneratedContent
    {
        public Cube3D()
            : this(new Vector3(1f, 1f, 1f))
        {
        }

        public Cube3D(Vector3 size)
        {
            Size = size;
            Build();
        }

        public void Build()
        {
            Vertices = VertexData.FromPosNormalUV(
            [
               //X    Y      Z       Normals
                Size.X, Size.Y, -Size.Z, -0f, 1f, -0f, 1f, 1f,
                -Size.X, Size.Y, -Size.Z, -0f, 1f, -0f, 0f, 1f,
                -Size.X, Size.Y, Size.Z, -0f, 1f, -0f, 0f, 0f,
                Size.X, Size.Y, Size.Z, -0f, 1f, -0f, 1f, 0f,
                Size.X, -Size.Y, Size.Z, -0f, -0f, 1f, 1f, 0f,
                Size.X, Size.Y, Size.Z, -0f, -0f, 1f, 1f, 1f,
                -Size.X, Size.Y, Size.Z, -0f, -0f, 1f, 0f, 1f,
                -Size.X, -Size.Y, Size.Z, -0f, -0f, 1f, 0f, 0f,
                -Size.X, -Size.Y, Size.Z, -1f, -0f, -0f, 0f, 0f,
                -Size.X, Size.Y, Size.Z, -1f, -0f, -0f, 0f, 1f,
                -Size.X, Size.Y, -Size.Z, -1f, -0f, -0f, 1f, 1f,
                -Size.X, -Size.Y, -Size.Z, -1f, -0f, -0f, 1f, 0f,
                -Size.X, -Size.Y, -Size.Z, -0f, -1f, -0f, 0f, 1f,
                Size.X, -Size.Y, -Size.Z, -0f, -1f, -0f, 1f, 1f,
                Size.X, -Size.Y, Size.Z, -0f, -1f, -0f, 1f, 0f,
                -Size.X, -Size.Y, Size.Z, -0f, -1f, -0f, 0f, 0f,
                Size.X, -Size.Y, -Size.Z, 1f, -0f, -0f, 1f, 0f,
                Size.X, Size.Y, -Size.Z, 1f, -0f, -0f, 1f, 1f,
                Size.X, Size.Y, Size.Z, 1f, -0f, -0f, 0f, 1f,
                Size.X, -Size.Y, Size.Z, 1f, -0f, -0f, 0f, 0f,
                -Size.X, -Size.Y, -Size.Z, -0f, -0f, -1f, 0f, 0f,
                -Size.X, Size.Y, -Size.Z, -0f, -0f, -1f, 0f, 1f,
                Size.X, Size.Y, -Size.Z, -0f, -0f, -1f, 1f, 1f,
                Size.X, -Size.Y, -Size.Z, -0f, -0f, -1f, 1f, 0f,
             ]);

            Indices =
            [
                 0,1,2,
                 0,2,3,
                 4,5,6,
                 4,6,7,
                 8,9,10,
                 8,10,11,
                 12,13,14,
                 12,14,15,
                 16,17,18,
                 16,18,19,
                 20,21,22,
                 20,22,23,
             ];

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0;
        }

        public override void GetState(IStateContainer container)
        {
            container.Write(nameof(Size), Size);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            Size = container.Read<Vector3>(nameof(Size));
            Build();
        }

        public Vector3 Size { get; set; }


        public static readonly Cube3D Instance = new();
    }
}
