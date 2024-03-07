using System.Numerics;

namespace Xr.Engine
{
    public class MeshCollider : Behavior<Object3D>, ICollider3D
    {
        long _version = -1;
        Triangle3[]? _triangles;

        void Update()
        {
            var geo = _host!.Feature<Geometry3D>();

            if (geo == null)
                throw new Exception("Geometry3D not found in Object");

            _triangles = geo.Triangles().ToArray();
            _version = geo.Version;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_version != _host!.Feature<Geometry3D>()!.Version)
                Update();

            var tRay = ray.Transform(_host!.WorldMatrixInverse);

            var span = _triangles.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                var point = tRay.Intersects(span[i], out var _);
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
