using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Windows;
using XrEngine.Services;

namespace XrSamples
{

    public static class XrSceneApp
    {

        private static XrEngineApp? _game;

        public static Task Run(IServiceProvider services)
        {
            ModuleManager.Instance.Init();

            Context.Implement<ILogger>(services.GetRequiredService<ILogger<WindowSceneApp>>());

            var builder = new XrEngineAppBuilder();
            _game = builder
                .UseOpenGL()
                .UsePlatform<ConsolePlatform>()
                .CreateRoomManager()
                .Build();

            _game.App.Start();

            _game.XrApp.Start(XrAppStartMode.Render);

            while (true)
            {
                _game.XrApp.RenderFrame(_game.XrApp.ReferenceSpace);

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if (key.Key == ConsoleKey.Enter)
                        break;
                }
            }

            _game.XrApp.Stop();

            return Task.CompletedTask;
        }
    }
}
