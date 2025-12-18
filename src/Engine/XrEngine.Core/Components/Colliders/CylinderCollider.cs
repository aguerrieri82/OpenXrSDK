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
            var localPoint = _host!.ToLocal(worldPoint);
            var invPos = Pose.Inverse().Transform(localPoint);

            if (invPos.Y < -Height / 2 - tolerance || invPos.Y > Height / 2 + tolerance)
                return false;

            var radius = new Vector2(invPos.X, invPos.Z).Length();

            return radius + tolerance <= Radius;
        }



        public float Distance(Vector3 worldPoint)
        {
            var localPoint = _host!.ToLocal(worldPoint);
            var invPos = Pose.Inverse().Transform(localPoint);

            var dist = 0f;

            if (invPos.Y > Height / 2)
                dist = invPos.Y - Height / 2;
            else if (invPos.Y < -Height / 2)
                dist = -Height / 2 - invPos.Y;

            var radius = new Vector2(invPos.X, invPos.Z).Length();
            if (radius > Radius)
            {
                var rDist = radius - Radius;
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
