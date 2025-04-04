﻿using System.Numerics;

namespace XrEngine
{
    public class QuadPatch3D : Geometry3D, IGeneratedContent
    {
        public QuadPatch3D()
            : this(Vector2.One)
        {
        }

        public QuadPatch3D(Vector2 size, int patches = 2)

        {
            Flags |= EngineObjectFlags.Readonly;
            Size = size;
            Patches = patches;
            Build();
        }

        public void Build()
        {
            var vertices = new List<VertexData>();

            void AddVertex(float x, float y)
            {
                vertices.Add(new VertexData
                {
                    Pos = new Vector3((x - 0.5f) * Size.X, (y - 0.5f) * Size.Y, 0),
                    Normal = new Vector3(0, 0, 1),
                    UV = new Vector2(x, (1 - y)),
                    Tangent = new Vector4(1, 0, 0, 1)
                });
            }

            var step = 1f / Patches;
            for (var x = 0; x < Patches; x++)
            {
                var x1 = step * x;
                var x2 = x1 + step;
                for (var y = 0; y < Patches; y++)
                {
                    var y1 = step * y;
                    var y2 = y1 + step;
                    AddVertex(x1, y1);
                    AddVertex(x2, y1);
                    AddVertex(x2, y2);
                    AddVertex(x1, y2);
                }
            }

            Vertices = vertices.ToArray();

            this.EnsureIndices();

            Primitive = DrawPrimitive.Quad;

            ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0 | VertexComponent.Tangent;
        }

        public Vector2 Size { get; set; }

        public int Patches { get; set; }
    }
}
