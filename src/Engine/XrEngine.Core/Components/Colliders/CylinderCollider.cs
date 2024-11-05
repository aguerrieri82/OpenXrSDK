using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using XrMath;

namespace XrEngine
{
    public class CylinderCollider : Behavior<Object3D>, ICollider3D
    {
        public Collision? CollideWith(Ray3 ray)
        {
            return null;
        }

        public bool ContainsPoint(Vector3 globalPoint)
        {
            return false;
        }

        public void Initialize()
        {
           
        }


        public float Radius { get; set; }   

        public float Height { get; set; }

        public Pose3 Pose { get; set; }
    }
}
