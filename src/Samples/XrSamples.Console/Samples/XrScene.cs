using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{

    public static class XrSceneApp
    {
        private static XrOculusTouchController? _inputs;
        private static XrEngineApp? _game;

        public static Task Run(IServiceProvider services)
        {
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets"));
            Context.Implement<IProgressLogger>(new NullProgressLogger());

            var builder = new XrEngineAppBuilder();
            _game = builder
                .UseOpenGL()
                .UsePlatform<ConsolePlatform>()
                .CreateSponza()
                .Build();

            _game.XrApp.Start(XrAppStartMode.Render);

            while (true)
            {
                _game.XrApp.RenderFrame(_game.XrApp.Stage);

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
