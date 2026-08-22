using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Sponza")]
        public static XrEngineAppBuilder CreateSponza(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Sponza/Sponza.bin");

            var mesh = (Group3D)GltfLoader.LoadFile(GetAssetPath("Sponza/Sponza.gltf"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .ConfigureSampleApp();

        }
    }
}
