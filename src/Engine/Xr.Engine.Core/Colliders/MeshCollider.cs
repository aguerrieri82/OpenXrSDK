using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class MeshCollider : Behavior<Mesh>, ICollider
    {
        public Collision? CollideWith(Ray3 ray)
        {
            var tRay = ray.Transform(_host!.WorldMatrixInverse);

            foreach (var triangle in _host!.Geometry!.Triangles())
            {
                var point = triangle.RayIntersect(tRay, out var _);
                if (point != null)
                {
                    var worldPoint = Vector3.Transform(point.Value, _host.WorldMatrix);
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
