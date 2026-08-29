using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BoxColliderV2 : Behavior<Object3D>, ICollider3D
    {
        protected bool _isInit;

        public BoxColliderV2()
        {
            Usage = ColliderUsage.All;
            Pose = Pose3.Identity;
        }

        protected override void Start(RenderContext ctx)
        {
        }

        public void Initialize()
        {
            if (Size.Length() == 0)
            {
                var local = _host?.Feature<ILocalBounds>();

                if (local != null)
                {
                    local.UpdateBounds(true);
                    Size = local.LocalBounds.Size;

                    var pose = Pose;
                    pose.Position = local.LocalBounds.Center;
                    Pose = pose;
                }
            }
            _isInit = true;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (!_isInit)
                Initialize();

            var hostRay = ray.Transform(_host.WorldMatrixInverse);
            var localRay = hostRay.Transform(Pose.Inverse());

            var halfSize = Size / 2;
            var bounds = new Bounds3
            {
                Min = -halfSize,
                Max = halfSize
            };

            if (bounds.Intersects(localRay.ToLine(10000), out var localDistance))
            {
                var colliderPoint = localRay.PointAt(localDistance);
                var localPoint = Pose.Transform(colliderPoint);
                var wordPoint = _host.ToWorld(localPoint);

                var normal = Vector3.Zero;

                const float epsilon = 0.0001f;

                if (MathF.Abs(colliderPoint.Z - bounds.Min.Z) <= epsilon)
                    normal = -Vector3.UnitZ;

                else if (MathF.Abs(colliderPoint.Z - bounds.Max.Z) <= epsilon)
                    normal = Vector3.UnitZ;

                else if (MathF.Abs(colliderPoint.X - bounds.Min.X) <= epsilon)
                    normal = -Vector3.UnitX;

                else if (MathF.Abs(colliderPoint.X - bounds.Max.X) <= epsilon)
                    normal = Vector3.UnitX;

                else if (MathF.Abs(colliderPoint.Y - bounds.Min.Y) <= epsilon)
                    normal = -Vector3.UnitY;

                else if (MathF.Abs(colliderPoint.Y - bounds.Max.Y) <= epsilon)
                    normal = Vector3.UnitY;

                normal = normal.Transform(Pose.Orientation);

                return new Collision()
                {
                    Distance = (wordPoint - ray.Origin).Length(),
                    LocalPoint = localPoint,
                    Point = wordPoint,
                    Normal = normal,
                    Object = _host,
                };
            }
            return null;
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Size = container.Read<Vector3>(nameof(Size));
            Pose = container.Read<Pose3>(nameof(Pose));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Size), Size);
            container.Write(nameof(Pose), Pose);
        }

        public bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f)
        {
            if (!_isInit)
                Initialize();

            var hostPoint = _host.ToLocal(worldPoint);
            var localPoint = Pose.Inverse().Transform(hostPoint);
            var halfSize = Size / 2 + new Vector3(tolerance);

            var bounds = new Bounds3
            {
                Min = -halfSize,
                Max = halfSize
            };

            return bounds.Contains(localPoint);
        }

        public ColliderUsage Usage { get; set; }

        public Vector3 Size { get; set; }

        public Pose3 Pose { get; set; }
    }
}