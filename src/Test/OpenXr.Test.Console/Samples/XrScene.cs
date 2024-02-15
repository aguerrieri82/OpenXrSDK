using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using OpenXr.Framework.OpenGLES;
using OpenXr.Test;
using System.Xml.Linq;
using Xr.Engine.OpenXr;

namespace OpenXr.Samples
{
    public static class XrSceneApp
    {
        private static XrMetaQuestTouchPro? _inputs;
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

            _inputs = xrApp.WithInteractionProfile<XrMetaQuestTouchPro>(bld => bld
               .AddAction(a => a.Right!.Button!.AClick)
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.AimPose)
            .AddAction(a => a.Right!.TriggerClick));

            _game = Common.CreateScene(LocalAssetManager.Instance);

            _game.ActiveScene!.AddComponent(new RayCollider(_inputs.Right!.AimPose!));

            xrApp.BindEngineApp(_game, options.SampleCount, options.EnableMultiView);

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
