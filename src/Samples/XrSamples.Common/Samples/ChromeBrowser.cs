#if !__ANDROID__

using XrEngine.Browser.Windows;
using System.Numerics;
using XrEngine;

#endif

using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreateChromeBrowser(this XrEngineAppBuilder builder)
        {
#if !__ANDROID__
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var display = new TriangleMesh(Quad3D.Default)
            {
                Name = "display"
            };

            display.Transform.Scale = new Vector3(1.6f, 1.2f, 0.01f);

            display.AddComponent<MeshCollider>();
            display.AddComponent<SurfaceController>();
            display.AddComponent(new ChromeWebBrowserView
            {
                ZoomLevel = 0,
                Source = "www.youtube.com",
            });

            scene.AddChild(display);

            return builder.UseApp(app)
              .ConfigureSampleApp()
              .UseClickMoveFront(display, 0.5f);
#else
            return builder;
#endif

        }
    }
}
