using XrMath;

namespace XrEngine
{
    public class SphereCollider : Behavior<Object3D>, ICollider3D
    {
        public SphereCollider()
        {
        }

        public void Initialize()
        {

        }

        public Collision? CollideWith(Ray3 ray)
        {
            //TODO implement
            return null;
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Radius = container.Read<float>(nameof(Radius));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Radius), Radius);
        }

        public float Radius { get; set; }
    }
}
