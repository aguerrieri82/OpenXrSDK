using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Geometry3D : EngineObject, IHosted
    {
        protected bool _boundsDirty;
        protected Bounds3 _bounds;
        protected HashSet<EngineObject> _hosts = [];
        protected VertexData[] _vertices;
        protected uint[] _indices;

        public Geometry3D()
        {
            _boundsDirty = true;
            ActiveComponents = VertexComponent.Position;
            _indices = [];
            _vertices = [];

        }

        public void Attach(EngineObject host)
        {
            _hosts.Add(host);
        }

        public void Detach(EngineObject host)
        {
            _hosts.Remove(host);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            if (this is IGeneratedContent gen)
            {
                container.ReadObject(this, GetType());
                gen.Build();
            }
            else
            {
                Indices = container.ReadBuffer<uint>(nameof(Indices));
                Vertices = container.ReadBuffer<VertexData>(nameof(Vertices));
                ActiveComponents = container.Read<VertexComponent>(nameof(ActiveComponents));
            }
        }

        public unsafe override void GetState(IStateContainer container)
        {
            base.GetState(container);
            if (this is IGeneratedContent gen)
                container.WriteObject(this, GetType());
            else
            {
                container.WriteBuffer(nameof(Indices), Indices);
                container.WriteBuffer(nameof(Vertices), Vertices);
                container.Write(nameof(ActiveComponents), ActiveComponents);
            }
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

        public void FreeBuffers()
        {
            UpdateBounds();
            Indices = [];
            Vertices = [];
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

        public unsafe void Serialize(Stream stream)
        {
            using var writer = new BinaryWriter(stream);

            writer.Write("GEOM");
            writer.Write((int)ActiveComponents);
            writer.Write(Vertices.Length);
            
            fixed (VertexData* pVertex = &Vertices[0])
                writer.Write(new Span<byte>(pVertex, Vertices.Length * sizeof(VertexData)));
            
            writer.Write(Indices.Length);

            if (Indices.Length > 0)
            {
                fixed (uint* pIndex = &Indices[0])
                    writer.Write(new Span<byte>(pIndex, Vertices.Length * sizeof(uint)));
            }
            writer.Flush();
        }

        public void ScaleUV(Vector2 scale)
        {
            for (int i = 0; i < _vertices.Length; i++)
                _vertices[i].UV *= scale;
            Version++;
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

        public IReadOnlySet<EngineObject> Hosts => _hosts;

        public VertexComponent ActiveComponents { get; set; }

        public uint[] Indices
        {
            get => _indices;
            set => _indices = value;
        }

        public VertexData[] Vertices
        {
            get => _vertices;
            set => _vertices = value;
        }
    }
}
