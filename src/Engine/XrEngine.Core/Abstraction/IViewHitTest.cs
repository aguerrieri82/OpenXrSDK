using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine
{
    public struct HitTestResult
    {
        public Object3D? Object;
        public Vector3 Normal;
        public Vector3 Pos;
        public float Depth;
    }

    public interface IViewHitTest
    {
        HitTestResult HitTest(uint x, uint y);
    }
}
