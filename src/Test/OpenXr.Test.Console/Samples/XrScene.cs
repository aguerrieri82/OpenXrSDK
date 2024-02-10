using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGLES;

namespace OpenXr.Samples
{
    public static class XrSceneApp
    {
        public static Task Run(IServiceProvider services, ILogger logger)
        {
            var viewManager = new ViewManager();
            viewManager.Initialize();

            using var xrApp = new XrApp(logger,
                    new XrOpenGLESGraphicDriver(viewManager.View),
                    new OculusXrPlugin());


            xrApp.BindEngineApp(Common.CreateScene());

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
