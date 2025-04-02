using System.Numerics;

namespace XrEngine
{
    public class Cube3D : Geometry3D, IGeneratedContent
    {
        public Cube3D()
            : this(Vector3.One)
        {

        }

        public Cube3D(Vector3 size)
        {
            Flags |= EngineObjectFlags.Readonly;
            Size = size;
            Build();
        }

        public void Build()
        {

            var builder = new MeshBuilder();
            builder.AddCube(Center, Size);
            var halfSize = Size / 2;

            Vertices = VertexData.FromPosNormalUV(
            [
               //X    Y      Z       Normals
                halfSize.X, halfSize.Y, -halfSize.Z, -0f, 1f, -0f, 1f, 1f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -0f, 1f, -0f, 0f, 1f,
                -halfSize.X, halfSize.Y, halfSize.Z, -0f, 1f, -0f, 0f, 0f,
                halfSize.X, halfSize.Y, halfSize.Z, -0f, 1f, -0f, 1f, 0f,
                halfSize.X, -halfSize.Y, halfSize.Z, -0f, -0f, 1f, 1f, 0f,
                halfSize.X, halfSize.Y, halfSize.Z, -0f, -0f, 1f, 1f, 1f,
                -halfSize.X, halfSize.Y, halfSize.Z, -0f, -0f, 1f, 0f, 1f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -0f, -0f, 1f, 0f, 0f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -1f, -0f, -0f, 0f, 0f,
                -halfSize.X, halfSize.Y, halfSize.Z, -1f, -0f, -0f, 0f, 1f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -1f, -0f, -0f, 1f, 1f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -1f, -0f, -0f, 1f, 0f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -1f, -0f, 0f, 1f,
                halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -1f, -0f, 1f, 1f,
                halfSize.X, -halfSize.Y, halfSize.Z, -0f, -1f, -0f, 1f, 0f,
                -halfSize.X, -halfSize.Y, halfSize.Z, -0f, -1f, -0f, 0f, 0f,
                halfSize.X, -halfSize.Y, -halfSize.Z, 1f, -0f, -0f, 1f, 0f,
                halfSize.X, halfSize.Y, -halfSize.Z, 1f, -0f, -0f, 1f, 1f,
                halfSize.X, halfSize.Y, halfSize.Z, 1f, -0f, -0f, 0f, 1f,
                halfSize.X, -halfSize.Y, halfSize.Z, 1f, -0f, -0f, 0f, 0f,
                -halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 0f, 0f,
                -halfSize.X, halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 0f, 1f,
                halfSize.X, halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 1f, 1f,
                halfSize.X, -halfSize.Y, -halfSize.Z, -0f, -0f, -1f, 1f, 0f,
             ]);

            if (Center != Vector3.Zero)
            {
                for (var i = 0; i < Vertices.Length; i++)
                    _vertices[i].Pos += Center;
            }

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

            this.ComputeTangents();
        }

        public Vector3 Size { get; set; }

        public Vector3 Center { get; set; }

        public static readonly Cube3D Default = new();
    }
}
