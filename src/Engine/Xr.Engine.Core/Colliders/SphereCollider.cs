using Xr.Math;

namespace Xr.Engine.Colliders
{
    public class SphereCollider : Behavior<Object3D>, ICollider3D
    {
        public SphereCollider()
        {
        }


        //TODO implement
        public Collision? CollideWith(Ray3 ray)
        {
            return null;
        }

        public float Radius { get; set; }
    }
}
