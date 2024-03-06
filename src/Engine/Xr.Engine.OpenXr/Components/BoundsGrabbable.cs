using System.Numerics;

namespace Xr.Engine.OpenXr
{
    public class BoundsGrabbable : Behavior<Object3D>, IGrabbable
    {
        public bool CanGrab(Vector3 position)
        {
            var mesh = _host!.Feature<TriangleMesh>();
            if (mesh != null)
                return mesh.Geometry!.Bounds.Contains(position.Transform(_host.WorldMatrixInverse));

            return _host!.WorldBounds.Contains(position);   
        }

        public void Grab()
        {

        }

        public void Release()
        {
        }
    }
}
