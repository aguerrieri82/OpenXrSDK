using XrEngine;
using XrEngine.OpenXr;
using XrEngine.Reconstruct;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Depth Snapeshot")]
        public static XrEngineAppBuilder CreateDepthSnapeshot(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var group = scene.AddChild(new Group3D());
            group.Name = "Depth Frames";
            group.IsVisible = false;

            var mode = DepthSnapeshotMode.Read;

            var snapeshot = group.AddComponent(new DepthCapture(mode)
            {
                SplatMode = false,
                GridSize = 320,
                UseDepthOcclusion = true,
                Optimize = true,
                ComputeIndices = true,
                UseMeshCache = true
            });

            if (mode == DepthSnapeshotMode.Read)
            {
                var path = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots", "20260619_094000_765");
                //var path = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots", "20260619_080632_705");
                snapeshot.Load(path);
            }

            return builder
                .UseApp(app)
                .UseEnvironmentDepth()
                .UseDefaultHDR()
                .UseFloorTeleport(scene)
                .ConfigureSampleApp(false)
                .ConfigureApp(a =>
                {

                    snapeshot.ConfigureInput(a.Inputs!);

                    if (mode != DepthSnapeshotMode.Read)
                        return;

                    Task.Run(async () =>
                    {
                        await EngineApp.RenderThread;

                        //await snapeshot.GenerateMeshAsync();
                    });

                    group.AddBehavior((_, ctx) =>
                    {
                        var thumb = a.Inputs!.Right!.Thumbstick;
                        if (thumb!.IsActive)
                        {
                            var val = thumb.Value.X;
                            if (Math.Abs(val) > 0.4f)
                            {
                                snapeshot.Mesh?.Transform.SetPositionY(snapeshot.Mesh.Transform.Position.Y + val * 0.5f * (float)ctx.DeltaTime);
                            }
                        }
                    });
                });
        }
    }
}
