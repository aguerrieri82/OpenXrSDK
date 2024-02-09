using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public class DirectionalLight : Light
    {
        public DirectionalLight()
        {

        }

        public DirectionalLight(Vector3 direction)
        {
            Transform.Orientation = Quaternion.Normalize(new Quaternion(direction.X, direction.Y, direction.Z, 0));
        }


    }
}
