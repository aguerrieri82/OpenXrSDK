#if __ANDROID__
using XrEngine.Devices.Android;
#endif

using CanvasUI;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Portal Video")]
        public static XrEngineAppBuilder CreatePortalVideo(this XrEngineAppBuilder builder)
        {
            var settings = new PortalSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "portal_settings.json"));

            var size = new Vector2(3840, 1920);
            var p1 = new Vector2(137, 170);
            var p2 = new Vector2(1717, 1717);
            var p3 = new Vector2(2110, 211);
            var p4 = new Vector2(3677, 1755);

            var s1 = (p2 - p1);
            var s2 = (p4 - p3);
            var c1 = p1 + s1 / 2;
            var c2 = p3 + s2 / 2;

            var c1u = c1 / size;
            var c2u = c2 / size;
            var s1u = (s1 / size);
            var s2u = (s2 / size);

            c2u.X = 0.76f;
            c2u.Y = 0.525f;

            c1u.X = 0.24f;
            c1u.Y = 0.49f;

            s1u.X = 0.411f;
            s1u.Y = 0.826f;

            s2u.X = 0.408f;
            s2u.Y = 0.804f;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            ICameraManager? manager = null;

#if __ANDROID__
            manager = Context.Require<AndroidUsbCameraManager>();
#endif
            var controller = scene.AddComponent(new CameraController(manager));

            var videoTex = new Texture2D
            {
                Format = TextureFormat.Rgba8,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
            };

            controller.GetTexture = _ => videoTex;

            /*
            if (OperatingSystem.IsAndroid())
                videoTex.Type = TextureType.External;
            */

            var mat = new FishReflectionSphereMaterial(videoTex, FishReflectionMode.Stereo)
            {
                SphereRadius = 10f,
                SphereCenter = new Vector3(0, 0.68f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend,
                TextureCenter = [c1u, c2u],
                TextureRadius = [s1u, s2u],
                //IsExternal = true
            };

            var mesh = new TriangleMesh(new Quad3D(), mat);

            mesh.Transform.SetScale(1.3f);
            mesh.Transform.SetPosition(0, 1f, 0);

            /*
            mesh.AddComponent(new VideoTexturePlayer()
            {
                Texture = videoTex,
                Source = new Uri(GetAssetPath("Fish/0eb494e4-2537-4650-8718-9d6798c76898.mp4")),
                //Source = new Uri("rtsp://admin:123@192.168.1.60:8554/live"),
                //Source = new Uri("rtsp://admin:123@192.168.1.148:8554/live"),
                //Source = new Uri("rtsp://192.168.1.89:554/videodevice"),
                //Source = new Uri("rtsp://192.168.1.97:554/onvif1"),
                Reader = new RtspVideoReader()
            });
            */

            mesh.Name = "mesh";

            scene.AddChild(mesh);

            settings.Apply(mesh.Scene!);

            return builder
                .UseApp(app)
                .ConfigureSampleApp()
                .AddPanel(new PortalSettingsPanel(settings, scene))
                .ConfigureApp(e =>
                {
                    var oculus = e.XrApp.Plugin<OculusXrPlugin>();
                    var isLoading = false;
                    XrAnchor? window = null;

                    mesh.AddBehavior((_, _) =>
                    {
                        if (window == null)
                            return;

                        var loc = e.XrApp.LocateSpace(new Silk.NET.OpenXR.Space(window.Space), e.XrApp.ReferenceSpace);
                        if (loc.IsValid)
                        {
                            var offset = mesh.GetProp<float>("Offset");
                            var sphereY = mesh.GetProp<float>("SphereY");

                            var pos = loc.Pose.Position;
                            var q = loc.Pose.Orientation;

                            var fow = new Vector3(
                                2 * (q.X * q.Z + q.W * q.Y),
                                2 * (q.Y * q.Z - q.W * q.X),
                                1 - 2 * (q.X * q.X + q.Y * q.Y)
                            ).Normalize();

                            mesh.Transform.Position = pos + fow * offset;
                            mesh.Transform.Orientation = q;

                            var mat = ((FishReflectionSphereMaterial)mesh.Materials[0])!;

                            mat.SphereCenter = new Vector3(mesh.Transform.Position.X, sphereY, mesh.Transform.Position.Z);
                        }
                    });

                    mesh.AddBehavior(async (_, _) =>
                    {
                        if (!e.XrApp.IsStarted || isLoading || window != null)
                            return;

                        isLoading = true;
                        try
                        {
                            var anchors = await e.XrApp.Plugin<OculusXrPlugin>().GetAnchorsAsync(new XrAnchorFilter
                            {
                                Components = XrAnchorComponent.Label | XrAnchorComponent.Bounds
                            });

                            var walls = anchors.Where(a => a.Labels != null && a.Labels.Contains("WALL_FACE")).ToArray();
                            window = walls[2];

                            if (window.Bounds2D != null)
                            {
                                mesh.Transform.Scale = new Vector3(window.Bounds2D.Value.Width, window.Bounds2D.Value.Height, 0.01f);
                            }

                            await oculus.SetSpaceComponentStatusAsync(new Silk.NET.OpenXR.Space(window.Space), Silk.NET.OpenXR.SpaceComponentTypeFB.LocatableFB, true);
                        }
                        catch
                        {

                        }
                        finally
                        {
                            isLoading = false;
                        }
                    });
                });
        }
    }
}
