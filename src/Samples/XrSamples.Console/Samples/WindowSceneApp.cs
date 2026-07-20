#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.Logging;
using XrEngine;
using XrEngine.OpenGL;
using Silk.NET.Windowing;
using XrMath;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Windows;
using Microsoft.Extensions.DependencyInjection;
using XrSamples.Graffiti;

namespace XrSamples
{
    public class WindowSceneApp
    {

        public static readonly string[] AssetsPath = [
            @"Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Common\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Earth\Assets\",
            @"D:\Development\Personal\Git\XrSDK\src\Samples\XrSamples.Graffiti\Assets\",
            @"D:\Projects\"];

        public static Task Run(IServiceProvider services)
        {
            ModuleManager.Instance.Init();

            Context.Implement<ILogger>(services.GetRequiredService<ILogger<WindowSceneApp>>());

            var builder = new XrEngineAppBuilder();

            var app = builder
                .UsePlatform<ConsolePlatform>()
                .Configure(_ =>
                {
                    Context.Implement<IAssetStore>(MergedAssetStore.FromLocalPaths(AssetsPath));
                })
                .CreateBed()
                .Build()
                .App;

            var view = Window.Create(WindowOptions.Default);
            view.ShouldSwapAutomatically = true;

            var viewRect = new Rect2I();

            var camera = app.ActiveScene!.PerspectiveCamera();

            void UpdateSize()
            {
                viewRect.Width = (uint)view.Size.X;
                viewRect.Height = (uint)view.Size.Y;
                camera.SetFov(45, viewRect.Width, viewRect.Height);
            }

            //UboSsbo1000DrawBenchmark? bench = null;
            OpenGLRender? render = null;

            view.Load += () =>
            {
                UpdateSize();

#if GLES
                var gl = view.CreateOpenGLES();
#else
                var gl = view.CreateOpenGL();
#endif

                var bench = new UboSsbo1000DrawBenchmark(gl, gles: false);
                bench.Init();
                bench.RunAll();

#if GL_WRAPPER
                render = new OpenGLRender(new OpenGLWrapper.GlSwitchWrapper(gl));
#else
                render = new OpenGLRender(gl);
#endif
                render.EnableDebug(RenderEngineDebug.Sync);
                render.AddPass(new GlSimulationPass(render, true), 0);

                app.Renderer = render;

                app.Start();
            };

            view.Resize += x =>
            {
                UpdateSize();
            };

            var isRecorded = false;

            view.Render += t =>
            {
                app.RenderFrame();

                if (!isRecorded)
                {
                    var record = CanvasRecordingReader.ReadFile("D:\\Projects\\XrEditor\\Graffiti\\Recording\\Graffiti-20260608-220511.json");

                    var generator = new CanvasImageGenerator((OpenGLRender)app.Renderer);

                    using var image = generator.Generate(record, 0.001f / 1f);

                    Log.Debug(generator, "Encoding image...");

                    using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    using var outStream = File.OpenWrite("d:\\image.png");
                    data.SaveTo(outStream);

                    Log.Debug(generator, "Image saved");

                    isRecorded = true;
                }
            };

            view.Run();

            return Task.CompletedTask;
        }

    }
}
