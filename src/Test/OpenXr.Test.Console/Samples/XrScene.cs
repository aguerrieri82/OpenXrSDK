using Microsoft.Extensions.Logging;
using OpenXr.Framework.OpenGL;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            xrApp.StartEventLoop();

            xrApp.Start(XrAppStartMode.Render);

            xrApp.BindEngineApp(Common.CreateScene());

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
