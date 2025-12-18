using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class QuadCollider : Behavior<Object3D>, ICollider3D
    {
        bool _isInit;

        public Collision? CollideWith(Ray3 ray)
        {
            if (!_isInit)
                Initialize();

            Ray3 localRay = ray.Transform(_host!.WorldMatrixInverse);

            Plane plane = Quad.ToPlane();

            if (localRay.Intersects(plane, out Vector3 localPoint))
            {
                Vector2 uv = Quad.LocalPointAt(localPoint);

                if (!PlaneMode && !uv.InRange(Vector2.Zero, Quad.Size))
                    return null;

                Vector3 point = localPoint.Transform(_host.WorldMatrix);

                return new Collision
                {
                    Distance = (point - ray.Origin).Length(),
                    Normal = plane.Normal.ToDirection(_host.WorldMatrix),
                    UV = uv,
                    LocalPoint = localPoint,
                    Point = point,
                    Object = _host
                };
            }

            return null;
        }

        public void Initialize()
        {
            if (Quad.Size == Vector2.Zero)
            {
                if (_host is TriangleMesh mesh && mesh.Geometry is Quad3D quad3d)
                {
                    Quad = new Quad3
                    {
                        Size = quad3d.Size,
                        Pose = Pose3.Identity
                    };
                }
            }

            _isInit = true;
        }

        public bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f)
        {
            return false;
        }

        public Quad3 Quad { get; set; }

        public bool PlaneMode { get; set; }

    }
}
