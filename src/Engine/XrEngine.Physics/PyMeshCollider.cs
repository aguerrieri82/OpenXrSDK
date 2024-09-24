using PhysX.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine.Physics
{
    public class PyMeshCollider : Behavior<Object3D>, ICollider3D
    {
        private PhysicsManager? _manager;
        private PhysicsSystem? _system;

        protected override void Start(RenderContext ctx)
        {
            _manager = _host!.Scene!.Components<PhysicsManager>().FirstOrDefault();
            if (_manager == null)
                throw new Exception("PhysicsManager not found in Scene");
            _system = _manager.System;
        }

        public Collision? CollideWith(Ray3 ray)
        {
            foreach (var item in _host!.DescendantsOrSelf())
            {
                var geo = item.Feature<Geometry3D>();
                if (geo == null)
                    continue;

                var pyGeo = geo.GetOrCreateProp("PyGeo", () =>
                {
                    geo.EnsureIndices();

                    return _system!.CreateTriangleMesh(
                        geo.Indices,
                        geo.ExtractPositions(),
                        Vector3.One,
                        0.01f);
                });

                var localRay = ray.Transform(item.WorldMatrixInverse);

                if (!geo.Bounds.Intersects(localRay.ToLine(1000f), out _))
                    continue;

                var result = pyGeo.Raycast(ray, item.WorldMatrix.ToPose(), 1000, PhysX.PxHitFlags.Normal, 2);

                if (result.Length == 0)
                    continue;

                var min = result.Min(a=> a.Distance);

                var minRes = result.FirstOrDefault(a => a.Distance == min); 

                return new Collision
                {
                    Distance = min,
                    Normal = minRes.Normal,
                    Point = minRes.Position,
                    UV = minRes.UV,
                    Object = item
                };
            }
            return null;
        }

        public void Initialize()
        {

        }
    }
}
