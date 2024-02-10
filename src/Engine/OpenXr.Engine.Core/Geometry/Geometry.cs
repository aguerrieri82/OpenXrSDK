﻿namespace OpenXr.Engine
{

    public abstract class Geometry : EngineObject
    {

        public void Rebuild()
        {
            if (Indices == null)
                return;
            var vertices = new VertexData[Indices!.Length];

            for (var i = 0; i < Indices!.Length; i++)
                vertices[i] = Vertices![Indices[i]];

            Vertices = vertices;
            Indices = [];
        }


        public uint[]? Indices { get; set; }

        public VertexData[]? Vertices { get; set; }
    }
}
