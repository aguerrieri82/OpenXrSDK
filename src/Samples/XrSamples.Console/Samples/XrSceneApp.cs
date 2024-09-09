﻿using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Services;

namespace XrSamples
{

    public static class XrSceneApp
    {
        private static XrOculusTouchController? _inputs;
        private static XrEngineApp? _game;

        public static Task Run(IServiceProvider services)
        {
            ModuleManager.Instance.Init();

            var builder = new XrEngineAppBuilder();
            _game = builder
                .UseOpenGL()
                .UsePlatform<ConsolePlatform>()
                .CreateChromeBrowser()
                .Build();

            _game.App.Start();

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