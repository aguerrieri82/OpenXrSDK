using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Iridescent")]
        public static XrEngineAppBuilder CreateGltfIridescent(this XrEngineAppBuilder builder)
        {
            return CreateGltfTest(builder, "Models/IridescentDishWithOlives.glb")
                  .UseCameraRefraction();
        }

        [Sample("Dragon")]
        public static XrEngineAppBuilder CreateGltfDragon(this XrEngineAppBuilder builder)
        {
            return CreateGltfTest(builder, "Models/DragonAttenuation.glb")
                  .UseCameraRefraction(true);
        }

        public static XrEngineAppBuilder CreateGltfTest(this XrEngineAppBuilder builder, string assetPath)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GltfOptions.TransmissionBkOnly = false;

            var mesh = GltfLoader.LoadFile(GetAssetPath(assetPath), GltfOptions, GetAssetPath);

            scene.AddChild(mesh);

#if __ANDROID__

            if (mesh is Group3D group)
                group.FindByName<Object3D>("Cloth Backdrop")?.Remove();
 #endif

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/aerodynamics_workshop_4k.hdr")
                .UseClickMoveFront(mesh, 0.5f)
                .ConfigureSampleApp();
        }
    }
}
