using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using Xr.Engine;
using Xr.Engine.OpenXr;

namespace OpenXr.Samples
{
    public static class XrSceneApp
    {
        private static XrOculusTouchController? _inputs;
        private static EngineApp? _game;

        public static Task Run(IServiceProvider services, ILogger logger)
        {
            bool isStarted = true;

            var options = new OculusXrPluginOptions
            {
                EnableMultiView = false,
                SampleCount = 4,
                ResolutionScale = 1f
            };

            var viewManager = new ViewManager();
            viewManager.Initialize();

            using var xrApp = new XrApp(logger,
                    new XrOpenGLGraphicDriver(viewManager.View),
                    new OculusXrPlugin(options));

            _inputs = xrApp.WithInteractionProfile<XrOculusTouchController>(bld => bld
               .AddAction(a => a.Right!.Button!.AClick)
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.AimPose)
            .AddAction(a => a.Right!.TriggerClick));

            _game = SampleScenes.CreateDefaultScene(new LocalAssetManager("Assets"));

            _game.ActiveScene!.AddComponent(new RayCollider(_inputs.Right!.AimPose!));


            xrApp.BindEngineAppGL(_game, options.SampleCount, options.EnableMultiView);

            xrApp.StartEventLoop(() => !isStarted);

            xrApp.Start(XrAppStartMode.Render);

            while (true)
            {
                xrApp.RenderFrame(xrApp.Stage);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                    {
                        isStarted = false;
                        break;
                    }

                }
            }

            xrApp.Stop();

            return Task.CompletedTask;
        }
    }
}
