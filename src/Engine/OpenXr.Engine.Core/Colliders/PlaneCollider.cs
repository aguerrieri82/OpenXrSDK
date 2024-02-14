using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class PlaneCollider : Behavior<Mesh>, ICollider
    {
        public Collision? CollideWith(Ray3 ray)
        {
            var normal = _host!.Forward;
            var origin = _host.WorldPosition;

            var difference = origin - ray.Origin;
            var product1 = Vector3.Dot(difference, normal);
            var product2 = Vector3.Dot(ray.Direction, normal);
            var distance = product1 / product2;
            if (distance >= 0)
            {
                var intersection = ray.Origin + ray.Direction * distance;
                return new Collision
                {
                    Distance = distance,
                    Object = _host,
                    Point = intersection,
                    UV = null
                };
            }

            return null;
        }
    }
}
