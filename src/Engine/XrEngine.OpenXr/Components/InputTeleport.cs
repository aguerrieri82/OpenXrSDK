using System.Diagnostics;
using System.Numerics;
using XrInteraction;
using XrMath;

namespace XrEngine.OpenXr
{
    public class FloorTeleportTarget : ITeleportTarget
    {
        public bool CanTeleport(Vector3 point)
        {
            if (point.Y != Y)
                return false;
            if (Bounds != null)
                return Bounds.Value.Contains(new Vector2(point.X, point.Z));

            return true;
        }

        public IEnumerable<float> GetYPlanes()
        {
            yield return Y;
        }


        public Bounds2? Bounds { get; set; }

        public float Y { get; set; }
    }


    public class SceneTeleportTarget : BaseComponent<Scene3D>, ITeleportTarget
    {
        protected IEnumerable<ITeleportTarget> Targets()
        {
            Debug.Assert(_host != null);
            return _host!.DescendantsWithFeature<ITeleportTarget>().Select(a => a.Feature);
        }

        public bool CanTeleport(Vector3 point)
        {
            return Targets().Any(a => a.CanTeleport(point));
        }

        public IEnumerable<float> GetYPlanes()
        {
            return Targets().SelectMany(a => a.GetYPlanes());
        }
    }


    public class InputTeleport : Behavior<Object3D>, IDrawGizmos
    {
        readonly TeleportRayView _rayView;
        Ray3 _lastRay;
        Vector3 _hitDest;
        Vector3 _lastIntPoint;
        bool _lastIntValid;
        bool _isTeleportStart;
        float _lastElev;
        float _lastMaxT;

        public InputTeleport()
        {
            _rayView = new TeleportRayView();
            ElevationFactor = 2.2f;
            MaxRange = new Vector2(10, 3);
            IsSimulation = false;
            MinY = 0.2f;
            SmoothFactor = 0.08f;
            Exponent = 2;
        }


        protected override void Start(RenderContext ctx)
        {
            _host!.Scene!.AddChild(_rayView);
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            if (Target == null || Pointer == null || IsTriggerActive == null)
                return;

            if (IsSimulation)
            {
                var obj = _host!.Scene!.Children.OfType<XrRoot>().First().LeftController!;

                _lastRay = obj.GetWorldPose().ToRay();
                _lastRay.Origin = _host!.WorldPosition;
            }
            else
            {
                var isActive = IsTriggerActive();

                if (isActive)
                {
                    if (!_isTeleportStart)
                    {
                        _isTeleportStart = true;
                        _lastIntValid = false;
                    }
                }
                else
                {
                    if (_isTeleportStart)
                    {
                        if (_lastIntValid)
                            Teleport(_lastIntPoint);
                        _isTeleportStart = false;
                    }
                }

                if (_isTeleportStart)
                {
                    var status = Pointer.GetPointerStatus();
                    if (!status.IsActive)
                        return;
                    _lastRay = Pointer.GetPointerStatus().Ray;
                }
            }

            _lastIntValid = false;

            foreach (var yPlane in Target.GetYPlanes().OrderByDescending(a => a))
            {
                var intPoint = RayIntersection(_lastRay, yPlane);

                if (Target.CanTeleport(intPoint))
                {
                    _lastIntPoint = intPoint;
                    _lastIntValid = true;
                    break;
                }
            }

            if (_isTeleportStart || IsSimulation)
            {
                _rayView.Update(Sample(_rayView.Segments), _lastIntValid);
                _rayView.IsVisible = true;
            }
            else
                _rayView.IsVisible = false;
        }


        protected virtual void Teleport(Vector3 position)
        {
            _host!.WorldPosition = position;
        }

        [Action]
        public void Teleport()
        {
            Teleport(_lastIntPoint);
        }

        public Vector3 RayIntersection(Ray3 ray, float yPlane)
        {
            var cos = Math.Clamp(ray.Direction.Dot(Vector3.UnitY), 0, 1);
            var curElev = 1 - (MathF.Acos(cos) / (MathF.PI / 2));
            curElev = MathF.Pow(curElev, Exponent) + 0.05f;

            _lastElev = MathUtils.Smooth(_lastElev, curElev, SmoothFactor);

            var yMax = MinY +
                (MathF.Pow(MathF.E, ElevationFactor * _lastElev) - 1) / (MathF.Pow(MathF.E, ElevationFactor) - 1) *
                (MaxRange.Y - MinY);

            var ofsPlane = yPlane - ray.Origin.Y;

            var det = MathF.Sqrt(1 - ofsPlane / yMax);
            var t0 = (1 + det) / 2;
            var t1 = (1 - det) / 2;

            _lastMaxT = MathF.Max(t0, t1);

            var xSpan = _lastMaxT * MaxRange.X * _lastElev;

            _hitDest = new Vector3(ray.Direction.X, 0, ray.Direction.Z).Normalize() * xSpan + new Vector3(ray.Origin.X, yPlane, ray.Origin.Z);

            _lastRay = ray;

            return _hitDest;
        }

        protected IEnumerable<Vector3> Sample(int numPoints)
        {
            var part = _lastMaxT / (numPoints - 1);
            for (var i = 0; i < numPoints; i++)
            {
                var t = i == numPoints - 1 ? _lastMaxT : part * i;
                var p0 = SamplePoint(t);
                yield return p0;
            }
        }

        protected Vector3 SamplePoint(float t)
        {
            var yMax = MinY +
                (MathF.Pow(MathF.E, ElevationFactor * _lastElev) - 1) / (MathF.Pow(MathF.E, ElevationFactor) - 1) *
                (MaxRange.Y - MinY);

            var xMax = MaxRange.X * _lastElev;

            var x = t * xMax;
            var y = 4 * yMax * t * (1 - t);

            var res = new Vector3(_lastRay.Direction.X, 0, _lastRay.Direction.Z).Normalize() * x +
                      new Vector3(_lastRay.Origin.X, _lastRay.Origin.Y + y, _lastRay.Origin.Z);

            return res;

        }

        public void DrawGizmos(Canvas3D canvas)
        {
            if (!_isTeleportStart && !IsSimulation)
                return;

            canvas.Save();
            canvas.State.Color = "#00FFFF";
            //canvas.DrawLine(_lastRay.PointAt(0), _lastRay.PointAt(2f));

            if (!_lastIntValid)
                canvas.State.Color = "#FF0000";

            /*
            canvas.DrawCircle(new Pose3
            {
                Position = _hitDest,
                Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2)
            }, 0.2f);


            var part = _lastMaxT / 20f;
            var curT = 0.0f;
            while (curT < _lastMaxT)
            {
                var p0 = SamplePoint(curT);
                var p1 = SamplePoint(curT + part);
                canvas.DrawLine(p0, p1);
                curT += part;
            }
            */

            canvas.Restore();

        }

        bool IDrawGizmos.IsEnabled => true;

        public bool IsSimulation { get; set; }

        public Func<bool>? IsTriggerActive { get; set; }

        public ITeleportTarget? Target { get; set; }

        public IRayPointer? Pointer { get; set; }

        public float ElevationFactor { get; set; }

        public Vector2 MaxRange { get; set; }

        public float MinY { get; set; }


        [Range(0, 1, 0.01f)]
        public float SmoothFactor { get; set; }

        [Range(1, 10, 0.1f)]
        public float Exponent { get; set; }
    }
}
