﻿using System.Numerics;

namespace OpenXr.Engine
{

    public abstract class Geometry : EngineObject
    {
        private int _version;

        public void ApplyTransform(Matrix4x4 matrix)
        {
            if (Vertices == null)
                return;

            for (int i = 0; i < Vertices.Length; i++)
                Vertices[i].Pos = Vector3.Transform(Vertices[i].Pos, matrix);

            _version++;
        }

        public void Rebuild()
        {
            if (Indices == null)
                return;
            var vertices = new VertexData[Indices!.Length];

            for (var i = 0; i < Indices!.Length; i++)
                vertices[i] = Vertices![Indices[i]];

            Vertices = vertices;
            Indices = [];
            _version++;
        }



        public long Version => _version;

        public uint[]? Indices { get; set; }

        public VertexData[]? Vertices { get; set; }
    }
}
