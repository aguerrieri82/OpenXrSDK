using DotSpatial.Projections.Transforms;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
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
            DistanceFalloff = 4.0f;
            RadialFalloff = 2.0f;
            BaseDensity = 1.0f;
        }

        protected override void Start(RenderContext ctx)
        {
            _brush = _host!.Scene!.Descendants<SprayBrush>().First();
            _intersets = new Vector3[_brush.Geometry!.Vertices.Length];
            _rays = new Ray3[_brush.Geometry!.Vertices.Length];
            _originalGeo = _brush.Geometry!.Clone();

            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            ComputeRays();

            base.Update(ctx);
        }

        public void Update(ref PaintProjUniforms uniforms)
        {
            if (_host == null)
                return;



            _canvas ??= _host.Scene!.Descendants<PaintCanvas>().First();

            var hostLocalToWorld = _host.Transform.Matrix;

            var canvasLocalToWorld = _canvas.GetWorldPose().ToMatrix();

            if (!Matrix4x4.Invert(canvasLocalToWorld, out var canvasWorldToLocal))
                return;

            var sprayDirection = SprayDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(SprayDirection)
                : Vector3.UnitZ;

            uniforms.HostLocalToWorld = hostLocalToWorld;
            uniforms.CanvasWorldToLocal = canvasWorldToLocal;
            uniforms.CanvasLocalToWorld = canvasLocalToWorld;

            uniforms.SprayCenterLocal = SprayCenter;
            uniforms.SprayDirectionLocal = sprayDirection;

            uniforms.SprayRadius = SprayRadius;
            uniforms.SpreadAngle = MathF.Max(SpreadAngle, 0.0001f);

            uniforms.CanvasSize = new Vector2(
                _canvas.WorldBounds.Size.X,
                _canvas.WorldBounds.Size.Y
            );

            float t = Math.Clamp(_host.SprayAperture, 0.0f, 1.0f);


            const float minRange = 0.35f;
            const float flowExponent = 1.5f;

            float flow = MathF.Pow(t, flowExponent);
            float range = minRange + (1.0f - minRange) * t;

            uniforms.DensityScale = BaseDensity * flow;
            uniforms.DistanceFalloff = DistanceFalloff / (range * range);
            uniforms.RadialFalloff = RadialFalloff;


        }

        void ComputeRays()
        {
            if (_originalGeo == null || _host == null)
                return;

            _canvas ??= _host!.Scene!.Descendants<PaintCanvas>().First();

            var world = _host.Transform.Matrix;

            Vector3 localCenter = SprayCenter;

            Vector3 localDirection = SprayDirection.LengthSquared() > 0.000001f
                ? Vector3.Normalize(SprayDirection)
                : Vector3.UnitZ;

            float radius = SprayRadius;
            float angle = MathF.Max(SpreadAngle, 0.0001f);

            float h = radius / MathF.Tan(angle);

            Vector3 localApex = localCenter - localDirection * h;

            BuildBasis(localDirection, out var tangent, out var bitangent);


            var wordQuod = new Quad3()
            {
                Pose = _canvas.GetWorldPose(),
                Size = new Vector2(_canvas.WorldBounds.Size.X, _canvas.WorldBounds.Size.Y)
            };

            for (var i = 0; i < _rays!.Length; i++)
            {
                var vertex = _originalGeo!.Vertices[i].Pos;

                float x = vertex.X * radius * 2f;
                float y = vertex.Y * radius * 2f;

                Vector3 localOrigin =
                    localCenter +
                    tangent * x +
                    bitangent * y;

                Vector3 localRayDirection =
                    Vector3.Normalize(localOrigin - localApex);

                Vector3 worldOrigin =
                    localOrigin.Transform(world);

                Vector3 worldDirection =
                    Vector3.Normalize(Vector3.TransformNormal(localRayDirection, world));

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

                _brush!.Geometry!.Vertices[i].Pos = _intersets[i];

            }

            _brush!.Geometry!.NotifyChanged(ObjectChangeType.Geometry);
            _brush.Geometry.ComputeNormals();

        }

        void IDrawGizmos.DrawGizmos(Canvas3D canvas)
        {
            if (_rays == null || _intersets == null)
                return;

            canvas.Save();
            canvas.State.Color = new Color(1, 0, 0);

            for (var i = 0; i < _rays.Length; i++)
                canvas.DrawLine(_rays[i].Origin, _intersets[i]);

            canvas.Restore();
        }

        private static void BuildBasis(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            normal = Vector3.Normalize(normal);

            Vector3 helper = MathF.Abs(Vector3.Dot(normal, Vector3.UnitY)) < 0.999f
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

        public float DistanceFalloff { get; set; }

        public float BaseDensity { get; set; }
    }
}
