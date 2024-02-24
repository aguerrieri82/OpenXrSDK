using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class BoundsGrabbable : Behavior<TriangleMesh>, IGrabbable
    {
        public bool CanGrab(Vector3 position)
        {
            var localPos = position.Transform(_host!.WorldMatrixInverse);

            return _host.Geometry!.Bounds.Contains(localPos);
        }

        public void Grab()
        {

        }

        public void Release()
        {
        }
    }
}
