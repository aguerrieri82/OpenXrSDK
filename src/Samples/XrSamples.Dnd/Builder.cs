
using XrEngine;
using XrEngine.OpenXr;


namespace XrSamples.Dnd
{
    public static class Builder
    {
        [Sample("DnD")]
        public static XrEngineAppBuilder CreateDnd(this XrEngineAppBuilder builder)
        {
            EngineApp app = new EngineApp();

            DndScene scene = new DndScene();

            Group3D map = scene.LoadMap("Dnd/tavern");

            scene.LoadAsync("65718833435872349").Wait();

            scene.AddToken("#6265", Guid.Parse("7fddd04d-39df-4b3b-8615-276ae3af2662"));

            app.OpenScene(scene);

            scene.Settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "dnd_settings.json"));
            map.WorldMatrix = scene.Settings.MapTransform;

            return builder.UseApp(app)
                    .AddPanel(new DndSettingsPanel(scene.Settings, scene))
                    .UseDefaultHDR()
                    .ConfigureApp(scene.InputController.Configure)
                    .ConfigureSampleApp()
                    .UseTeleport(ControllerHand.Left, scene.Player);
        }
    }
}
