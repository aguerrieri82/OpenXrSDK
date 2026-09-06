using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Snapeshot View")]
        public static XrEngineAppBuilder CreateDepthSnapeshotView(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var path = Path.Combine(XrPlatform.Current!.SharedPath, "DepthSnapshots", "20260619_094000_765");

            var mesh = AssetLoader.Instance.Load<TriangleMesh>(Path.Combine(path, "reconstruct_final.obj"));

            var tex = AssetLoader.Instance.Load<Texture2D>(Path.Combine(path, "reconstruct_final.jpg"), new TextureLoadOptions
            {
                IsSrgb = true
            });

            mesh.Materials.Add(new TextureMaterial(tex));

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseFloorTeleport(scene)
                .ConfigureSampleApp(false)
                .ConfigureApp(a =>
                {
                    scene.AddBehavior((_, ctx) =>
                    {
                        var thumb = a.Inputs!.Right!.Thumbstick;
                        if (thumb!.IsActive)
                        {
                            var val = thumb.Value.X;
                            if (Math.Abs(val) > 0.4f)
                                mesh.Transform.SetPositionY(mesh.Transform.Position.Y + val * 0.5f * (float)ctx.DeltaTime);
                        }
                    });
                });
        }
    }
}
