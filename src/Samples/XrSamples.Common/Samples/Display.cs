using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Display")]
        public static XrEngineAppBuilder CreateDisplay(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var display = new TriangleMesh(Quad3D.Default);
            //display.Materials.Add(new StandardMaterial { Color = Color.White, DoubleSided = false, WriteDepth = false });

            display.Name = "display";

            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);

            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            return builder.UseApp(app)
                          .UseClickMoveFront(display, 0.5f)
                          .ConfigureSampleApp();
        }
    }
}
