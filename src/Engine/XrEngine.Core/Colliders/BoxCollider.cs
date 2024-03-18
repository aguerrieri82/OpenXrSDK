using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class BoxCollider : Behavior<Object3D>, ICollider3D
    {
        public Collision? CollideWith(Ray3 ray)
        {
            //TODO implement
            return null;
        }

        public Vector3 Size { get; set; }
    }
}
