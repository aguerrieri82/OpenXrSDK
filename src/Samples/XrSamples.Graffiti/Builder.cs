using OpenXr.Framework.Oculus;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Graffiti
{
    public static class Builder
    {
        [Sample("Graffiti")]
        public static XrEngineAppBuilder CreateGraffiti(this XrEngineAppBuilder builder, bool reconstructMode = false)
        {
            var halfAngle = MathF.PI / 20f;
            var bottomRadius = 0.01f;
            var distance = 0.8f;
            var baseDensity = 1f;

            var topRadius = bottomRadius + distance * MathF.Sin(halfAngle);

            var area1 = MathF.PI * bottomRadius * bottomRadius;
            var area2 = MathF.PI * topRadius * topRadius;

            var ratio = area1 / area2;

            var newDensity = baseDensity * ratio;

            var coneDensityK = MathF.Sin(halfAngle) / bottomRadius;
            var x = 1.0f + distance * coneDensityK;
            var ratio2 = 1.0f / (x * x);


            Embedded.Register(typeof(Builder).Assembly);

            var app = new EngineApp();

            var scene = new MainScene(reconstructMode);

            app.OpenScene(scene);

            return builder.UseApp(app)
                    //.AddPanel(new DndSettingsPanel(scene.Settings, scene))
                    .UseEnvironmentHDR("res://asset/Envs/StudioTomoco.hdr", false)
                    .ConfigureApp(scene.Configure)
                    .UseRightController()
                    .UseInputs<XrOculusTouchController>(a => a
                        .AddAction(b => b.Right!.Haptic)
                        .AddAction(b => b.Right!.Thumbstick)
                        .AddAction(b => b.Right!.ThumbstickClick))
                    .AddPassthrough();
            //.UseTeleport(ControllerHand.Left, scene.Player);
        }
    }
}
