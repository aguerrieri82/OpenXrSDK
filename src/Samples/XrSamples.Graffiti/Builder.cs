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

            Embedded.Register(typeof(Builder).Assembly);

            Context.Implement<GraffitiStore>();

            var app = new EngineApp();

            var scene = new MainScene(reconstructMode);

            app.OpenScene(scene);

            if (!reconstructMode)
                builder.UseEnvironmentHDR("res://asset/Envs/StudioTomoco.hdr", false);

            builder.UseApp(app)
                  .UseRightController()
                  .UseLeftController()
                  .UseInputs<XrOculusTouchController>(a => a
                      .AddAction(b => b.Right.Haptic)
                      .AddAction(b => b.Right.Thumbstick)
                      .AddAction(b => b.Right.ThumbstickClick))
                  .AddPassthrough()
                  .UseRayCollider()
                  .AddRightPointer()
                  .AddXrRoot()
                  .AddWebUI(new WebUIOptions
                  {
                      Bridges = [new Bridge()],
                      DevServer = "https://localhost:4541/",
                      DisableMoveForward = true
                  })
                  .ConfigureApp(scene.Configure);

            return builder;
        }
    }
}
