using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Portal")]
        public static XrEngineAppBuilder CreatePortal(this XrEngineAppBuilder builder)
        {
            var settings = new PortalSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "portal_settings.json"));

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var options = new TextureLoadOptions() { IsSrgb = true };

            var left = AssetLoader.Instance.Load<Texture2D>("res://asset/Fish/cam_left.jpg", options);
            var right = AssetLoader.Instance.Load<Texture2D>("res://asset/Fish/cam_right.jpg", options);
            var cube = AssetLoader.Instance.Load<Texture2D>("res://asset/Fish/cube_orig.jpg", options);
            var stereo = AssetLoader.Instance.Load<Texture2D>("res://asset/Fish/stereo.jpg", options);

            var mat = new FishReflectionSphereMaterial(left, right)
            {
                SphereRadius = 6,
                SphereCenter = new Vector3(0, 1.5f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend,
                Mode = FishReflectionMode.Eye
            };

            var mesh = new TriangleMesh(new Quad3D(), mat);
            mesh.Name = "mesh";
            scene.AddChild(mesh);
            /*


            var mesh2 = new TriangleMesh(new FishEyeHemisphere(), new TextureMaterial
            {
                Texture = left,
                CullFront = true
            });

            scene.AddChild(mesh2);
            */

            return builder
                .UseApp(app)
                .ConfigureSampleApp()
                //.AddPanel(new PortalSettingsPanel(settings, scene))
                .UseClickMoveFront(mesh)
                .ConfigureApp(e =>
                {
                    var oculus = e.XrApp.Plugin<OculusXrPlugin>();
                    var isLoading = false;
                    var lastUpdate = new DateTime();
                    mesh.AddBehavior(async (_, _) =>
                    {
                        if (!e.XrApp.IsStarted || isLoading || ((DateTime.UtcNow - lastUpdate).TotalSeconds < 1000))
                            return;

                        isLoading = true;
                        try
                        {
                            var anchors = await e.XrApp.Plugin<OculusXrPlugin>().GetAnchorsAsync(new XrAnchorFilter
                            {
                                Components = XrAnchorComponent.All
                            });

                            var window = anchors.FirstOrDefault(a => a.Labels != null && a.Labels.Contains("WINDOW_FRAME"));

                            if (window != null)
                            {
                                if (window.Pose != null)
                                {
                                    var pos = window.Pose.Value.Position;

                                    /*
                                    pos.X += 0.16f;
                                    pos.Z += 0.05f;
                                    pos.Y -= 0.05f;
                                    */
                                    mesh.Transform.Position = pos;
                                    mesh.Transform.Orientation = window.Pose.Value.Orientation;

                                    var mat = ((FishReflectionSphereMaterial)mesh.Materials[0])!;
                                    mat.SphereCenter = new Vector3(mesh.Transform.Position.X, 1.5f, mesh.Transform.Position.Z);
                                }

                                if (window.Bounds2D != null)
                                {
                                    mesh.Transform.Scale = new Vector3(window.Bounds2D.Value.Width, window.Bounds2D.Value.Height, 0.01f);
                                }
                            }

                        }
                        finally
                        {
                            isLoading = false;
                            lastUpdate = DateTime.UtcNow;
                        }

                    });
                });
        }
    }
}
