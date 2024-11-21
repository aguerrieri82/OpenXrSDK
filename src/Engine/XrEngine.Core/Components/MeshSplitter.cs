using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.Components
{
    public class MeshSplitter : Behavior<TriangleMesh>, IDrawGizmos
    {
        readonly List<Triangle3> _splitTriangles = [];
        readonly List<Triangle3> _originalTriangles = [];
        Bounds3 _splitBounds;
        Matrix4x4 _boundsTransform;
        TriangleMesh? _splittedMesh;
        Geometry3D? _startGeo;


        public MeshSplitter()
        {
        }

        public MeshSplitter(TriangleMesh host)
        {
            Attach(host);
        }

        public void Attach(TriangleMesh host)
        {
            _host = host;
            OnAttach();
        }

        protected override void OnAttach()
        {
            _splittedMesh = null;
            _startGeo = _host!.Geometry!.Clone();
            base.OnAttach();
        }

        protected override void Update(RenderContext ctx)
        {
            ComputeSplit();
        }

        public TriangleMesh ExecuteSplit()
        {
            Debug.Assert(_startGeo != null && _host != null);

            ComputeSplit();

            if (_splittedMesh == null)
            {
                _splittedMesh = new TriangleMesh();
                _splittedMesh.Name = SplittedName;

                foreach (var material in _host.Materials)
                    _splittedMesh.Materials.Add(material);

                _splittedMesh.Geometry = new Geometry3D()
                {
                    ActiveComponents = _startGeo!.ActiveComponents
                };

                _host.Parent!.AddChild(_splittedMesh);
                _splittedMesh.Transform.Set(_host.Transform);
            }

            _host.Geometry!.Vertices = _startGeo.Vertices;
            _host.Geometry!.Rebuild(_originalTriangles);

            _splittedMesh.Geometry!.Vertices = _startGeo.Vertices;
            _splittedMesh.Geometry!.Rebuild(_splitTriangles);

            return _splittedMesh;
        }

        protected void ComputeSplit()
        {
            Debug.Assert(_startGeo != null);

            _startGeo.EnsureIndices();

            _splitTriangles.Clear();
            _originalTriangles.Clear();

            _splitBounds = new Bounds3
            {
                Min = -Bounds / 2,
                Max = Bounds / 2
            };

            _splitBounds.Min.Z = 0;
            _splitBounds.Max.Z = Bounds.Z;

            _boundsTransform = new Pose3
            {
                Orientation = Orientation,
                Position = Origin
            }.ToMatrix();

            Matrix4x4.Invert(_boundsTransform, out var reverse);

            foreach (var triangle in _startGeo.Triangles())
            {
                var newTr = triangle.Transform(reverse);

                var isValid = FullIntersection ?
                    newTr.Vertices.All(a => _splitBounds.Contains(a)) :
                    newTr.Vertices.Any(a => _splitBounds.Contains(a));

                if (isValid)
                    _splitTriangles.Add(triangle);
                else
                    _originalTriangles.Add(triangle);
            }
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            if (!ShowGizmos)
                return;

            canvas.Save();

            Matrix4x4.Invert(_boundsTransform, out var reverse);

            canvas.State.Color = "#00A000";
            canvas.State.Transform = _host!.WorldMatrix;

            foreach (var triangle in _splitTriangles)
                canvas.DrawTriangle(triangle);

            canvas.State.Color = "#00ff00";
            canvas.State.Transform = _boundsTransform * _host!.WorldMatrix;
            canvas.DrawBounds(_splitBounds);

            canvas.Restore();
        }



        public bool ShowGizmos { get; set; }

        public string? SplittedName { get; set; }

        public Vector3 Origin { get; set; }

        public Quaternion Orientation { get; set; }

        public Vector3 Bounds { get; set; }

        public bool FullIntersection { get; set; }

    }
}
