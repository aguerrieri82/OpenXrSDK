using DrumsVR.Game;
using XrEngine;
using XrEngine.AI;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Drums")]
        public static XrEngineAppBuilder CreateDrums(this XrEngineAppBuilder builder)
        {
            builder.Configure(DrumsVRApp.Build)
                .UseRayCollider("Mouse")
                .AddPassthrough()

            .ConfigureApp(app =>
            {
                var drumApp = (DrumsVRApp)app.App;
                var scene = (MainScene)app.App.ActiveScene!;
                scene.Id = Guid.Parse("5ae3f2c6-ae6b-4c57-a885-26dc8fc9fa89");

                scene.AddComponent<DebugGizmos>();
                scene.AddComponent<XrInputRecorder>();
                scene.AddComponent(new XrInputPlayer(new AIPosePredictor("d:\\pose_prediction_model")));
                scene.AddChild(new PlaneGrid(6f, 12f, 2f));
            });

            return builder;
        }
    }
}
