using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;

namespace XrSamples
{
    public class SceneAnchors
    {
        public static async Task Run(IServiceProvider services, ILogger logger)
        {
            var viewManager = new ViewManager();
            viewManager.Initialize();


            var xrOculus = new OculusXrPlugin();

            var app = new XrApp(services!.GetRequiredService<ILogger<XrApp>>(),
                      new XrOpenGLGraphicDriver(viewManager.View),
                xrOculus);


            while (true)
            {

                app.Start(XrAppStartMode.Query);

                var res = await xrOculus.GetAnchorsAsync(new XrAnchorFilter
                {
                    Components = XrAnchorComponent.All
                });

                app.Stop();

                if (Console.ReadKey().Key == ConsoleKey.Enter)
                    break;
            }

            app.Dispose();
        }
    }
}
