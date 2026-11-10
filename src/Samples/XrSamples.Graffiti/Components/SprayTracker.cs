using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Graffiti.Shaders;

namespace XrSamples.Graffiti
{
    public class SprayTracker : Behavior<Can>, IDrawGizmos
    {
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
            BaseDensity = 8f;
            FlowExponent = 1.5f;
            MaskSize = Vector2.One;
            IsEnabled = XrPlatform.IsEditor && false;
        }

        protected override void Update(RenderContext ctx)
        {
            ComputeRays();
        }

        protected override void OnDisabled()
        {
            Footprint?.IsVisible = false;
            base.OnDisabled();
        }

        public void Update(ref SprayUniforms uniforms)
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

            var flow = MathF.Pow(_host.SprayAperture, FlowExponent);
            uniforms.DensityScale = BaseDensity * flow;
            uniforms.RadialFalloff = RadialFalloff;
            uniforms.MaskSize = MaskSize;
            uniforms.MaskAngle = MaskAngle;
        }

        void ComputeRays()
        {
            if (_host == null || Footprint == null)
                return;

            _canvas ??= _host.Scene!.Descendants<PaintCanvas>().First();

            Footprint.SetMask(MaskSize, MaskAngle);

            var geometry = Footprint.Geometry!;
            var vertices = geometry.Vertices;

            if (_rays == null || _rays.Length != vertices.Length)
            {
                _rays = new Ray3[vertices.Length];
                _intersets = new Vector3[vertices.Length];
            }

            var wireframe = (WireframeMaterial)Footprint.Materials[0];
            var wireColor = new Color(_host.Color.R, _host.Color.G, _host.Color.B, wireframe.Color.A);

            if (wireframe.Color != wireColor)
            {
                wireframe.Color = wireColor;
                wireframe.Invalidate();
            }

            var world = _host.Transform.Matrix;

            var localCenter = SprayCenter;

            var localDirection = SprayDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(SprayDirection)
                : Vector3.UnitZ;

            var radius = SprayRadius;
            var angle = MathF.Max(SpreadAngle, 0.0001f);

            var h = radius / MathF.Tan(angle);
            var localApex = localCenter - localDirection * h;

            MathUtils.BuildBasis(localDirection, out var tangent, out var bitangent);

            var canvasPlane = new Quad3()
            {
                Pose = _canvas.GetWorldPose(),
                Size = _canvas.Size
            }.ToPlane();

            var isOnPlane = true;

            for (var i = 0; i < _rays.Length; i++)
            {
                var vertex = vertices[i].UV - new Vector2(0.5f);

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

                if (ray.Intersects(canvasPlane, out var intPoint))
                    _intersets![i] = intPoint - worldDirection * 0.002f;
                else
                {
                    _intersets![i] = ray.PointAt(2f);
                    isOnPlane = false;
                }

                vertices[i].Pos = _intersets[i];
            }

            geometry.NotifyChanged(ChangeType.Geometry);
            geometry.ComputeNormals();
            Footprint.IsVisible = isOnPlane;
        }

        void IDrawGizmos.DrawGizmos(Canvas3D canvas, RenderContext ctx)
        {
            if (_rays == null || _intersets == null)
                return;

            canvas.Save();
            canvas.State.Color = _host.Color;

            for (var i = 0; i < _rays.Length; i++)
                canvas.DrawLine(_rays[i].Origin, _intersets[i]);

            canvas.Restore();
        }

        bool IDrawGizmos.IsEnabled => _isEnabled && ShowRays;

        [ValueType(XrEngine.ValueType.Radiant)]
        public float SpreadAngle { get; set; }

        public Vector3 SprayCenter { get; set; }

        public Vector3 SprayDirection { get; set; }

        public float SprayRadius { get; set; }

        public float RadialFalloff { get; set; }

        public float BaseDensity { get; set; }

        public float FlowExponent { get; set; }

        public Vector2 MaskSize { get; set; }

        [ValueType(XrEngine.ValueType.Radiant)]
        public float MaskAngle { get; set; }

        public SprayBrush? Brush { get; set; }

        public SprayBrush? Footprint { get; set; }

        public bool ShowRays { get; set; }
    }
}
