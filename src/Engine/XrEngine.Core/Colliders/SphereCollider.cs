using XrMath;

namespace XrEngine.Colliders
{
    public class SphereCollider : Behavior<Object3D>, ICollider3D
    {
        public SphereCollider()
        {
        }

        public Collision? CollideWith(Ray3 ray)
        {
            //TODO implement
            return null;
        }

        public float Radius { get; set; }
    }
}
