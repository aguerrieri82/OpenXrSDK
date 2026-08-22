using System.Numerics;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using System.Xml.Linq;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreateController(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("Models/MetaQuestTouchPlus_Right.glb"), GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetPositionY(1);
            mesh.AddComponent<BoundsGrabbable>();

            scene.AddChild(mesh);

            scene.PerspectiveCamera.Target = mesh.Transform.Position;
            scene.PerspectiveCamera.Transform.Position = new Vector3(0.2f, 1.4f, 0.2f);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
