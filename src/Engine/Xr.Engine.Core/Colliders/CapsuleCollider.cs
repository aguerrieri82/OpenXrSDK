using System.Numerics;

namespace Xr.Engine
{
    [Flags]
    public enum CapsuleColliderMode
    {
        Top = 0x1,
        Center = 0x2,
        Bottom = 0x4,
        All = Top | Center | Bottom,    
    }

    public class CapsuleCollider : Behavior<Object3D>, ICollider
    {
        public Collision? CollideWith(Ray3 ray)
        {
            var localRay = ray.Transform(_host!.WorldMatrixInverse);

            Vector3 sphereCenter = new Vector3(0, 0, Height / 2);

            Vector3 oc = sphereCenter - localRay.Origin;

            float tca = Vector3.Dot(oc, localRay.Direction);

            float d2 = Vector3.Dot(oc, oc) - tca * tca;

            float thc = (float)Math.Sqrt(Radius * Radius - d2);

            float t0 = tca - thc;
            float t1 = tca + thc;

            Vector3 intersectionPoint1 = localRay.Direction * t0;
            Vector3 intersectionPoint2 = localRay.Direction * t1;


            throw new NotImplementedException();
        }

        public float Height { get; set; }

        public float Radius { get; set; }   

        public CapsuleColliderMode Mode { get; set; }
    }
}
