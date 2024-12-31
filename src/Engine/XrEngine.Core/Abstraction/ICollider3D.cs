using System.Numerics;
using XrMath;

namespace XrEngine
{
    public class Collision
    {
        public Vector3 Point;

        public Vector3 LocalPoint;

        public Vector2? UV;

        public Vector3? Normal;

        public float Distance;

        public Object3D? Object;

        public Vector4? Tangent;
    }


    public interface ICollider3D : IComponent
    {
        Collision? CollideWith(Ray3 ray);

        bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f);

        void Initialize();
    }
}
