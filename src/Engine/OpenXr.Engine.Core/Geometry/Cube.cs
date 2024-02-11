﻿namespace OpenXr.Engine
{
    public class Cube : Geometry
    {
        public Cube()
        {
            Vertices = VertexData.FromPosNormalUV(
            [
               //X    Y      Z       Normals
            1f, 1f, -1f, -0f, 1f, -0f, 1f, 1f,
            -1f, 1f, -1f, -0f, 1f, -0f, 0f, 1f,
            -1f, 1f, 1f, -0f, 1f, -0f, 0f, 0f,
            1f, 1f, 1f, -0f, 1f, -0f, 1f, 0f,
            1f, -1f, 1f, -0f, -0f, 1f, 1f, 0f,
            1f, 1f, 1f, -0f, -0f, 1f, 1f, 1f,
            -1f, 1f, 1f, -0f, -0f, 1f, 0f, 1f,
            -1f, -1f, 1f, -0f, -0f, 1f, 0f, 0f,
            -1f, -1f, 1f, -1f, -0f, -0f, 0f, 0f,
            -1f, 1f, 1f, -1f, -0f, -0f, 0f, 1f,
            -1f, 1f, -1f, -1f, -0f, -0f, 1f, 1f,
            -1f, -1f, -1f, -1f, -0f, -0f, 1f, 0f,
            -1f, -1f, -1f, -0f, -1f, -0f, 0f, 1f,
            1f, -1f, -1f, -0f, -1f, -0f, 1f, 1f,
            1f, -1f, 1f, -0f, -1f, -0f, 1f, 0f,
            -1f, -1f, 1f, -0f, -1f, -0f, 0f, 0f,
            1f, -1f, -1f, 1f, -0f, -0f, 1f, 0f,
            1f, 1f, -1f, 1f, -0f, -0f, 1f, 1f,
            1f, 1f, 1f, 1f, -0f, -0f, 0f, 1f,
            1f, -1f, 1f, 1f, -0f, -0f, 0f, 0f,
            -1f, -1f, -1f, -0f, -0f, -1f, 0f, 0f,
            -1f, 1f, -1f, -0f, -0f, -1f, 0f, 1f,
            1f, 1f, -1f, -0f, -0f, -1f, 1f, 1f,
            1f, -1f, -1f, -0f, -0f, -1f, 1f, 0f,
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

            //Rebuild();
        }


        public static readonly Cube Instance = new Cube();
    }
}
