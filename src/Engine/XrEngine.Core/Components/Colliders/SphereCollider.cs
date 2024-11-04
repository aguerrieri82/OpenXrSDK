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

        public void Initialize()
        {

        }


        public bool ContainsPoint(Vector3 globalPoint)
        {
            var localPoint = _host!.ToLocal(globalPoint);

            return localPoint.Length() <= Radius;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            var sphere = new Sphere(_host!.WorldPosition, Radius);
            
            var point = ray.Intersects(sphere, out var distance);

            if (point == null)
                return null;

            return new Collision
            {
                Distance = distance,
                Point = point.Value,
                LocalPoint = _host.ToLocal(point.Value),
                Object = _host,
                Normal = (point.Value - _host.WorldPosition).Normalize()
            };
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
