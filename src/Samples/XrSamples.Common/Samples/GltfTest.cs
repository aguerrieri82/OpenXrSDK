using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreateGltfTest(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("Models/DragonAttenuation.glb"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/StudioTomoco.hdr")
                .ConfigureSampleApp();
        }
    }
}
