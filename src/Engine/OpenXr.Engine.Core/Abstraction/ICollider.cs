using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class Collision
    {
        public Vector3 Point;

        public Vector2? UV;

        public float Distance;

        public Object3D? Object;
    }


    public interface ICollider : IComponent
    {
        Collision? CollideWith(Ray3 ray);
    }
}
