using System.Numerics;

namespace OpenXr.Engine
{
    public class MeshCollider : Behavior<Mesh>, ICollider
    {
        long _version = -1;
        Triangle3[]? _triangles;

        void Update()
        {
            _triangles = _host!.Geometry!.Triangles().ToArray();
            _version = _host!.Geometry!.Version;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_version != _host!.Geometry!.Version)
                Update();

            var tRay = ray.Transform(_host!.WorldMatrixInverse);

            var span = _triangles.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                var point = span[i].RayIntersect(tRay, out var _);
                if (point != null)
                {
                    var worldPoint = point.Value.Transform(_host.WorldMatrix);
                    return new Collision
                    {
                        Distance = Vector3.Distance(worldPoint, ray.Origin),
                        Object = _host,
                        LocalPoint = point.Value,
                        Point = worldPoint,
                        UV = null
                    };
                }
            }

            return null;

        }

    }
}
