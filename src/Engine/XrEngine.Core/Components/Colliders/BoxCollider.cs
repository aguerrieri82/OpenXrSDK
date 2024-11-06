using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BoxCollider : Behavior<Object3D>, ICollider3D
    {
        protected bool _isInit;

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
                    local.UpdateBounds();
                    Size = local.LocalBounds.Size;
                    Center = local.LocalBounds.Center;
                }
            }
            _isInit = true;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (!_isInit)
                Initialize();

            var localRay = ray.Transform(_host!.WorldMatrixInverse);

            var bounds = new Bounds3
            {
                Min = Center - Size / 2,
                Max = Center + Size / 2
            };

            if (bounds.Intersects(localRay.ToLine(10000), out var localDistance))
            {
                var localPoint = localRay.PointAt(localDistance);
                var wordPoint = _host.ToWorld(localPoint);

                Vector3 normal = Vector3.Zero;

                const float epsilon = 0.0001f;

                if (MathF.Abs(localPoint.Z - bounds.Min.Z) <= epsilon)
                    normal = -Vector3.UnitZ;

                else if (MathF.Abs(localPoint.Z - bounds.Max.Z) <= epsilon)
                    normal = Vector3.UnitZ;

                else if (MathF.Abs(localPoint.X - bounds.Min.X) <= epsilon)
                    normal = -Vector3.UnitX;

                else if (MathF.Abs(localPoint.X - bounds.Max.X) <= epsilon)
                    normal = Vector3.UnitX;

                else if (MathF.Abs(localPoint.Y - bounds.Min.Y) <= epsilon)
                    normal = -Vector3.UnitY;

                else if (MathF.Abs(localPoint.Y - bounds.Max.Y) <= epsilon)
                    normal = Vector3.UnitY;

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
            Center = container.Read<Vector3>(nameof(Center));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Size), Size);
            container.Write(nameof(Center), Center);
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (!_isInit)
                Initialize();

            var localPoint = _host!.ToLocal(worldPoint);

            var bounds = new Bounds3
            {
                Min = Center - Size / 2,
                Max = Center + Size / 2
            };

            return bounds.Contains(localPoint);
        }

        public Vector3 Size { get; set; }

        public Vector3 Center { get; set; }
    }
}
