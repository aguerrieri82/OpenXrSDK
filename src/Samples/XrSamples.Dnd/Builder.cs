
using XrEngine;
using XrEngine.OpenXr;


namespace XrSamples.Dnd
{
    public static class Builder
    {
        [Sample("DnD")]
        public static XrEngineAppBuilder CreateDnd(this XrEngineAppBuilder builder)
        {
            var app = new EngineApp();

            var scene = new DndScene();

            var map = scene.LoadMap("Dnd/tavern");
            scene.AddToken("#6265");

            app.OpenScene(scene);

            return builder.UseApp(app)
                    .AddPanel(new DndSettingsPanel(scene.Settings, scene))
                    .UseDefaultHDR()
                    .ConfigureApp(scene.InputController.Configure)
                    .ConfigureSampleApp()
                    .UseTeleport(ControllerHand.Left, scene.Player);
        }
    }
}
