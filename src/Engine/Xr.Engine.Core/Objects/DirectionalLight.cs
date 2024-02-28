using System.Numerics;

namespace Xr.Engine
{
    public class DirectionalLight : Light
    {
        public DirectionalLight()
        {

        }

        public DirectionalLight(Vector3 direction)
        {
            Direction = direction;
        }

        public Vector3 Direction { get; set; }
    }
}
