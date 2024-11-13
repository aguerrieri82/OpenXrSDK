using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class CylinderCollider : Behavior<Object3D>, ICollider3D
    {
        public Collision? CollideWith(Ray3 ray)
        {
            //TODO: Implement
            return null;
        }

        public bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f)
        {
            //TODO: Implement
            return false;
        }

        public void Initialize()
        {
            //TODO: Implement
        }


        public float Radius { get; set; }

        public float Height { get; set; }

        public Pose3 Pose { get; set; }
    }
}
