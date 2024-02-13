using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenXr.Engine;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGLES;
using OpenXr.Test;

namespace OpenXr.Samples
{
    public static class XrSceneApp
    {
        private static XrMetaQuestTouchPro? _inputs;

        public static Task Run(IServiceProvider services, ILogger logger)
        {
            var viewManager = new ViewManager();
            viewManager.Initialize();

            using var xrApp = new XrApp(logger,
                    new XrOpenGLESGraphicDriver(viewManager.View),
                    new OculusXrPlugin());

            _inputs = xrApp.WithInteractionProfile<XrMetaQuestTouchPro>(bld => bld
               .AddAction(a => a.Right!.GripPose)
               .AddAction(a => a.Right!.GripAim)
               .AddAction(a => a.Right!.Button!.AClick)
               .AddAction(a => a.Right!.TriggerClick));



            xrApp.BindEngineApp(Common.CreateScene(LocalAssetManager.Instance));

            xrApp.StartEventLoop();

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
