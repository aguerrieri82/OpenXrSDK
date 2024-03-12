using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
