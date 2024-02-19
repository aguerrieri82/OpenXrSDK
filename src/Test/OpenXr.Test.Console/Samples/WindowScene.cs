#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.Logging;
using OpenXr.Engine;
using OpenXr.Engine.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;



namespace OpenXr.Samples
{
    public static class WindowSceneApp
    {
        public static Task Run(IServiceProvider services, ILogger logger)
        {
            var app = SampleScenes.CreateDefaultScene(new LocalAssetManager("Assets"));

            var view = Window.Create(WindowOptions.Default);
            view.ShouldSwapAutomatically = true;

            var viewRect = new Rect2I();

            var camera = (app.ActiveScene?.ActiveCamera as PerspectiveCamera)!;

            void UpdateSize()
            {
                viewRect.Width = (uint)view.Size.X;
                viewRect.Height = (uint)view.Size.Y;
                camera.SetFov(45, viewRect.Width, viewRect.Height);
            }

            view.Load += () =>
            {
                UpdateSize();

                camera!.LookAt(new Vector3(0f, 2f, 2f), Vector3.Zero, new Vector3(0, 1, 0));

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

        private static void View_Resize(Silk.NET.Maths.Vector2D<int> obj)
        {
            throw new NotImplementedException();
        }
    }
}
