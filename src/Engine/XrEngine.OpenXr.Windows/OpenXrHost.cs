using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrEngine.OpenGL;
using XrEngine.Services;

namespace XrEngine.OpenXr.Windows
{
    public class OpenXrHost
    {
        public static void Start(string[] args, Action<XrEngineAppBuilder> build)
        {
            Gpu.EnableNvAPi();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging((ctx, logging) =>
                {
                    logging.AddConfiguration(ctx.Configuration)
                           .AddOneLineConsole();
                })
                .ConfigureServices((ctx, services) =>
                {
                    var envName = ctx.HostingEnvironment.EnvironmentName;

                })
                .Build();

            _ = host.RunAsync();

            Context.Implement<ILogger>(host.Services.GetRequiredService<ILogger<OpenXrHost>>());

            ModuleManager.Instance.Init();

            var builder = new XrEngineAppBuilder();
            build(builder);

            var engineApp = builder.Build();

            if (engineApp.App.Renderer is OpenGLRender openGL)
                openGL.EnableDebug();

            engineApp.App.Start();

            engineApp.XrApp.Start(XrAppStartMode.Render);

            while (engineApp.XrApp.State != XrAppState.Disposed)
            {
                engineApp.XrApp.RenderFrame(engineApp.XrApp.Stage);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                        break;
                }
            }

            engineApp.XrApp.Stop();

        }
    }
}
