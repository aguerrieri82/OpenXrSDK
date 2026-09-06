using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Panorama Maker")]
        public static XrEngineAppBuilder CreatePanoramaMaker(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var maker = scene.AddComponent<PanoramaMaker>();

            scene.AddChild(new CubeView(maker.CubeTexture));

            var display = new TriangleMesh(Quad3D.Default, new TextureMaterial(maker.CameraTexture));
            display.Name = "camera_preview";
            display.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);

            scene.AddChild(display);

            return builder
                .UseApp(app)
                .UseClickMoveFront(display, 0.5f)
                .ConfigureApp(cfg =>
                {
                    maker.Configure(cfg.Inputs!);
                })
                .ConfigureSampleApp();
        }
    }
}
