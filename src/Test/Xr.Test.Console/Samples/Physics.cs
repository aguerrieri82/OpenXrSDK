using MagicPhysX;
using Microsoft.Extensions.Logging;
using System.Numerics;
using Xr.Engine.Physics;
using static MagicPhysX.NativeMethods;

namespace OpenXr.Samples
{
    public static class Physics
    {
        public unsafe static Task Run(IServiceProvider services, ILogger logger)
        {
            var manager = new PhysicsSystem();
            manager.Create("127.0.0.1", 5425);
            manager.CreateScene(new Vector3(0, -9.81f, 0));
            var mat = manager.CreateMaterial(new PhysicsMaterialInfo { StaticFriction = 0.5f, DynamicFriction = 0.5f, Restitution = 0.6f });
            var geo = manager.CreateBox(new Vector3(0.5f, 0.5f, 0.5f));
            var shape = manager.CreateShape(new PhysicsShapeInfo { Geometry = geo, Material = mat });
            var actor = manager.CreateActor(new PhysicsActorInfo { Shapes = [shape], Transform = PxTransform_new_2(PxIDENTITY.PxIdentity), Type = PhysicsActorType.Dynamic, Density = 10f });

            for (int i = 0; i < 300; i++)
            {
                manager.Simulate(1 / 30f, 1 / 30f);

                var pose = actor.GlobalPose;

                Console.WriteLine($"{i:000}: {pose.p.y}");
            }


            Console.ReadKey();

            return Task.CompletedTask;
        }
    }
}
