using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BoxCollider : Behavior<Object3D>, ICollider3D
    {

        protected override void Start(RenderContext ctx)
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

        }

        public Collision? CollideWith(Ray3 ray)
        {
            var localRay = ray.Transform(_host!.WorldMatrixInverse);
            var bounds = new Bounds3
            {
                Min = Center - Size / 2,
                Max = Center + Size / 2
            };
            if (bounds.Intersects(localRay.ToLine(10000), out var distance))
            {
                return new Collision()
                {
                    Distance = distance,
                    LocalPoint = localRay.Origin + localRay.Direction * distance,
                    Object = _host,
                };
            }
            return null;
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Size = container.Read<Vector3>(nameof(Size));
            Center = container.Read<Vector3>(nameof(Center));
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(Size), Size);
            container.Write(nameof(Center), Center);
        }


        public Vector3 Size { get; set; }

        public Vector3 Center { get; set; }

    }
}
