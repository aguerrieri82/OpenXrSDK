using System.Numerics;

namespace OpenXr.Engine
{
    public class Collision
    {
        public Vector3 Point;

        public Vector3 LocalPoint;

        public Vector2? UV;

        public float Distance;

        public Object3D? Object;
    }


    public interface ICollider : IComponent
    {
        Collision? CollideWith(Ray3 ray);
    }
}
