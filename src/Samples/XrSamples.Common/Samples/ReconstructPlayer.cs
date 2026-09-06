using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Reconstruct;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreateReconstructPlayer(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            //XrReconstructReader.Current.Open("D:\\Projects\\XrEditor\\Capture");

            var scene = app.ActiveScene!;
            //var player = scene.AddComponent(new XrReconstructPlayer());

            var group = scene.AddChild(new Group3D());

            var snap = group.AddComponent(new DepthCapture(DepthSnapeshotMode.Read)
            {
                SplatMode = false,
                Clip = false,
                GridSize = 320
            });

            //snap.Load("D:\\Projects\\XrEditor\\DepthSnapshots\\20260619_094000_765");
            snap.Load("D:\\Projects\\XrEditor\\DepthSnapshots\\20260619_080632_705");

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/Neutral.hdr")
                .ConfigureSampleApp();
        }
    }
}
