using System.Numerics;

namespace OpenXr.Engine
{
    public class Geometry3D : EngineObject
    {
        protected bool _boundsDirty;
        protected Bounds3 _bounds;

        public Geometry3D()
        {
            _boundsDirty = true;
        }

        public void ApplyTransform(Matrix4x4 matrix)
        {
            if (Vertices == null)
                return;

            for (int i = 0; i < Vertices.Length; i++)
                Vertices[i].Pos = Vertices[i].Pos.Transform(matrix);

            Version++;
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

        public uint[]? Indices { get; set; }

        public VertexData[]? Vertices { get; set; }
    }
}
