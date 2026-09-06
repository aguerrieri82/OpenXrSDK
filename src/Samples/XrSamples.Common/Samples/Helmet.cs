using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Helmet")]
        public static XrEngineAppBuilder CreateHelmet(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Helmet/DamagedHelmet.bin");

            var mesh = GltfLoader.LoadFile(GetAssetPath("Helmet/DamagedHelmet.gltf"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.4f);
            mesh.Transform.SetPositionY(1);
            mesh.AddComponent<BoundsGrabbable>();
            mesh.CastShadows(true);

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .UseEnvironmentMesh(100)
                .UseShadows()
                .ConfigureSampleApp();
        }
    }
}
