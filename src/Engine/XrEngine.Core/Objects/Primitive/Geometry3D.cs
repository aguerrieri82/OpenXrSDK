using SkiaSharp;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using XrEngine.Objects;
using XrMath;

namespace XrEngine
{

    public abstract class Geometry3D : EngineObject, IHosted, IGeometryVertices
    {
        protected bool _boundsDirty;
        protected Bounds3 _bounds;
        protected HashSet<EngineObject> _hosts = [];
        protected uint[] _indices;

        public Geometry3D()
        {
            _boundsDirty = true;
            ActiveComponents = VertexComponent.Position;
            Primitive = DrawPrimitive.Triangle;
            _indices = [];

        }

        public Geometry3D<TRes> As<TRes>()
            where TRes : unmanaged, IVertexProvider
        {
            return (Geometry3D<TRes>)(object)this;
        }


        public Geometry3D Clone()
        {
            var result = Utils.CreateInstance<Geometry3D>(GetType());

            result.Indices = new uint[_indices.Length];
            Array.Copy(_indices, result.Indices, _indices.Length);
            result.ActiveComponents = ActiveComponents;
            result._bounds = _bounds;
            result._boundsDirty = _boundsDirty;

            CloneWork(result);

            return result;
        }

        protected virtual void CloneWork(Geometry3D result)
        {
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
                _indices = container.ReadBuffer<uint>(nameof(Indices));
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

                container.Write(nameof(ActiveComponents), ActiveComponents);
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
        }

        public abstract IVerticesList NewVertices(int capacity = 0);

        public abstract void Rebuild();

        public abstract void UpdateBounds();

        public abstract TVert[] GetVertices<TVert>() 
            where TVert : unmanaged, IVertexProvider;

        public abstract void ScaleUV(Vector2 scale);
        
        public abstract void Serialize(Stream stream);

        public abstract void ApplyTransform(Matrix4x4 matrix);

        public abstract void SetVertices(IVerticesList vertices);

        public abstract void SetVertices(IVerticesArray vertices);

        public IReadOnlySet<EngineObject> Hosts => _hosts;

        public VertexComponent ActiveComponents { get; set; }

        public DrawPrimitive Primitive { get; set; }

        public abstract IVerticesArray Vertices { get; }

        public uint[] Indices
        {
            get => _indices;
            set => _indices = value;
        }
    }

