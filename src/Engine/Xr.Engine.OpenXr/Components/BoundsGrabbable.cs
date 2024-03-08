using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class BoundsGrabbable : Behavior<Object3D>, IGrabbable
    {
        public bool CanGrab(Vector3 position)
        {
            var local = _host!.Feature<ILocalBounds>();
            if (local != null)
                return local.LocalBounds.Contains(position.Transform(_host.WorldMatrixInverse));

            return false;  
        }

        public void Grab()
        {

        }

        public void Release()
        {
        }
    }
}
