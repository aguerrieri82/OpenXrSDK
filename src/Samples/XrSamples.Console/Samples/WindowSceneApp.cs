#if GLES
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenXr.Framework.Angle;
using Silk.NET.Windowing;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Windows;
using XrMath;
using XrSamples.Dnd;
using XrEngine.Components;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using Silk.NET.Maths;
using CefSharp.DevTools.Media;
using Windows.ApplicationModel.Appointments.DataProvider;

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
            XrDevice.IsMetaQuest = false;

            ModuleManager.Instance.Init();

            Context.Implement<ILogger>(services.GetRequiredService<ILogger<WindowSceneApp>>());

            EngineApp? app = null;

            void CreateApp()
            {
                var builder = new XrEngineAppBuilder();

                app = builder
                    .UsePlatform(new ConsolePlatform()
                    {
                        PersistentPath = "D:\\Projects\\XrEditor\\"
                    })
                    //.EnableDebug()
                    .UseOpenGL(opt =>
                    {
                        opt.UseAsyncShaderCompile = false;
                        opt.UseShaderCache = true;
                        opt.SampleCount = 2;
                        opt.UseDefaultIntermediate = true;
                    })
                    .Configure(_ =>
                    {
                        Context.Implement<IAssetStore>(MergedAssetStore.FromLocalPaths(AssetsPath));
                    })
                    .CreateDnd()
                    .Build()
                    .App;
            }
           

            var options = WindowOptions.Default;

            options.Samples = 1;
            //options.WindowState = WindowState.Fullscreen;
            options.VSync = false;
            options.ShouldSwapAutomatically = false;
            options.Size = new Vector2D<int>(1600, 1000);

            if (Context.TryRequire<AngleVulkanContext>(out var angle))
                options.API = GraphicsAPI.None;

            var view = Window.Create(options);

            async void UpdateSize()
            {
                if (app == null)
                    return;

                await EngineApp.MainThread;

                var camera = app.ActiveScene!.PerspectiveCamera();

                var viewRect = new Rect2I
                {
                    Width = (uint)view.FramebufferSize.X,
                    Height = (uint)view.FramebufferSize.Y
                };

                camera.SetFov(45, viewRect.Width, viewRect.Height);
            }

            AutoResetEvent renderReady = new AutoResetEvent(false);

            async void RenderLoop()
            {
                angle?.MakeCurrent();

                angle?.CreateWindowSurface(view.Native!.Win32!.Value.Hwnd);

                view.MakeCurrent();

                CreateApp();

                UpdateSize();
  
                var player = app!.ActiveScene!.ActiveCamera!.AddComponent<TransformPlayer>();
                player.Loop = true;

                app!.Start();

                _ = player.LoadAsync();

                player.SetPlayState(PlayerState.Play);

                var lastEmitTime = DateTime.Now;

                renderReady.Set();

                while (true)
                {
                    if (!view.IsVisible)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    if (view.IsClosing)
                        break;

                    app.RenderFrame();

                    angle?.SwapBuffers();

                    view.SwapBuffers();

                    if ((DateTime.Now - lastEmitTime).TotalSeconds > 1)
                    {
                        Log.Info(typeof(WindowSceneApp), "{0} FPS", app.Stats.Fps);
                        lastEmitTime = DateTime.Now;
                    }
                }
            }

            view.Resize += _ => UpdateSize();

            view.Initialize();

            view.ClearContext();

            var renderThread = new Thread(RenderLoop);
            renderThread.Start();

            renderReady.WaitOne();

            while (!view.IsClosing)
            {
                view.DoEvents();
            }

            return Task.CompletedTask;
        }

    }
}