    public class Geometry3D<TVert> : Geometry3D, IGeometryVertices<TVert>
        where TVert : unmanaged, IVertexProvider
    {
        readonly struct VerticesArrayImpl : IVerticesArray
        {
            readonly internal TVert[] _vertices;

            public VerticesArrayImpl(TVert[] vertices)
            {
                _vertices = vertices;
            }


            public IEnumerator<VertexData> GetEnumerator()
            {
                foreach (var item in _vertices)
                    yield return item.Vertex;
            }

            public readonly Array ToArray()
            {
                return _vertices;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public readonly ref VertexData this[int index] => ref _vertices[index].Vertex;

            public readonly int Length => _vertices.Length;
        }

        readonly struct VerticesList : IVerticesList
        {
            internal readonly List<TVert> _vertices;

            public VerticesList(int capacity = 0)
            {
                _vertices = new List<TVert>(capacity);
            }

            public VertexData this[int index]
            {
                get => _vertices[index].Vertex;
                set
                {
                    var item = _vertices[index];
                    item.Vertex = value;
                    _vertices[index] = item;
                }
            }

            public int Count => _vertices.Count;

            public bool IsReadOnly => false;

            public void Add(VertexData item)
            {
                var vertex = default(TVert);
                vertex.Vertex = item;
                _vertices.Add(vertex);
            }

            public void Clear()
            {
                _vertices.Clear();
            }

            public bool Contains(VertexData item)
            {
                return IndexOf(item) != -1;
            }

            public void CopyTo(VertexData[] array, int arrayIndex)
            {
                for (var i = 0; i < _vertices.Count; i++)
                    array[arrayIndex + i] = _vertices[i].Vertex;
            }

            public IEnumerator<VertexData> GetEnumerator()
            {
                foreach (var item in _vertices)
                    yield return item.Vertex;
            }

            public int IndexOf(VertexData item)
            {
                var comparer = EqualityComparer<VertexData>.Default;

                for (var i = 0; i < _vertices.Count; i++)
                    if (comparer.Equals(_vertices[i].Vertex, item))
                        return i;

                return -1;
            }

            public void Insert(int index, VertexData item)
            {
                var vertex = default(TVert);
                vertex.Vertex = item;
                _vertices.Insert(index, vertex);
            }

            public bool Remove(VertexData item)
            {
                var index = IndexOf(item);
                if (index < 0)
                    return false;

                _vertices.RemoveAt(index);
                return true;
            }

            public void RemoveAt(int index)
            {
                _vertices.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        protected TVert[] _vertices;

        public Geometry3D()
        {
            _vertices = [];
        }

        public override void ApplyTransform(Matrix4x4 matrix)
        {
            var inverse = matrix.Invert();

            var normalMatrix = Matrix4x4.Transpose(inverse);

            for (var i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Vertex.Pos = _vertices[i].Vertex.Pos.Transform(matrix);
                _vertices[i].Vertex.Normal = _vertices[i].Vertex.Normal.Transform(normalMatrix).Normalize();
            }

            NotifyChanged(ChangeType.Geometry);
        }

        public unsafe override void Serialize(Stream stream)
        {
            using var writer = new BinaryWriter(stream);

            writer.Write("GEOM");
            writer.Write((int)ActiveComponents);
            writer.Write(_vertices.Length);

            fixed (VertexData* pVertex = &_vertices[0].Vertex)
                writer.Write(new Span<byte>(pVertex, _vertices.Length * sizeof(VertexData)));

            writer.Write(_indices.Length);

            if (_indices.Length > 0)
            {
                fixed (uint* pIndex = &_indices[0])
                    writer.Write(new Span<byte>(pIndex, _vertices.Length * sizeof(uint)));
            }
            writer.Flush();
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);

            if (this is not IGeneratedContent)
                container.WriteBuffer(nameof(Vertices), _vertices);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);

            if (this is not IGeneratedContent)
                _vertices = container.ReadBuffer<TVert>(nameof(Vertices));
        }

        public override void NotifyLoaded()
        {
            base.NotifyLoaded();
            _vertices = [];
        }


        public override void ScaleUV(Vector2 scale)
        {
            for (var i = 0; i < _vertices.Length; i++)
                _vertices[i].Vertex.UV *= scale;

            NotifyChanged(ChangeType.Geometry);
        }

        public override void Rebuild()
        {
            if (_indices.Length == 0)
                return;

            var vertices = new TVert[_indices.Length];

            for (var i = 0; i < _indices.Length; i++)
                vertices[i] = _vertices[Indices[i]];

            _vertices = vertices;
            _indices = [];

            NotifyChanged(ChangeType.Geometry);
        }

        protected override void CloneWork(Geometry3D result)
        {
            var typedRes = (Geometry3D<TVert>)(result);
            typedRes._vertices = new TVert[_vertices.Length];

            Array.Copy(_vertices, typedRes._vertices, _vertices.Length);
        }

        public override void UpdateBounds()
        {
            _bounds = this.ComputeBounds(Matrix4x4.Identity);
            _boundsDirty = false;
        }

        public override TGet[] GetVertices<TGet>()
        {
            if (typeof(TGet) != typeof(TVert))
                throw new NotSupportedException();

            return (TGet[])(object)_vertices;
        }

        public override IVerticesArray Vertices
        {
            get => new VerticesArrayImpl(_vertices);
        }

        public override void SetVertices(IVerticesArray vertices)
        {
            if (vertices is VerticesArrayImpl va)
                _vertices = va._vertices;
            throw new NotSupportedException();
        }

        public override IVerticesList NewVertices(int capacity = 0)
        {
            return new VerticesList(capacity);
        }

        public override void SetVertices(IVerticesList vertices)
        {
            if (vertices is not VerticesList vl)
                throw new NotSupportedException();

            _vertices = vl._vertices.ToArray();
        }

        public TVert[] VerticesArray
        {
            get => _vertices;
            set => _vertices = value;
        }

        TVert[] IGeometryVertices<TVert>.Vertices
        {
            get => _vertices;
            set => _vertices = value;
        }
    }

    public class SimpleGeometry3D : Geometry3D<VertexData>
    {
    }

    public class SkinnedGeometry3D : Geometry3D<SkinnedVertexData>
    {
    }
}
