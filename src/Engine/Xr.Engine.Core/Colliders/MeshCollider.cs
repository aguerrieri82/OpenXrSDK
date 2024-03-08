using System.Diagnostics;
using System.Numerics;
using Xr.Math;

namespace Xr.Engine
{
    public class MeshCollider : Behavior<Object3D>, ICollider3D
    {
        long _version = -1;
        Triangle3[]? _triangles;
        Geometry3D? _geometry;

        public MeshCollider()
        {
        }

        public MeshCollider(Geometry3D geometry)
        {
            _geometry = geometry;   
        }

        protected override void Start(RenderContext ctx)
        {
            if (_geometry == null)
            {
                _geometry = _host!.Feature<Geometry3D>();

                if (_geometry == null)
                    throw new Exception("Geometry3D not found in Object");
            }
        }

        void Update()
        {
            Debug.Assert(_geometry != null);

            _triangles = _geometry!.Triangles().ToArray();
            _version = _geometry.Version;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_geometry == null)
                return null;

            if (_version != _geometry.Version)
                Update();

            var localRay = ray.Transform(_host!.WorldMatrixInverse);

            var span = _triangles.AsSpan();

            for (var i = 0; i < span.Length; i++)
            {
                var point = localRay.Intersects(span[i], out var _);
                if (point != null)
                {
                    var worldPoint = point.Value.Transform(_host.WorldMatrix);
                    return new Collision
                    {
                        Distance = Vector3.Distance(worldPoint, ray.Origin),
                        Object = _host,
                        LocalPoint = point.Value,
                        Point = worldPoint,
                        Normal = span[i].Normal(),
                        UV = null
                    };
                }
            }

            return null;

        }


        public Geometry3D Geometry => _geometry;

    }
}
