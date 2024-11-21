using System.Numerics;

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
