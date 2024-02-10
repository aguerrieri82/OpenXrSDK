using System.Numerics;

namespace OpenXr.Engine
{
    public class Cube : Geometry
    {
        public Cube()
        {
            Vertices = VertexData.FromPosNormalUV(
            [
               //X    Y      Z       Normals
               1f, 1f, -1f, -0f, 1f, -0f, 0.625f, 0.5f,
                 -1f, 1f, -1f, -0f, 1f, -0f, 0.875f, 0.5f,
                 -1f, 1f, 1f, -0f, 1f, -0f, 0.875f, 0.75f,
                 1f, 1f, 1f, -0f, 1f, -0f, 0.625f, 0.75f,
                 1f, -1f, 1f, -0f, -0f, 1f, 0.375f, 0.75f,
                 1f, 1f, 1f, -0f, -0f, 1f, 0.625f, 0.75f,
                 -1f, 1f, 1f, -0f, -0f, 1f, 0.625f, 1f,
                 -1f, -1f, 1f, -0f, -0f, 1f, 0.375f, 1f,
                 -1f, -1f, 1f, -1f, -0f, -0f, 0.375f, 0f,
                 -1f, 1f, 1f, -1f, -0f, -0f, 0.625f, 0f,
                 -1f, 1f, -1f, -1f, -0f, -0f, 0.625f, 0.25f,
                 -1f, -1f, -1f, -1f, -0f, -0f, 0.375f, 0.25f,
                 -1f, -1f, -1f, -0f, -1f, -0f, 0.125f, 0.5f,
                 1f, -1f, -1f, -0f, -1f, -0f, 0.375f, 0.5f,
                 1f, -1f, 1f, -0f, -1f, -0f, 0.375f, 0.75f,
                 -1f, -1f, 1f, -0f, -1f, -0f, 0.125f, 0.75f,
                 1f, -1f, -1f, 1f, -0f, -0f, 0.375f, 0.5f,
                 1f, 1f, -1f, 1f, -0f, -0f, 0.625f, 0.5f,
                 1f, 1f, 1f, 1f, -0f, -0f, 0.625f, 0.75f,
                 1f, -1f, 1f, 1f, -0f, -0f, 0.375f, 0.75f,
                 -1f, -1f, -1f, -0f, -0f, -1f, 0.375f, 0.25f,
                 -1f, 1f, -1f, -0f, -0f, -1f, 0.625f, 0.25f,
                 1f, 1f, -1f, -0f, -0f, -1f, 0.625f, 0.5f,
                 1f, -1f, -1f, -0f, -0f, -1f, 0.375f, 0.5f,
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


            VertexCount = 36;

            Rebuild();
        }


        public static readonly Cube Instance = new Cube();
    }
}
