using System.Drawing;
using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class SphereCollider : Behavior<Object3D>, ICollider3D
    {
        public SphereCollider()
        {
        }

        public Collision? CollideWith(Ray3 ray)
        {
            //TODO implement
            return null;
        }

        protected override void SetStateWork(StateContext ctx, IStateContainer container)
        {
            base.SetStateWork(ctx, container);
            Radius = container.Read<float>(nameof(Radius));
        }

        public override void GetState(StateContext ctx, IStateContainer container)
        {
            base.GetState(ctx, container);
            container.Write(nameof(Radius), Radius);
        }


        public float Radius { get; set; }
    }
}
