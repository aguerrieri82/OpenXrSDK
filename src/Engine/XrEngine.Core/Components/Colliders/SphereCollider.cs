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


        public bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f)
        {
            var localPoint = _host!.ToLocal(worldPoint);

            return Vector3.Distance(localPoint, Center) <= Radius;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            var localRay = ray.Transform(_host!.WorldMatrixInverse);

            var sphere = new Sphere(Center, Radius);

            var point = ray.Intersects(sphere, out _);

            if (point == null)
                return null;

            var worldPoint = _host.ToWorld(point.Value);

            return new Collision
            {
                Distance = Vector3.Distance(worldPoint, ray.Origin),
                Point = worldPoint,
                LocalPoint = point.Value,
                Object = _host,
                Normal = (point.Value - Center).Normalize()
            };
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Radius = container.Read<float>(nameof(Radius));
            Center = container.Read<Vector3>(nameof(Center));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Radius), Radius);
            container.Write(nameof(Center), Center);
        }

        public float Radius { get; set; }

        public Vector3 Center { get; set; }
    }
}
