#if GLES
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenXr.Framework.Angle;
using Silk.NET.Windowing;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Windows;
using XrMath;
using XrSamples.Dnd;
using XrEngine.Components;
using OpenXr.Framework;
using Silk.NET.Maths;
using System.Diagnostics;

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
            if (!EngineNativeLib.RdcIsAttached())
            {
                using var profiles = new NvidiaProfiles();

                profiles.DisableOpenGlThreadedOptimization();
                profiles.SetOpenGlPresentMethod(NvidiaProfiles.OpenGlPresentMethod.Native);
                profiles.SetVerticalSyncMode(NvidiaProfiles.VerticalSyncMode.ForceOff);
            }

            XrDevice.IsMetaQuest = false;

            ModuleManager.Instance.Init();

            Context.Implement<ILogger>(services.GetRequiredService<ILogger<WindowSceneApp>>());

            EngineApp? app = null;

            AngleVulkanContext? angle = null;

            bool useAngle = false;

            void CreateApp()
            {
                var builder = new XrEngineAppBuilder();

                if (useAngle)
                    builder.UseAngle();
                else
                    builder.UseOpenGL();

                app = builder
                    .UsePlatform(new ConsolePlatform()
                    {
                        PersistentPath = "D:\\Projects\\XrEditor\\",
                        SharedPath = "D:\\Projects\\XrEditor\\Storage\\",
                    })

                    .SetGlOptions(opt =>
                    {
                        opt.UseAsyncShaderCompile = false;
                        opt.UseShaderCache = true;
                        opt.SampleCount = 2;
                        opt.UseDefaultIntermediate = true;
                        opt.UseDepthPass = false;
                    })
                    .Configure(_ =>
                    {
                        Context.Implement<IAssetStore>(MergedAssetStore.FromLocalPaths(AssetsPath));
                    })
                    .SetRenderQuality(1f, 1)
                    .CreateGltfTest()
                    .Build()
                    .App;

                if (useAngle)
                    Context.TryRequire(out angle);
            }


            var options = WindowOptions.Default;

            options.Samples = 1;
            //options.WindowState = WindowState.Fullscreen;
            options.Size = new Vector2D<int>(1600, 1000);
            if (useAngle)
                options.API = GraphicsAPI.None;

            var view = Window.Create(options);

            async void UpdateSize()
            {
                if (app == null)
                    return;

                await EngineApp.MainThread;

                var camera = app.ActiveScene!.PerspectiveCamera;

                var viewRect = new Rect2I
                {
                    Width = (uint)view.FramebufferSize.X,
                    Height = (uint)view.FramebufferSize.Y
                };

                camera.SetFov(45, viewRect.Width, viewRect.Height);
            }

            async void RenderLoop()
            {
                CreateApp();

                if (useAngle)
                {
                    Debug.Assert(angle != null);

                    angle.CreateWindowSurface(view.Native!.Win32!.Value.Hwnd);

                    angle.SetSwapInterval(0);
                }

                UpdateSize();

                var player = app!.ActiveScene!.ActiveCamera!.AddComponent<TransformPlayer>();
                player.Loop = true;

                app!.Start();

                _ = player.LoadAsync();

                player.SetPlayState(PlayerState.Play);

                var lastEmitTime = DateTime.Now;

                while (true)
                {
                    if (!useAngle && !view.GLContext!.IsCurrent)
                    {
                        view.MakeCurrent();
                        view.GLContext!.SwapInterval(0);
                    }

                    if (!view.IsVisible)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    if (view.IsClosing)
                        break;

                    app.RenderFrame();

                    if (angle != null)
                        angle.SwapBuffers();
                    else
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

            var renderThread = new Thread(RenderLoop);
            renderThread.Start();

            while (!view.IsClosing)
            {
                Debug.Assert(useAngle && view.GLContext == null);

                if (!useAngle && view.GLContext!.IsCurrent)
                    view.ClearContext();

                view.DoEvents();
            }

            return Task.CompletedTask;
        }

    }
}
