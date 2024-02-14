using System.Numerics;

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
