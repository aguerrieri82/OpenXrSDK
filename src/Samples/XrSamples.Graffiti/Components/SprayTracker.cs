using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Graffiti.Shaders;

namespace XrSamples.Graffiti
{
    public class SprayTracker : Behavior<Can>, IDrawGizmos
    {
        Geometry3D? _originalGeo;
        SprayBrush? _brush;
        PaintCanvas? _canvas;
        Ray3[]? _rays;
        Vector3[]? _intersets;

        public SprayTracker()
        {
            SpreadAngle = MathF.PI / 20f;
            SprayDirection = new Vector3(1, 0, 0);
            SprayRadius = 0.01f;
            SprayCenter = new Vector3(0.3f, 1.81f, 0f);
            RadialFalloff = 2.0f;
            BaseDensity = 5f;
            IsEnabled = XrPlatform.IsEditor;
        }

        protected override void Start(RenderContext ctx)
        {
            _brush = _host!.Scene!.Descendants<SprayBrush>().First();
            _intersets = new Vector3[_brush.Geometry!.Vertices.Length];
            _rays = new Ray3[_brush.Geometry!.Vertices.Length];
            _originalGeo = _brush.Geometry!.Clone();
        }

        protected override void Update(RenderContext ctx)
        {
            ComputeRays();
        }

        public void Update(ref PaintProjUniforms uniforms)
        {
            if (_host == null)
                return;

            _canvas ??= _host.Scene!.Descendants<PaintCanvas>().First();

            var sprayDirection = SprayDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(SprayDirection)
                : Vector3.UnitZ;

            uniforms.CanWorld = _host.Transform.Matrix;
            uniforms.CanvasWorldInverse = _canvas.WorldMatrixInverse;
            uniforms.CanvasWorld = _canvas.WorldMatrix;

            uniforms.SprayCenterLocal = SprayCenter;
            uniforms.SprayDirectionLocal = sprayDirection;

            uniforms.SprayRadius = SprayRadius;
            uniforms.SpreadAngle = MathF.Max(SpreadAngle, 0.0001f);

            uniforms.CanvasSize = _canvas.Size;

            var flow = MathF.Pow(_host.SprayAperture, 1.5f);     
            uniforms.DensityScale = BaseDensity * flow;
            uniforms.RadialFalloff = RadialFalloff;
        }

        void ComputeRays()
        {
            if (_originalGeo == null || _host == null || _brush == null)
                return;

            _canvas ??= _host!.Scene!.Descendants<PaintCanvas>().First();

            var world = _host.Transform.Matrix;

            var localCenter = SprayCenter;

            var localDirection = SprayDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(SprayDirection)
                : Vector3.UnitZ;

            var radius = SprayRadius;
            var angle = MathF.Max(SpreadAngle, 0.0001f);

            var h = radius / MathF.Tan(angle);
            var localApex = localCenter - localDirection * h;

            BuildBasis(localDirection, out var tangent, out var bitangent);

            var wordQuod = new Quad3()
            {
                Pose = _canvas.GetWorldPose(),
                Size = _canvas.Size
            };

            for (var i = 0; i < _rays!.Length; i++)
            {
                var vertex = _originalGeo!.Vertices[i].Pos;

                var x = vertex.X * radius * 2f;
                var y = vertex.Y * radius * 2f;

                var localOrigin =
                    localCenter +
                    tangent * x +
                    bitangent * y;

                var localRayDirection = Vector3.Normalize(localOrigin - localApex);
                var worldOrigin = localOrigin.Transform(world);
                var worldDirection = Vector3.Normalize(Vector3.TransformNormal(localRayDirection, world));

                var ray = new Ray3
                {
                    Origin = worldOrigin,
                    Direction = worldDirection
                };

                _rays[i] = ray;

                if (ray.Intersects(wordQuod, out var intPoint))
                    _intersets![i] = intPoint;
                else
                    _intersets![i] = ray.PointAt(2f);

                _brush.Geometry!.Vertices[i].Pos = _intersets[i];
            }

            _brush.Geometry!.NotifyChanged(ObjectChangeType.Geometry);
            _brush.Geometry.ComputeNormals();
            _brush.IsVisible = true;
        }

        void IDrawGizmos.DrawGizmos(Canvas3D canvas)
        {
            if (_rays == null || _intersets == null)
                return;

            canvas.Save();
            canvas.State.Color = _host!.Color;

            for (var i = 0; i < _rays.Length; i++)
                canvas.DrawLine(_rays[i].Origin, _intersets[i]);

            canvas.Restore();
        }

        private static void BuildBasis(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            normal = Vector3.Normalize(normal);

            var helper = MathF.Abs(Vector3.Dot(normal, Vector3.UnitY)) < 0.999f
                ? Vector3.UnitY
                : Vector3.UnitX;

            tangent = Vector3.Normalize(Vector3.Cross(helper, normal));
            bitangent = Vector3.Normalize(Vector3.Cross(normal, tangent));
        }

        bool IDrawGizmos.IsEnabled => _isEnabled;


        [ValueType(XrEngine.ValueType.Radiant)]
        public float SpreadAngle { get; set; }

        public Vector3 SprayCenter { get; set; }

        public Vector3 SprayDirection { get; set; }

        public float SprayRadius { get; set; }

        public float RadialFalloff { get; set; }

        public float BaseDensity { get; set; }
    }
}
