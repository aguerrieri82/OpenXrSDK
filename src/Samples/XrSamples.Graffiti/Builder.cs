using System.Runtime.InteropServices;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public static class Builder
    {
        [Sample("Graffiti")]
        public static XrEngineAppBuilder CreateGraffiti(this XrEngineAppBuilder builder)
        {
            var app = new EngineApp();

            var scene = new MainScene();

            app.OpenScene(scene);


            return builder.UseApp(app)
                    //.AddPanel(new DndSettingsPanel(scene.Settings, scene))
                    .UseEnvironmentHDR("res://asset/Envs/StudioTomoco.hdr", false)
                    .ConfigureApp(scene.Configure)
                    .ConfigureSampleApp()
                    .AddPassthrough();
                    //.UseTeleport(ControllerHand.Left, scene.Player);
        }
    }
}
