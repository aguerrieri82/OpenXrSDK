using System.Numerics;
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

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            throw new NotImplementedException();
            base.SetStateWork(ctx, container);
        }

        protected override void GetState(StateContext ctx, IStateContainer container)
        {
            container.Write(nameof(Indices), Indices);
            
            var vert = container.Enter(nameof(Vertices));   

            foreach (var component in Enum.GetValues< VertexComponent>())
            {
                if ((ActiveComponents & component) != component)
                    continue;

                if (component == VertexComponent.Position)
                    vert.Write("Position", Vertices.SelectMany(a => new float[] { a.Pos.X, a.Pos.Y, a.Pos.Z }));

                else if (component == VertexComponent.Normal)
                    vert.Write("Normal", Vertices.SelectMany(a => new float[] { a.Normal.X, a.Normal.Y, a.Normal.Z }));

                else if (component == VertexComponent.Tangent)
                    vert.Write("Tangent", Vertices.SelectMany(a => new float[] { a.Tangent.X, a.Tangent.Y, a.Tangent.Z, a.Tangent.W}));

                else if (component == VertexComponent.UV0)
                    vert.Write("UV0", Vertices.SelectMany(a => new float[] { a.UV.X, a.UV.Y }));

                else
                    throw new NotImplementedException();
            }

            base.GetState(ctx, container);
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
