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
            Gravity = new Vector3(0, -0.981f, 0);
        }

        protected override void Start(RenderContext ctx)
        {
            _system.Create("localhost", 5425);
            _system.CreateScene(Gravity);
        }

        protected override void Update(RenderContext ctx)
        {
            _system.Simulate((float)DeltaTime);

            base.Update(ctx);
        }

        public PhysicsSystem System => _system;

        public Vector3 Gravity { get; set; }    
    }
}
