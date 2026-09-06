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

        public uint TriangleId;

        public Collision Clone()
        {
            return (Collision)MemberwiseClone();
        }
    }

    [Flags]
    public enum ColliderUsage
    {
        None = 0x0,
        Physics = 0x1,
        Collisions = 0x2,
        All = Physics | Collisions
    }

    public interface ICollider3D : IComponent
    {
        Collision? CollideWith(Ray3 ray);

        bool ContainsPoint(Vector3 worldPoint, float tolerance = 0f);

        void Initialize();

        ColliderUsage Usage { get; }
    }
}
