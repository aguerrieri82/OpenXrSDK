using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static class XrSceneApp
    {
        private static XrOculusTouchController? _inputs;
        private static EngineApp? _game;

        public static Task Run(IServiceProvider services, ILogger logger)
        {
            var viewManager = new ViewManager();
            viewManager.Initialize();


            using var xrApp = new XrApp(logger,
                    new XrOpenGLGraphicDriver(viewManager.View),
                    new OculusXrPlugin());

            xrApp.RenderOptions.SampleCount = 1;

            _inputs = xrApp.WithInteractionProfile<XrOculusTouchController>(bld => bld
               .AddAction(a => a.Right!.Button!.AClick)
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.AimPose)
               .AddAction(a => a.Right!.TriggerClick));

            _game = new EngineApp(); //TODO Fix this
            //_game = SampleScenes.CreateDefaultScene();

            _game.ActiveScene!.AddComponent(new RayCollider(_inputs.Right!.AimPose!));

            xrApp.BindEngineAppGL(_game);

            xrApp.Start(XrAppStartMode.Render);

            while (true)
            {
                xrApp.RenderFrame(xrApp.Stage);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                        break;
                }
            }

            xrApp.Stop();

            return Task.CompletedTask;
        }
    }
}
