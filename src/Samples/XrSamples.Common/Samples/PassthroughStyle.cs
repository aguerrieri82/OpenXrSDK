using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Passthrough Style")]
        public static XrEngineAppBuilder CreatePassthroughStyle(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var pt = scene.AddComponent<PassthroughStyle>();

            var lut = new ColorLut(32)
                .Exposure(-0.5f)
                .Contrast(1.25f)
                .Saturation(0.8f)
                .Temperature(-0.2f)
                .Tint(-0.05f)
                .Lift(new Vector3(-0.02f, 0.015f, 0.03f))
                .Gamma(new Vector3(1.02f, 0.98f, 0.92f))
                .Gain(new Vector3(0.95f, 1.03f, 1.12f))
                .HueDegrees(4);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .AddPassthrough(asLayer: true)
                .UseOculus(opt =>
                {
                    opt.UseBodyTrack = false;
                    opt.UseBothHandAndControllers = false;
                })
                .ConfigureSampleApp(useHands: false)
                .ConfigureApp(e =>
                {
                    var click = e.Inputs!.Right.Button.AClick;

                    scene.AddBehavior((_, _) =>
                    {
                        if (click.IsChanged && click.Value)
                        {
                            if (pt.Lut == null)
                                pt.Lut = lut;
                            else
                                pt.Lut = null;
                        }
                    });

                });
        }
    }
}

