using PhysX.Framework;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.Physics;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Bed")]
        public static XrEngineAppBuilder CreateBed(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"), GltfOptions);
            mesh.Name = "Bed 1";
            mesh.AddComponent<PyMeshCollider>();
            mesh.AddComponent<BoundsGrabbable>();

            foreach (var material in mesh.Materials!)
            {
                material.CastShadows = true;
                material.WriteStencil = 1;
            }

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                //.UseSceneModel(false, false)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .AddFloorShadow(4, false)
                .UsePhysics(new PhysicsOptions())
                .ConfigureSampleApp();
        }
    }
}
