using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine
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

        public void Initialize()
        {
            if (_geometry == null)
            {
                _geometry = _host!.Feature<Geometry3D>();

                if (_geometry == null)
                    throw new NotSupportedException("Geometry3D not found in Object");
            }
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            _geometry = container.Read<Geometry3D?>(nameof(Geometry));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Geometry), Geometry);
        }

        void Update()
        {
            Debug.Assert(_geometry != null);

            _triangles = _geometry!.Triangles().ToArray();
            _version = _geometry.Version;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            Initialize();

            if (_geometry == null)
                return null;

            if (_version != _geometry.Version)
                Update();

            var localRay = ray.Transform(_host!.WorldMatrixInverse);

            if (!_geometry.Bounds.Intersects(localRay.ToLine(1000f), out _))
                return null;

            var span = _triangles.AsSpan();

            Collision? collision = null;

            for (var i = 0; i < span.Length; i++)
            {
                var point = localRay.Intersects(span[i], out var _);
                if (point != null)
                {
                    var worldPoint = point.Value.Transform(_host.WorldMatrix);
                    var distance = Vector3.Distance(worldPoint, ray.Origin);

                    if (collision == null || distance < collision.Distance)
                    {
                        uint ix = (uint)i * 3;
                        if (_geometry.Indices.Length > 0)
                            ix = _geometry.Indices[ix];

                        collision = new Collision
                        {
                            Distance = distance,
                            Object = _host,
                            LocalPoint = point.Value,
                            Point = worldPoint,
                            Normal = _geometry.Vertices[ix].Normal,
                            Tangent = _geometry.Vertices[ix].Tangent,
                            UV = null
                        };
                    }
                }
            }

            return collision;
        }


        public Geometry3D? Geometry => _geometry;

    }
}
