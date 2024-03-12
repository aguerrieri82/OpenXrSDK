using System.Numerics;

namespace PhysX.Framework
{
    public struct RaycastHit
    {
        public PhysicsActor? Actor;

        public PhysicsShape? Shape;

        public Vector3 Position;

        public Vector3 Normal;

        public Vector2 UV;

        public float Distance;
    }
}
