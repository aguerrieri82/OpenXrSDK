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
            Primitive = DrawPrimitive.Triangle;
            _indices = [];
            _vertices = [];

        }

        public override void GeneratePath(List<string> parts)
        {
            if (_hosts.Count > 0)
                _hosts.First().GeneratePath(parts);
            parts.Add("Geometry");
            base.GeneratePath(parts);
        }

        public void Attach(EngineObject host)
        {
            _hosts.Add(host);
        }

        public void Detach(EngineObject host)
        {
            Detach(host, false);
        }

        public void Detach(EngineObject host, bool dispose)
        {
            _hosts.Remove(host);
            if (dispose && _hosts.Count == 0)
                Dispose();
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

        public override void GetState(IStateContainer container)
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

        public virtual void UpdateBounds()
        {
            _bounds = this.ComputeBounds(Matrix4x4.Identity);
            _boundsDirty = false;

            foreach (var host in _hosts.OfType<TriangleMesh>())
                host.InvalidateLocalBounds();

            if (_components != null)
            {
                foreach (var item in _components.OfType<IGeometryComponent>())
                    item.UpdateBounds();
            }
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

        protected override void OnChanged(ObjectChange change)
        {
            if (change.IsAny(ChangeType.Geometry))
                _boundsDirty = true;

            base.OnChanged(change);
        }

        public virtual void NotifyLoaded()
        {
            if (!this.Is(EngineObjectFlags.GpuOnly))
                return;

            if (_boundsDirty)
                UpdateBounds();

            _indices = [];
            _vertices = [];

            if (_components != null)
            {
                foreach (var item in _components.OfType<IGeometryComponent>())
                    item.NotifyLoaded();
            }
        }

        public override Geometry3D Clone(ObjectCloneFlags flags = ObjectCloneFlags.None)
        {
            return (Geometry3D)base.Clone(flags);
        }

        protected override void CloneWork(EngineObject newObj, ObjectCloneFlags flags)
        {
            var geo = (Geometry3D)newObj;

            geo.Vertices = new VertexData[_vertices.Length];
            Array.Copy(_vertices, geo.Vertices, _vertices.Length);

            geo.Indices = new uint[_indices.Length];
            Array.Copy(_indices, geo.Indices, _indices.Length);

            geo.ActiveComponents = ActiveComponents;
            geo._bounds = _bounds;
            geo._boundsDirty = _boundsDirty;

            base.CloneWork(newObj, flags);
        }

        public void InvalidateBounds()
        {
            _boundsDirty = true;
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

        public DrawPrimitive Primitive { get; set; }
    }
}
