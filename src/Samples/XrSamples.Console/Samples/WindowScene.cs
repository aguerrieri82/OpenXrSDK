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
using Xr.Test;
using XrEngine.OpenXr;
using OpenXr.Framework;
using Microsoft.Extensions.Logging.Abstractions;



namespace XrSamples
{
    public static class WindowSceneApp
    {
        class ConsolePlatform : IXrEnginePlatform
        {
            public string PersistentPath => Path.GetFullPath("Data");

            public IAssetManager AssetManager { get; } = new LocalAssetManager("Assets");

            public ILogger Logger { get; } = NullLogger.Instance;

            public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
            {
                throw new NotSupportedException();
            }

            public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
            {
                throw new NotSupportedException();
            }
        }

        public static Task Run(IServiceProvider services, ILogger logger)
        {
            XrPlatform.Current = new ConsolePlatform();

            var builder = new XrEngineAppBuilder();

            var app = builder.CreateChess().Build().App;

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
