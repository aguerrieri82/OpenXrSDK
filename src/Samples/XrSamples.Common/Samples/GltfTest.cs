using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Iridescent")]
        public static XrEngineAppBuilder CreateGltfTest(this XrEngineAppBuilder builder)
        {
            return CreateGltfTest(builder, "Models/IridescentDishWithOlives.glb");
        }

        public static XrEngineAppBuilder CreateGltfTest(this XrEngineAppBuilder builder, string assetPath)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath(assetPath), GltfOptions, GetAssetPath);

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .ConfigureSampleApp();
        }
    }
}
