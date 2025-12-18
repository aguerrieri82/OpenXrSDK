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
            Vector3 localPoint = _host!.ToLocal(worldPoint);
            Vector3 invPos = Pose.Inverse().Transform(localPoint);

            if (invPos.Y < -Height / 2 - tolerance || invPos.Y > Height / 2 + tolerance)
                return false;

            float radius = new Vector2(invPos.X, invPos.Z).Length();

            return radius + tolerance <= Radius;
        }



        public float Distance(Vector3 worldPoint)
        {
            Vector3 localPoint = _host!.ToLocal(worldPoint);
            Vector3 invPos = Pose.Inverse().Transform(localPoint);

            float dist = 0f;

            if (invPos.Y > Height / 2)
                dist = invPos.Y - Height / 2;
            else if (invPos.Y < -Height / 2)
                dist = -Height / 2 - invPos.Y;

            float radius = new Vector2(invPos.X, invPos.Z).Length();
            if (radius > Radius)
            {
                float rDist = radius - Radius;
                if (dist > 0)
                    dist = MathF.Min(rDist, dist);
                else
                    dist = rDist;
            }

            return dist;
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
