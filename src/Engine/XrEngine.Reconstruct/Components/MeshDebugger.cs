
using System.Diagnostics;
using System.Numerics;
using XrEngine.UI;
using XrMath;
using static XrEngine.TriangleMeshSpatialIndex;

namespace XrEngine.Reconstruct
{
    public class MeshDebugger : Behavior<TriangleMesh>, IDisposable
    {
        TriangleMesh? _slice;
        TriangleMeshSpatialIndex? _index;
        CanvasView2D? _canvas;
        bool _showSlice;
        List<Triangle>? _triangles;
        List<Triangle>? _newTriangles;


        static string[] palette = new[]
        {
            "#E63946",
            "#F4A261",
            "#E9C46A",
            "#2A9D8F",
            "#457B9D",
            "#1D3557",
            "#8D99AE",
            "#6D597A",
            "#B56576",
            "#E56B6F",
            "#EAAC8B",
            "#90BE6D",
            "#43AA8B",
            "#4D908E",
            "#577590",
            "#277DA1",
            "#9B5DE5",
            "#F15BB5",
            "#00BBF9",
            "#00F5D4"
        };

        public MeshDebugger()
        {
            CellSize = 0.1f;
            SliceArea = 0.05f;
            TextDistance = 0.01f;
            ShowSubMesh = false;
            ShowTriangles = true;
        }

        protected override void OnAttach()
        {
            Debug.Assert(_host!.Geometry != null);

            _index = new TriangleMeshSpatialIndex(_host.Geometry, CellSize);

            _canvas ??= new CanvasView2D();

            _canvas.DrawCanvas += OnDraw;
        }

        protected override void Start(RenderContext ctx)
        {
            if (_canvas!.Parent == null)
                _host!.Scene!.AddChild(_canvas);

        }

        private void OnDraw(ScreenCanvas ctx)
        {
            if (_newTriangles == null || _triangles == null || !IsEnabled)
                return;

            var hideList = string.IsNullOrEmpty(HideList) ? [] : HideList.Split(',').Select(int.Parse).ToArray();

            if (ShowTriangles)
            {
                int i = 0;
      
                foreach (var tri in _triangles)
                {
                    if (hideList.Contains(tri.TriangleId))
                    {
                        i++;
                        continue;
                    }
           
                    var value = new Triangle3(tri.V0, tri.V1, tri.V2);

                    Color color = !value.IsCCW() ? "#0000ff80" : "#ff000080";

                    color = palette[(i*3) % palette.Length] + "A0";
                    ctx.Draw(value, color, Color.Black, 2f);
                    i++;
                }
  
                foreach (var tri in _newTriangles)
                {
                    var value = new Triangle3(tri.V0, tri.V1, tri.V2);

                    ctx.Draw(value, "#333333E0", Color.Black, 2f);

                }
            }

            if (ShowTrianglesIds)
            {
                foreach (var tri in _triangles)
                {
                    if (hideList.Contains(tri.TriangleId))
                        continue;

                    var value = new Triangle3(tri.V0, tri.V1, tri.V2);

                    var c = value.Center();
                    Vector3 eps = c;

                    if (TextDistance > 0)
                    {
                        eps += value.Normal() * TextDistance;
                        ctx.Draw(new Line3(c, eps), "#ffff00", 3f);
                    }

                    ctx.DrawText($"{tri.A} {tri.B} {tri.C} [{tri.TriangleId}]", eps, 45, "#ffffff");
                }
            }
        }

        public void BuildSubMesh(int triangleId)
        {
            if (_index == null)
                return;

            if (_slice == null)
            {
                _slice = new TriangleMesh(new Geometry3D());
                _slice.Materials.Add(new WireframeMaterial() 
                { 
                    Color = new Color(1,0,0), 
                    Priority = 2, 
                    UseDepth = false 
                });
                _slice.Materials.Add(new ColorMaterial() 
                { 
                    Color = new Color(0, 1, 0, 0.8f), 
                    Alpha = AlphaMode.Add, 
                    Priority = 1, 
                    UseDepth = false });

                _slice.Name = "Mesh-Slice";
            
            }

            if (_slice.Parent == null)
                _host!.Scene!.AddChild(_slice);

            var result = _index.SearchAroundTriangle(
                triangleId,
                SliceArea,
                includeSelf: true);

            var source = _index.Geometry;
            var sourceVertices = source.Vertices;

            var vertices = new VertexData[result.Count * 3];
            var indices = new uint[result.Count * 3];

            var write = 0;

            for (var i = 0; i < result.Count; i++)
            {
                var tri = result[i].Triangle;

                vertices[write] = sourceVertices[(int)tri.A];
                indices[write] = (uint)write;
                write++;

                vertices[write] = sourceVertices[(int)tri.B];
                indices[write] = (uint)write;
                write++;

                vertices[write] = sourceVertices[(int)tri.C];
                indices[write] = (uint)write;
                write++;
            }

            Log.Warn(this, "{0} triangles found", result.Count);

            var geometry = _slice.Geometry!;

            geometry.Vertices = vertices;
            geometry.Indices = indices;
            geometry.ActiveComponents = source.ActiveComponents;

            geometry.NotifyChanged(ChangeType.Geometry);

            _slice.UpdateBounds();
            _slice.IsVisible = ShowSubMesh;

            _triangles = result.Select(a=> a.Triangle).ToList();
        }

        [Action]
        public async Task PickPoint()
        {
            var pick = Context.Require<IObjectPicker>();

            var collision = await pick.PickAsync(c => c.Object == _host);
            if (collision != null)
            {
                BuildSubMesh((int)collision.TriangleId);
            }
        }

        [Action]
        public void Analyze()
        {

            var filler = new MeshHoleFiller(new MeshHoleFillerParams
            {
                BarycentricEpsilon = 1e-2f,
                CoordMode = MeshVisualTriangleHoleFillCoordMode.Position
            });
            
            Log.Debug(this, "Analyzing {0}...", _triangles.Count);

            var indices = _triangles!.SelectMany(a => new uint[] { a.A, a.B, a.C }).ToArray();

            var result = filler.FindMissingTriangles(_host!.Geometry!.Vertices, indices);

            _newTriangles = result.Select(a => new Triangle(-1, a.A, a.B, a.C, _host.Geometry!.Vertices)).ToList();

            Log.Warn(this, "Found {0}", result.Count);
        }

        public void Dispose()
        {
            _slice?.Dispose();
            _canvas?.Dispose();
            _canvas= null;
            _slice = null;  
            GC.SuppressFinalize(this);
        }

        public float CellSize { get; set; }

        public float SliceArea { get; set; }

        public bool ShowSubMesh
        {
            get => _showSlice;
            set
            {
                _showSlice = value;
                //_host!.IsVisible = !_showSlice;
                _slice?.IsVisible = _showSlice;
            }
        }

        public bool ShowTriangles { get; set; }

        public bool ShowTrianglesIds { get; set; }

        public float TextDistance { get; set; }

        public string? HideList { get; set; }

    }
}
