﻿using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Geometry3D : EngineObject
    {
        protected bool _boundsDirty;
        protected Bounds3 _bounds;

        public Geometry3D()
        {
            _boundsDirty = true;
            ActiveComponents = VertexComponent.Position;
            Indices = [];
            Vertices = [];
        }

        public void ApplyTransform(Matrix4x4 matrix)
        {
            Matrix4x4.Invert(matrix, out var inverse);

            var normalMatrix = Matrix4x4.Transpose(inverse);

            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Pos = Vertices[i].Pos.Transform(matrix);
                Vertices[i].Normal = Vertices[i].Normal.Transform(normalMatrix).Normalize();
            }


            Version++;
        }

        public void Rebuild()
        {
            if (Indices.Length == 0)
                return;

            var vertices = new VertexData[Indices.Length];

            for (var i = 0; i < Indices.Length; i++)
                vertices[i] = Vertices![Indices[i]];

            Vertices = vertices;
            Indices = [];

            Version++;
        }

        public void UpdateBounds()
        {
            _bounds = this.ComputeBounds(Matrix4x4.Identity);
            _boundsDirty = false;
        }

        public Bounds3 Bounds
        {
            get
            {
                if (_boundsDirty)
                    UpdateBounds();
                return _bounds;
            }
        }


        public VertexComponent ActiveComponents { get; set; }

        public uint[] Indices { get; set; }

        public VertexData[] Vertices { get; set; }
    }
}