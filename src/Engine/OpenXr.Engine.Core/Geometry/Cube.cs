using System.Numerics;

namespace OpenXr.Engine
{
    public class Cube : Geometry
    {
        public Cube()
        {
            Vertices = VertexData.FromPosNormalUV(
            [
                  1.000000f, 1.000000f, -1.000000f,
                  1.000000f, -1.000000f, -1.000000f,
                  1.000000f, 1.000000f, 1.000000f,
                  1.000000f, -1.000000f, 1.000000f,
                  -1.000000f, 1.000000f, -1.000000f,
                  -1.000000f, -1.000000f, -1.000000f,
                  -1.000000f, 1.000000f, 1.000000f,
                  -1.000000f, -1.000000f, 1.000000f,

            ]);

            Indices = [0,2,3,
                    0,3,1,
                    8,4,5,
                    8,5,9,
                    10,6,7,
                    10,7,11,
                    12,13,14,
                    12,14,15,
                    16,17,18,
                    16,18,19,
                    20,21,22,
                    20,22,23
            ];

            TriangleCount = 9;
        }

        public static readonly Cube Instance = new Cube();
    }
}
