using PhysX.Framework;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Physics;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Chess")]
        public static XrEngineAppBuilder CreateChess(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Chess/ABeautifulGame.bin");

            var mesh = (Group3D)AssetLoader.Instance.Load(new Uri("res://asset/Chess/ABeautifulGame.gltf"), typeof(Group3D), null, GltfOptions);
            mesh.Name = "mesh";
            mesh.BoundUpdateMode = UpdateMode.Automatic;

            foreach (var child in mesh.Children)
            {
                var rb = child.AddComponent<RigidBody>();
                child.AddComponent<BoxCollider>();

                if (child.Name!.Contains("board"))
                {
                    rb.Type = PhysicsActorType.Static;
                    child.Transform.SetPositionY(-0.25f);
                }
                else
                    child.AddComponent<BoundsGrabbable>();
                /*
                if (child is TriangleMesh mc)
                    XrEngine.MeshOptimizer.Simplify(mc.Geometry!);
                */
            }

            mesh.Transform.SetScale(4f);
            mesh.Transform.Position = new Vector3(0, 1.5f, 0);

            scene.AddChild(mesh);
            scene.PerspectiveCamera.Target = mesh.Transform.Position;

            return builder
                    .UseApp(app)
                    .ConfigureSampleApp()
                    .UseDefaultHDR()
                    .UsePhysics(new PhysicsOptions());
        }
    }
}
