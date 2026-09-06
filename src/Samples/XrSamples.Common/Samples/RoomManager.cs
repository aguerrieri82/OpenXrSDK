using CanvasUI;
using RoomDesigner.Game;
using XrEngine;
using XrEngine.Components;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Room Manager")]
        public static XrEngineAppBuilder CreateRoomManager(this XrEngineAppBuilder builder)
        {
            builder.Configure(RoomDesignerApp.Build)
                .UseRayCollider("Mouse")
                .AddFloorShadow(4, false)
                .AddPassthrough()

            .ConfigureApp(e =>
            {
                var scene = (RoomScene)e.App.ActiveScene!;

                scene.AddChild<EnvironmentView>();
                scene.AddComponent<ShadowController>();
                scene.AddComponent<ResolveController>();
                scene.Id = Guid.Parse("5ae3f2c6-ae6b-4c57-a885-26dc8fc9fa89");

                scene.AddComponent<DebugGizmos>();
                scene.AddComponent<XrInputRecorder>();
                scene.AddComponent<XrInputPlayer>();
                scene.AddChild(new PlaneGrid(6f, 12f, 2f));
            });

            return builder;
        }
    }
}
