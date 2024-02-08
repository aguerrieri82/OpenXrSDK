using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Cube : Geometry
    {
        public Cube()
        {
           Vertices =
           [
                // Front face
                new(-0.5f, -0.5f, 0.5f),
                new(0.5f, -0.5f, 0.5f),
                new(0.5f, 0.5f, 0.5f),
                new(-0.5f, 0.5f, 0.5f),

                // Back face
                new(0.5f, -0.5f, -0.5f),
                new(-0.5f, -0.5f, -0.5f),
                new(-0.5f, 0.5f, -0.5f),
                new(0.5f, 0.5f, -0.5f),

                // Top face
                new(-0.5f, 0.5f, 0.5f),
                new(0.5f, 0.5f, 0.5f),
                new(0.5f, 0.5f, -0.5f),
                new(-0.5f, 0.5f, -0.5f),

                // Bottom face
                new(-0.5f, -0.5f, -0.5f),
                new(0.5f, -0.5f, -0.5f),
                new(0.5f, -0.5f, 0.5f),
                new(-0.5f, -0.5f, 0.5f),

                // Left face
                new(-0.5f, -0.5f, -0.5f),
                new(-0.5f, -0.5f, 0.5f),
                new(-0.5f, 0.5f, 0.5f),
                new(-0.5f, 0.5f, -0.5f),

                // Right face
                new(0.5f, -0.5f, 0.5f),
                new(0.5f, -0.5f, -0.5f),
                new(0.5f, 0.5f, -0.5f),
                new(0.5f, 0.5f, 0.5f)
           ];

            Triangles =
            [
                // Front face
                0, 1, 2,
                2, 3, 0,

                // Back face
                4, 5, 6,
                6, 7, 4,

                // Top face
                8, 9, 10,
                10, 11, 8,

                // Bottom face
                12, 13, 14,
                14, 15, 12,

                // Left face
                16, 17, 18,
                18, 19, 16,

                // Right face
                20, 21, 22,
                22, 23, 20
            ];
        }

        public static readonly Cube Instance = new Cube();
    }
}
