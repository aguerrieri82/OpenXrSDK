using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.Physics
{
    public class PhysicsManager : Behavior<Scene>
    {
        PhysicsSystem _system;

        public PhysicsManager()
        {
            _system = new PhysicsSystem();
            Gravity = new Vector3(0, -9.81f, 0);
            StepSizeSecs = 1f / 100;
        }

        protected override void Start(RenderContext ctx)
        {
            _system.Create("localhost", 5425);
            _system.CreateScene(Gravity);
        }

        protected override void Update(RenderContext ctx)
        {
            _system.Simulate((float)DeltaTime, StepSizeSecs);

            base.Update(ctx);
        }

        public float StepSizeSecs { get; set; }

        public PhysicsSystem System => _system;

        public Vector3 Gravity { get; set; }    
    }
}
