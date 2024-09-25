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

            /*
            var wordBounds = bounds.Transform(_host!.WorldMatrix);

            if (wordBounds.Intersects(ray.ToLine(10000), out var distance))
            {
                return new Collision()
                {
                    Distance = distance,
                    LocalPoint = Vector3.Transform(ray.Origin + ray.Direction * distance, _host.WorldMatrixInverse),
                    Object = _host,
                };
            }
            */

            var localRay = ray.Transform(_host!.WorldMatrixInverse);
            
            var bounds = new Bounds3
            {
                Min = Center - Size / 2,
                Max = Center + Size / 2
            };

            if (bounds.Intersects(localRay.ToLine(10000), out var distance))
            {
                var localPoint = localRay.Origin + localRay.Direction * distance;
                var wordPoint = localPoint.Transform(_host!.WorldMatrix);
                var distance2 = (wordPoint - ray.Origin).Length();

                return new Collision()
                {
                    Distance = distance2,
                    LocalPoint = localPoint,
                    Point = wordPoint,
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


        public Vector3 Size { get; set; }

        public Vector3 Center { get; set; }

    }
}
