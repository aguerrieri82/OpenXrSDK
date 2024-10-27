using PhysX.Framework;
using System.Numerics;
using XrMath;

namespace XrEngine.Physics
{
    public class PyMeshCollider : Behavior<Object3D>, ICollider3D
    {
        private PhysicsManager? _manager;
        private PhysicsSystem? _system;
        private bool _isInit;

        protected override void Start(RenderContext ctx)
        {
            _manager = _host!.Scene!.Components<PhysicsManager>().FirstOrDefault();
            if (_manager == null)
                throw new Exception("PhysicsManager not found in Scene");
            _system = _manager.System;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            if (_system == null)
                return null;

            Initialize();

            foreach (var item in _host!.DescendantsOrSelf())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                var pyGeo = geo.GetProp<PhysicsGeometry>("PyGeo")!;

                var localRay = ray.Transform(item.WorldMatrixInverse);

                if (!geo.Bounds.Intersects(localRay.ToLine(1000f), out _))
                    continue;

                var result = pyGeo.Raycast(ray, item.WorldMatrix.ToPose(), 1000, PhysX.PxHitFlags.Normal, 2);

                if (result.Length == 0)
                    continue;

                var min = result.Min(a => a.Distance);

                var minRes = result.FirstOrDefault(a => a.Distance == min);

                return new Collision
                {
                    Distance = min,
                    Normal = minRes.Normal.ToDirection(item.WorldMatrixInverse),
                    Point = minRes.Position,
                    LocalPoint = item.ToLocal(minRes.Position),
                    UV = minRes.UV,
                    Object = item
                };
            }
            return null;
        }

        public void Initialize()
        {
            if (_isInit)
                return;
            foreach (var item in _host!.DescendantsOrSelf())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                geo.GetOrCreateProp("PyGeo", () =>
                {
                    geo.EnsureIndices();

                    return _system!.CreateTriangleMesh(
                        geo.Indices,
                        geo.ExtractPositions(),
                        Vector3.One,
                        0.01f);
                });
            }

            _isInit = true;
        }
    }
}
