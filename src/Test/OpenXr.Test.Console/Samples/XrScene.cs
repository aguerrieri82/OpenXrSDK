using Microsoft.Extensions.Logging;
using OpenXr.Framework.OpenGL;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenXr.Framework.Oculus;

namespace OpenXr.Samples
{
    public static class XrSceneApp
    {
        public static Task Run(IServiceProvider services, ILogger logger)
        {

            using var xrApp = new XrApp(logger,
                    new XrOpenGLGraphicDriver(new OpenGLDevice()),
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
