#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.Logging;
using XrEngine;
using XrEngine.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using XrMath;
using XrEngine.OpenXr;
using OpenXr.Framework;
using XrEngine.Services;



namespace XrSamples
{
    public static class WindowSceneApp
    {
        public static Task Run(IServiceProvider services)
        {
            ModuleManager.Instance.Init();

            var builder = new XrEngineAppBuilder();

            var app = builder
                .UsePlatform<ConsolePlatform>()
                .CreateChromeBrowser()
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
                app.RenderFrame(viewRect);
            };

            view.Run();

            return Task.CompletedTask;
        }

     
    }
}
