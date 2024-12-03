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
using XrEngine.Services;
using XrEngine.OpenXr.Windows;
using Microsoft.Extensions.DependencyInjection;



namespace XrSamples
{
    public class WindowSceneApp
    {
        public static Task Run(IServiceProvider services)
        {
            ModuleManager.Instance.Init();

            Context.Implement<ILogger>(services.GetRequiredService<ILogger<WindowSceneApp>>());

            var builder = new XrEngineAppBuilder();

            var app = builder
                .UsePlatform<ConsolePlatform>()
                .CreatePingPong()
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

            view.Load += () =>
            {
                UpdateSize();

#if GLES
                var gl = view.CreateOpenGLES();
#else
                var gl = view.CreateOpenGL();
#endif

                var render = new OpenGLRender(gl);
                render.EnableDebug();

                app.Renderer = render;

                app.Start();
            };

            view.Resize += x =>
            {
                UpdateSize();
            };

            view.Render += t =>
            {
                app.RenderFrame();
            };

            view.Run();

            return Task.CompletedTask;
        }


    }
}
