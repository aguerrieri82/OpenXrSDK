using System.Numerics;
using XrMath;

namespace XrEngine
{
    [Flags]
    public enum CapsuleColliderMode
    {
        Top = 0x1,
        Center = 0x2,
        Bottom = 0x4,
        All = Top | Center | Bottom,
    }


    public class CapsuleCollider : Behavior<Object3D>, ICollider3D
    {
        public CapsuleCollider()
        {
            Pose = Pose3.Identity;
        }

        public void Initialize()
        {

        }

        public bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f)
        {
            Vector3 localPoint = _host!.ToLocal(worldPoint);

            localPoint = Pose.Inverse().Transform(localPoint);

            localPoint.Z += Height / 2;

            if (localPoint.Z < -tolerance || localPoint.Z > Height + tolerance)
                return false;

            float distance = new Vector2(localPoint.X, localPoint.Y).Length();
            if (distance >= Radius + tolerance)
                return false;

            return true;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            Ray3 localRay = ray.Transform(_host!.WorldMatrixInverse);

            Vector3 sphereCenter = new Vector3(0, 0, Height / 2);

            Vector3 oc = sphereCenter - localRay.Origin;

            float tca = Vector3.Dot(oc, localRay.Direction);

            float d2 = Vector3.Dot(oc, oc) - tca * tca;

            float thc = MathF.Sqrt(Radius * Radius - d2);

            float t0 = tca - thc;
            float t1 = tca + thc;

            Vector3 intersectionPoint1 = localRay.Direction * t0;
            Vector3 intersectionPoint2 = localRay.Direction * t1;

            //TODO implement

            return null;
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Height = container.Read<float>(nameof(Height));
            Radius = container.Read<float>(nameof(Radius));
            Mode = container.Read<CapsuleColliderMode>(nameof(Mode));
            Pose = container.Read<Pose3>(nameof(Pose));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Height), Height);
            container.Write(nameof(Radius), Radius);
            container.Write(nameof(Mode), Mode);
            container.Write(nameof(Pose), Pose);
        }

        public float Height { get; set; }

        public float Radius { get; set; }

        public Pose3 Pose { get; set; }

        public CapsuleColliderMode Mode { get; set; }
    }
}
