using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public class BoundsGrabbable : Behavior<Mesh>, IGrabbable
    {
        public bool CanGrab(Vector3 position)
        {
            var localPos = position.Transform(_host!.WorldMatrixInverse);
            
            return _host.Geometry!.Bounds.ContainsPoint(localPos);
        }

        public void Grab()
        {

        }

        public void Release()
        {
        }
    }
}
