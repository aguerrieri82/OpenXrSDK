#if GLES
using XrEngine.Media;
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

#if !__ANDROID__
using XrEngine.Browser.Windows;
using XrEngine.UI.Web;
using XrEngine.Media;
#else
using XrEngine.Devices.Android;
#endif

using CanvasUI;
using DrumsVR.Game;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using RoomDesigner.Game;
using System.Numerics;
using XrEngine;
using XrEngine.AI;
using XrEngine.Audio;
using XrEngine.Audio.Midi;
using XrEngine.Bullet;
using XrEngine.Components;
using XrEngine.Devices;
using XrEngine.Gltf;
using XrEngine.Helpers;
using XrEngine.Objects;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.UI;
using XrMath;
using XrEngine.Reconstruct;
using XrSamples.Components;
using XrEngine.Lighting;
using System.Diagnostics;
using XrEngine.OpenGL;

namespace XrSamples
{
    public static class SampleScenes
    {
        static readonly GltfLoaderOptions GltfOptions = new()
        {
            ConvertColorTextureSRgb = true,
        };

        static string GetAssetPath(string name)
        {
            return Context.Require<IAssetStore>().GetPath(name);
        }

        static EngineApp CreateBaseScene()
        {
            var app = new EngineApp();

            var scene = new Scene3D();

            scene.AddComponent<AudioSystem>();

            scene.AddComponent<DebugGizmos>();

            scene.AddComponent<ShadowController>();

            scene.AddComponent<ResolveController>();

            scene.AddChild(new SunLight()
            {
                Name = "sun-light",
                Intensity = 1.0f,
                Direction = new Vector3(-0.1f, -0.9f, -0.15f).Normalize(),
                IsVisible = true
            });

            var pl1 = scene.AddChild(new PointLight());
            pl1.Transform.Position = new Vector3(0, 2, 0);
            pl1.Intensity = 0.3f;

            var pl2 = scene.AddChild(new PointLight());
            pl2.Name = "point-light-2";
            pl2.Transform.Position = new Vector3(0, -2, 0);
            pl2.Intensity = 0.3f;

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
            {
                Far = 100f,
                Near = 0.01f,
                BackgroundColor = new Color(0, 0, 0, 0),
                Exposure = 1
            };

            camera.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            scene.ActiveCamera = camera;

            app.OpenScene(scene);

            return app;
        }

        public static XrEngineAppBuilder UseEnvironmentHDR(this XrEngineAppBuilder builder, string assetPath, bool showEnv = false)
        {

            return builder

            .ConfigureApp(e =>
            {
                var scene = e.App.ActiveScene!;

                scene.PerspectiveCamera().Exposure = 1.0f;

                var envView = scene.AddChild<EnvironmentView>();
                envView.IsVisible = showEnv;

                var light = scene.AddChild<ImageLight>();
                light.Intensity = 1f;

                foreach (var l in scene.Descendants<Light>())
                {
                    if (l != light)
                        l.IsVisible = false;
                }

                light.LoadPanorama(assetPath);
            });
        }

        public static XrEngineAppBuilder UseShadows(this XrEngineAppBuilder builder)
        {
            return builder
                .SetGlOptions(opt =>
                {
                    opt.ShadowMap.UseShadowSampler = true;
                    opt.ShadowMap.UseVirtualReceiver = true;
                    opt.ShadowMap.FrustumMaxDistance = 4f;
                    opt.ShadowMap.Mode = ShadowMapMode.PCF;
                    opt.ShadowMap.Size = 1024;
                })
                .UseInputs<XrOculusTouchController>(a => a
                           .AddAction(b => b.Right!.Thumbstick))
                .ConfigureApp(e =>
                {
                    var scene = e.App.ActiveScene!;

                    var sun = scene.Descendants<SunLight>().FirstOrDefault();

                    if (sun == null)
                        return;

                    var azimuth = 0.0f;
                    var tilt = 0.0f;

                    const float azimuthSpeed = 0.055f;
                    const float tiltSpeed = 0.035f;
                    const float maxTilt = MathF.PI * 0.49f;

                    sun.IsVisible = true;
                    sun.CastShadows = true;

                    var view = scene.AddChild(new SunLightView(sun)
                    {
                        UseRoof = false
                    });

                    view.WorldPosition = new Vector3(0, 2f, 0);

                    scene.AddBehavior((_, ctx) =>
                    {
                        var thumb = e.Inputs!.Right!.Thumbstick;

                        if (thumb!.IsActive && thumb.IsChanged)
                        {
                            var v = thumb.Value;

                            if (MathF.Abs(v.X) > MathF.Abs(v.Y))
                            {
                                azimuth += v.X * azimuthSpeed;
                            }
                            else
                            {
                                tilt += v.Y * tiltSpeed;
                                tilt = Math.Clamp(tilt, 0.0f, maxTilt);
                            }

                            var sinTilt = MathF.Sin(tilt);
                            var cosTilt = MathF.Cos(tilt);

                            sun.Direction = Vector3.Normalize(new Vector3(
                                MathF.Sin(azimuth) * sinTilt,
                               -cosTilt,
                                MathF.Cos(azimuth) * sinTilt
                            ));
                        }

                    });
                });
        }

        public static XrEngineAppBuilder UseDefaultHDR(this XrEngineAppBuilder builder)
        {
            if (DefaultHDR == null)
                DefaultHDR = "res://asset/Envs/pisa.hdr";
            return builder.UseEnvironmentHDR(DefaultHDR, DefaultShowHDR);
        }

        public static XrEngineAppBuilder UseClickMoveFront(this XrEngineAppBuilder builder, Object3D obj, float distance = 0.5f)
        {
            return builder.ConfigureApp(e =>
            {
                var inputs = e.GetInputs<XrOculusTouchController>();

                obj.AddBehavior((_, _) =>
                {
                    var click = inputs.Right.Button.AClick; 
                    if (click.IsChanged && click.Value)
                    {
                        var scene = obj.Scene!;
                        obj.WorldPosition = scene.ActiveCamera!.WorldPosition + scene.ActiveCamera.Forward * distance;
                        obj.WorldOrientation = scene.ActiveCamera!.WorldOrientation;
                    }
                });
            });
        }

        public static XrEngineAppBuilder RemovePlaneGrid(this XrEngineAppBuilder builder) => builder.ConfigureApp(e =>
        {
            var grid = e.App.ActiveScene!.Descendants<PlaneGrid>().FirstOrDefault();
            if (grid != null)
                grid.IsVisible = false;
        });

        public static XrEngineAppBuilder AddPanel(this XrEngineAppBuilder builder, UIRoot uiRoot, bool forceOverlay = false, bool noOverlay = false)
        {
            var panel = new Window3D
            {
                Name = "UI Panel",
                Size = new Size2(0.8f, 0.5f),
                DpiScale = 1.6f,
                Content = uiRoot,
                WorldPosition = new Vector3(0, 1, 0),
            };

            return builder
                .UseClickMoveFront(panel, 0.5f)
                .ConfigureApp(e =>
                {
                    e.App.ActiveScene!.AddChild(panel);

                    if (!noOverlay && (XrPlatform.IsAndroid || forceOverlay))
                        panel.CreateOverlay(e.XrApp);
                });
        }

        public static XrEngineAppBuilder AddFloorShadow(this XrEngineAppBuilder builder, float size = 4, bool showShadowMap = false)
        {
            var floor = new TriangleMesh(new Cube3D(new Vector3(size, 0.01f, size)));
            floor.Name = "Floor";
            floor.Materials.Add(new ShadowOnlyMaterial
            {
                Name = "FloorMaterial",
                ShadowColor = new Color(1f, 0.1f, 0.1f, 0.7f),
            });

            floor.AddComponent<BoxCollider>();
            floor.AddComponent(new RigidBody()
            {
                Type = PhysicsActorType.Static,
            });

            floor.Transform.SetPositionY(-0.01f / 2.0f);

            TriangleMesh? depth = null;

            if (showShadowMap)
            {
                var depthView = new DepthViewMaterial();
                var texView = new TextureMaterial();

                depth = new TriangleMesh(Quad3D.Default, depthView);
                depth.Materials.Add(texView);

                depth.Transform.SetPositionY(1);

                depth.Name = "Depth";

                depth.AddBehavior((_, _) =>
                {
                    var sp = depth.Scene!.App!.Renderer.Feature<IShadowMapProvider>()!;

                    if (sp.Options.Mode == ShadowMapMode.VSM)
                    {
                        texView.IsEnabled = true;
                        depthView.IsEnabled = false;
                        if (texView.Texture == null)
                        {
                            texView.Texture = sp.ShadowMap;
                            depthView.NotifyChanged(ChangeType.Render);
                        }
                    }
                    else
                    {
                        texView.IsEnabled = false;
                        depthView.IsEnabled = true;

                        if (depthView.Texture == null)
                        {
                            depthView.Texture = sp.ShadowMap;
                            depthView.NotifyChanged(ChangeType.Render);
                        }

                        if (depthView.Camera == null)
                        {
                            depthView.Camera = sp.LightCamera;
                            depthView.NotifyChanged(ChangeType.Render);
                        }
                    }
                });
            }

            builder.ConfigureApp(e =>
            {
                e.App.ActiveScene!.AddChild(floor);
                if (depth != null)
                    e.App.ActiveScene!.AddChild(depth);

                var light = e.App.ActiveScene!.Descendants<DirectionalLight>().FirstOrDefault();
                if (light != null)
                {
                    light.CastShadows = true;
                    light.IsVisible = true;
                }
            });

            return builder;
        }

        public static XrEngineAppBuilder AddPanel<T>(this XrEngineAppBuilder builder) where T : UIRoot, new()
        {
            return builder.AddPanel(new T());
        }

        public static XrEngineAppBuilder ConfigureSampleApp(this XrEngineAppBuilder builder, bool usePt = true)
        {
            builder.AddXrRoot()
                   .UseHands()
                   .UseLeftController()
                   .UseRightController()
                   .AddRightPointer()
                   .UseInputs<XrOculusTouchController>(a => a
                       .AddAction(b => b.Right!.Thumbstick)
                       .AddAction(b => b.Right!.Haptic)
                       .AddAction(b => b.Left!.Haptic))
                   .UseRayCollider()
                   .UseGrabbers();

            if (IsEditor)
            {
                //usePt = false;
                Log.Error(builder, "Passtrhout not ADDED in editor");
            }

            if (usePt)
                builder.AddPassthrough();

            return builder;
        }

        public static XrEngineAppBuilder CreateChromeBrowser(this XrEngineAppBuilder builder)
        {
#if !__ANDROID__
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var display = new TriangleMesh(Quad3D.Default)
            {
                Name = "display"
            };

            display.Transform.Scale = new Vector3(1.6f, 1.2f, 0.01f);

            display.AddComponent<MeshCollider>();
            display.AddComponent<SurfaceController>();
            display.AddComponent(new ChromeWebBrowserView
            {
                ZoomLevel = 0,
                Source = "www.youtube.com",
            });

            scene.AddChild(display);

            return builder.UseApp(app)
              .ConfigureSampleApp()
              .UseClickMoveFront(display, 0.5f);
#else
            return builder;
#endif

        }

        [Sample("Throw")]
        public static XrEngineAppBuilder CreateThrow(this XrEngineAppBuilder builder)
        {
            var settings = new ThrowSettings();
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            scene.AddComponent<XrInputRecorder>();
            scene.AddComponent(new XrInputPlayer
            {
                RealTime = true,
                UseReferenceTime = true,
                FirstFrame = 300,
                LastFrame = 710,
                Loop = true
            });

            var cube = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff00000"));
            cube.Transform.SetScale(0.1f);
            cube.AddComponent<BoundsGrabbable>();
            cube.AddComponent<BoxCollider>();
            cube.AddComponent<Throwable>();

            var rb = cube.AddComponent(new RigidBody()
            {
                Type = PhysicsActorType.Dynamic,
                ToolMode = RigidBodyToolMode.KinematicTarget,
                AutoTeleport = false,
                Density = 100
            });

            scene.AddChild(cube);

            XrPoseInput? pose = null;
            XrBoolInput? pick = null;

            cube.AddBehavior((_, _) =>
            {
                if (XrApp.Current != null)
                {
                    pose ??= (XrPoseInput?)XrApp.Current!.Inputs["RightGripPose"];
                    pick ??= (XrBoolInput?)XrApp.Current!.Inputs["RightSqueezeClick"];
                }

                if (pick != null && pick.IsChanged && pick.Value)
                {
                    rb.Teleport(pose!.Value.Position);
                    //Context.Require<ITimeLogger>().Clear();
                }
            });

            return builder
              .UseApp(app)
              .ConfigureSampleApp()
              .UseDefaultHDR()
              .UsePhysics(new PhysicsOptions
              {

              })
              .AddPanel(new ThrowSettingsPanel(settings, cube))
              .ConfigureApp(app => settings.Apply(cube));
        }

        [Sample("Display")]
        public static XrEngineAppBuilder CreateDisplay(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var display = new TriangleMesh(Quad3D.Default);
            //display.Materials.Add(new StandardMaterial { Color = Color.White, DoubleSided = false, WriteDepth = false });

            display.Name = "display";

            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);

            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            return builder.UseApp(app)
                          .UseClickMoveFront(display, 0.5f)
                          .ConfigureSampleApp();
        }

        [Sample("Ping Pong")]
        public static XrEngineAppBuilder CreatePingPong(this XrEngineAppBuilder builder)
        {
            var settings = new PingPongSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "settings.json"));

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var racket = (Group3D)GltfLoader.LoadFile(GetAssetPath("Paddle.glb"), GltfOptions);
            racket.Name = "Racket";

            //Reposition
            racket.Transform.LocalPivot = new Vector3(0.054f, -0.04f, 0.174f);
            racket.Transform.Update();
            racket.Transform.Rotation = new Vector3(-0.863f, -0.21f, -1.25f);
            racket.Transform.Position = Vector3.Zero;

            racket.Transform.Update();

            foreach (var geo in racket.DescendantsWithFeature<Geometry3D>())
                geo.Feature.ApplyTransform(racket.Transform.Matrix);

            racket.Transform.Reset();
            racket.Transform.Position = new Vector3(0, 1, 0);

            //Audio
            var audio = scene.Component<AudioSystem>();
            var sound = new DynamicSound();
            sound.AddBuffers(audio.Device.Al, Context.Require<IAssetStore>(), "BallSounds");

            //Grabber
            racket.AddComponent<BoundsGrabbable>();

            //Colliders
            foreach (var item in racket.DescendantsWithFeature<TriangleMesh>())
                racket.AddComponent(new MeshCollider(item.Feature.Geometry!));

            //Rigid body
            var rigidBody = racket.AddComponent<RigidBody>();
            rigidBody.Type = PhysicsActorType.Kinematic;
            rigidBody.MaterialInfo = new PhysicsMaterialInfo();

            //Ball generator
            var bg = scene!.AddComponent(new BallGenerator(sound, 0f));
            bg.PhysicSettings = settings.Ball;

            //Sample ball
            var ball = bg.PickBall(new Vector3(-0.5f, 1.1f, 0));

            var ballRigid = ball.Component<RigidBody>();
            ballRigid.Started += (_, _) =>
            {
                ballRigid.DynamicActor.AddForce(new Vector3(0.3f, 0, 0), PxForceMode.Force);
            };

            //Add racket
            scene!.AddChild(racket);

            //Setup camera
            scene.PerspectiveCamera().Target = racket.Transform.Position;

            return builder
                   .UseApp(app)
                   .UseSceneMesh(true, true)
                   .ConfigureSampleApp()
                   .UseDefaultHDR()
                   .SetGlOptions(opt =>
                   {
                       opt.ShadowMap.LightBleed = 1;
                       opt.ShadowMap.BlurRadius = 2;
                       opt.ShadowMap.Mode = ShadowMapMode.PCF;
                       opt.ShadowMap.UseFrustumIntersect = true;
                   })
                   //.AddFloorShadow()
                   .UsePhysics(new PhysicsOptions
                   {

                   })
                   .AddPanel(new PingPongSettingsPanel(settings, scene))
                   .ConfigureApp(app =>
                   {
                       settings.Apply(app.App.ActiveScene!);
                       scene.Children.OfType<SunLight>().Single().IsVisible = true;
                   });

        }

        [Sample("Chess")]
        public static XrEngineAppBuilder CreateChess(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Chess/ABeautifulGame.bin");

            var mesh = (Group3D)AssetLoader.Instance.Load(new Uri("res://asset/Chess/ABeautifulGame.gltf"), typeof(Group3D), null, GltfOptions);
            mesh.Name = "mesh";
            mesh.BoundUpdateMode = UpdateMode.Automatic;

            foreach (var child in mesh.Children)
            {
                var rb = child.AddComponent<RigidBody>();
                child.AddComponent<BoxCollider>();

                if (child.Name!.Contains("board"))
                {
                    rb.Type = PhysicsActorType.Static;
                    child.Transform.SetPositionY(-0.25f);
                }
                else
                    child.AddComponent<BoundsGrabbable>();
                /*
                if (child is TriangleMesh mc)
                    XrEngine.MeshOptimizer.Simplify(mc.Geometry!);
                */
            }

            mesh.Transform.SetScale(4f);
            mesh.Transform.Position = new Vector3(0, 1.5f, 0);

            scene.AddChild(mesh);
            scene.PerspectiveCamera().Target = mesh.Transform.Position;

            return builder
                    .UseApp(app)
                    .ConfigureSampleApp()
                    .UseDefaultHDR()
                    .UsePhysics(new PhysicsOptions());
        }

        [Sample("Sponza")]
        public static XrEngineAppBuilder CreateSponza(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Sponza/Sponza.bin");

            var mesh = (Group3D)GltfLoader.LoadFile(GetAssetPath("Sponza/Sponza.gltf"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .ConfigureSampleApp();

        }

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
                Format = TextureFormat.Rgba32,
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

        public static XrEngineAppBuilder CreateController(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("Models/MetaQuestTouchPlus_Right.glb"), GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetPositionY(1);
            mesh.AddComponent<BoundsGrabbable>();

            foreach (var child in ((Group3D)mesh).Descendants<TriangleMesh>())
            {
                foreach (var mat in child.Materials)
                {
                    if (mat is IPbrMaterial pbr && pbr.Roughness == 0.2f)
                    {
                        //pbr.MetallicRoughness.RoughnessFactor = 0.2f;
                        //pbr.MetallicRoughness.MetallicFactor = 0f;
                        //pbr.MetallicRoughness.MetallicRoughnessTexture = null;
                    }
                }
            }

            scene.AddChild(mesh);

            scene.PerspectiveCamera().Target = mesh.Transform.Position;
            scene.PerspectiveCamera().Transform.Position = new Vector3(0.2f, 1.4f, 0.2f);
            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }

        [Sample("Window/Door")]
        public static XrEngineAppBuilder CreateWindow(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("Window.glb"), GltfOptions);
            mesh.Name = "Window";
            mesh.AddComponent(new GeometryScale
            {
                Min = new Vector3(0.7f, 1.1f, -0.045f),
                Max = new Vector3(0.9f, 1.5f, -0.00f),
            });

            IPbrMaterial pbr;

            foreach (var item in mesh.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                if (item.Name == "Plane")
                {
                    pbr = MaterialFactory.CreatePbr(new Color(1, 1, 1, 0.4f));
                    pbr.Alpha = AlphaMode.Blend;
                    pbr.Roughness = 0;
                    pbr.Metalness = 0;
                    pbr.DoubleSided = true;
                    item.Materials.Add((Material)pbr);
                }

                foreach (var material in item.Materials)
                {
                    if (material.Name == "Wood1024")
                    {
                        pbr = (IPbrMaterial)material;
                        pbr.Color = "#96893F";
                        pbr.Metalness = 0.8f;
                        pbr.Roughness = 0.25f;
                    }
                    if (material.Name == "Metal1024")
                    {
                        pbr = (IPbrMaterial)material;
                        pbr.Metalness = 0.9f;
                        pbr.Roughness = 0.12f;
                    }
                    material.WriteStencil = 2;
                }
            }

            var door = GltfLoader.LoadFile(GetAssetPath("Door.glb"), GltfOptions);
            door.Name = "Door";
            door.AddComponent(new GeometryScale
            {
                Min = new Vector3(-0.13f, 0.9f, -0.005f),
                Max = new Vector3(0f, 1.14f, 0.01f),
            });

            scene.AddChild(door);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }

        [Sample("Bed")]
        public static XrEngineAppBuilder CreateBed(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"), GltfOptions);
            mesh.Name = "Bed 1";
            mesh.AddComponent<PyMeshCollider>();
            mesh.AddComponent<BoundsGrabbable>();

            foreach (var material in mesh.Materials!)
            {
                material.CastShadows = true;
                material.WriteStencil = 1;
            }

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                //.UseSceneModel(false, false)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .AddFloorShadow(4, true)
                .UsePhysics(new PhysicsOptions())
                .ConfigureSampleApp();
        }

        [Sample("Light Field")]
        public static XrEngineAppBuilder CreateLightField(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            scene.AddChild(new SpotLight()
            {
                WorldPosition = new Vector3(0, 0.63f, 1.84f),
                Direction = new Vector3(0, -0.2f, -1),
                Range = 4,
                Intensity = 5,
                InnerConeAngle = (14f).ToRadians(),
                OuterConeAngle = (20f).ToRadians(),
            });

            scene.AddChild(new AreaLight()
            {
                WorldPosition = new(0f, 1.31f, 1.6700001f),
                PlaneSize = new(1f, 0.8f),
                PlaneNormal = new(0.0348995f, 0f, -0.99939084f),
                Range = 4f,
                Direction = new(0f, -0.5344989f, -0.8451692f),
                Specular = "#00000000",
                Intensity = 5f
            });

            var voxelSize = 0.05f;
            var roomSize = new Vector3(5, 2, 5);

            var grid = new VoxelGridDesc
            {
                Origin = new Vector3(-roomSize.X / 2, 0f, -roomSize.Z / 2),
                VoxelSize = voxelSize,
                Size = new Vector3I(
                    (int)MathF.Round(roomSize.X / voxelSize),
                    (int)MathF.Round(roomSize.Y / voxelSize),
                    (int)MathF.Round(roomSize.Z / voxelSize))
            };

            var mesh = scene.AddChild((TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"), GltfOptions));
            mesh.AddComponent<LightFieldReceiver>();
            mesh.Name = "Bed";

            XrEngine.MeshOptimizer.Optimize(mesh.Geometry!);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseFloorTeleport(scene)
                .ConfigureApp(cfg =>
                {
                    foreach (var light in scene.Descendants<Light>())
                    {
                        light.IsVisible = true;
                        light.AddComponent(new LightFieldEmitter() { IsEnabled = false });
                    }

                    scene.AddComponent<LightFieldProvider>();

                    var lightField = scene.AddComponent(new LightFieldDebug(grid, XrPlatform.IsAndroid)
                    {
                        StorePath = Path.Combine(XrPlatform.Current!.SharedPath)
                    });

                    if (XrPlatform.IsAndroid)
                    {
                        lightField.Import();
                    }

                    scene.AddBehavior((_, _) =>
                    {
                        var click = cfg.Inputs!.Right!.Button!.AClick!;

                        if (click.IsChanged && click.Value)
                        {
                            var provider = Context.Require<IXrMotionVectorProvider>();
                            provider.IsActive = !provider.IsActive;
                        }
                    });
                })
                .ConfigureSampleApp(false);
        }

        [Sample("Cucina")]
        public static XrEngineAppBuilder CreateCucina(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("cucina.glb"), GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.04f);
            mesh.Transform.Position = new Vector3(-mesh.WorldBounds.Center.X, 0, -mesh.WorldBounds.Center.Z);

            var blank = (Material)MaterialFactory.CreatePbr(Color.White);

            foreach (var item in mesh.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                if (IsEditor)
                    item.AddComponent<BoxCollider>();

                if (item.Name != "Obj_PolyFaceMesh_51")
                    item.IsVisible = true;

                for (var i = 0; i < item.Materials.Count; i++)
                {
                    var material = (IPbrMaterial)item.Materials[i];
                    if (material.ColorMap == null)
                        item.Materials[i] = blank;

                    material.DoubleSided = true;

                    if (material.Name == "wfnhfaq_Stucco_Facade")
                    {
                        material.Color = new Color(1.6f, 1.6f, 1.6f, 1);
                    }

                    if (material.Name == "schcbgfp_Scratched_Polyvinylpyrrolidone_Plastic")
                    {
                        //material.Metalness = 0;
                    }

                    if (material.Name == "vigjfivg_Old_Plywood")
                    {
                        //material.Metalness = 0;
                        //material.Roughness = 0.7f;
                    }

                    if (material.Name == "wjmkfbnl_Crema_Marfi_Marble")
                    {
                        // material.Metalness = 0;
                    }

                    if (material.Name == "shkaaafc_Brushed_Aluminum")
                    {
                        material.Color = new Color(0.35f, 0.3f, 0.3f, 1);
                        //material.Roughness = 1;
                    }

                    if (material.Name == "uk3kec1ew_Brown_Tiles")
                    {
                        //material.Roughness = 0.6f;
                    }

                }

            }

            string[] wallNames = ["Obj_3dSolid_912", "Obj_3dSolid_909", "Obj_3dSolid_910", "Obj_3dSolid_911", "Obj_3dSolid_419"];
            var group = new Group3D()
            {
                Name = "walls",
                IsVisible = true
            };

            foreach (var item in wallNames)
            {
                var obj = mesh.DescendantsOrSelf().Where(a => a.Name == item).FirstOrDefault();
                if (obj != null)
                    group.AddChild(obj.Parent!);
            }

            if (mesh is Group3D meshGrp)
                meshGrp.AddChild(group);
            mesh.AddComponent<ConstraintGrabbable>();

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                //.UseSceneModel(true, false)
                .ConfigureApp(cfg =>
                {
                    scene.FindByName<Light>("point-light-1")!.IsVisible = true;
                    scene.FindByName<PointLight>("point-light-1")!.Range = 5f;
                })
                .ConfigureSampleApp();
        }

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

        [Sample("Tone Control")]
        public static XrEngineAppBuilder CreateToneControl(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var dir = scene.Descendants<SunLight>().First();
            dir.Direction = -Vector3.UnitZ;
            dir.Intensity = 3.2f;

            foreach (var light in scene.Descendants<PointLight>())
            {
                light.IsVisible = false;
                light.AddComponent<LightViewer>();
            }

            var spot = scene.AddChild(new SpotLight());
            spot.IsVisible = false;
            spot.AddComponent<LightViewer>();

            var tc = scene.AddComponent(new ToneControl());

            var mat1 = new TextureMaterial();
            var mat2 = new PbrMaterial()
            {
                Metalness = 0
            };

            var mat3 = new ColorMaterial(new Color(0.5f, 0.5f, 0.5f));
            var mat4 = new PbrMaterial()
            {
                Color = mat3.Color,
                Metalness = 0
            };


            var quod1 = scene.AddChild(new TriangleMesh(Quad3D.Default, mat1));
            quod1.Materials.Add(mat2);

            var quod2 = scene.AddChild(new TriangleMesh(Quad3D.Default, mat3));
            quod2.Materials.Add(mat4);
            quod2.WorldPosition = new Vector3(1, 0, 0);

            void LoadTexture(bool isSrgb)
            {
                var fileName = "D:\\Projects\\XrEditor\\Cache\\Download\\493AE6FA342EE91A1979CB965B081079.jpg";

                mat1.Texture = AssetLoader.Instance.Load<Texture2D>(fileName, new TextureLoadOptions
                {
                    IsSrgb = isSrgb,
                    UseCache = false,
                    MimeType = "image/jpeg"
                });

                mat1.Texture.MipLevelCount = 10;
                mat1.Texture.MinFilter = ScaleFilter.LinearMipmapLinear;

                mat2.ColorMap = mat1.Texture;

                mat1.IsEnabled = !tc.ShowPbr;
                mat2.IsEnabled = tc.ShowPbr;

                mat3.IsEnabled = !tc.ShowPbr;
                mat4.IsEnabled = tc.ShowPbr;

                var color = new Color(0.5f, 0.5f, 0.5f)
                {
                    IsSrgb = isSrgb
                };
                
                mat4.Color = color;
                mat3.Color = color;

                foreach (var mesh in scene.Descendants<TriangleMesh>())
                {
                    foreach (var material in mesh.Materials)
                        material.NotifyChanged();
                }

            }

            tc.Changed = () =>
            {
                LoadTexture(tc.TexSRgb);
            };

            LoadTexture(true);

            return builder
                .UseApp(app)
                //.UseDefaultHDR()
                .ConfigureSampleApp();
        }

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

        [Sample("Helmet")]
        public static XrEngineAppBuilder CreateHelmet(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            GetAssetPath("Helmet/DamagedHelmet.bin");

            var mesh = GltfLoader.LoadFile(GetAssetPath("Helmet/DamagedHelmet.gltf"), GltfOptions, GetAssetPath);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.4f);
            mesh.Transform.SetPositionY(1);
            mesh.AddComponent<BoundsGrabbable>();
            mesh.CastShadows(true);

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("res://asset/Envs/Cannon_Exterior.hdr")
                .UseEnvironmentMesh(100)
                .UseShadows()
                .ConfigureSampleApp();
        }

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

        [Sample("Tac")]
        public static XrEngineAppBuilder CreateTac(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh1 = (TriangleMesh)AssetLoader.Instance.Load(new Uri("D:\\Misc\\TAC\\Head-Skin.obj"), typeof(TriangleMesh), null);
            var mesh2 = (TriangleMesh)AssetLoader.Instance.Load(new Uri("D:\\Misc\\TAC\\Head-Bone.obj"), typeof(TriangleMesh), null);

            var mat1 = (PbrMaterial)MaterialFactory.CreatePbr(Color.White);

            mat1.ClipVolume = new Bounds3()
            {
                Min = new Vector3(0, float.NegativeInfinity, float.NegativeInfinity),
                Max = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            };

            mesh1.Materials.Add(mat1);

            var mat2 = (PbrMaterial)MaterialFactory.CreatePbr(Color.White);

            mat2.ClipVolume = new Bounds3()
            {
                Min = new Vector3(0, float.NegativeInfinity, float.NegativeInfinity),
                Max = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            };

            mesh2.Materials.Add(mat2);

            var grp = new Group3D();
            grp.Name = "Tac";
            grp.Transform.SetScale(0.001f);
            grp.Transform.Rotation = new Vector3(-MathF.PI / 2, 0, 0);

            grp.AddComponent<BoundsGrabbable>();
            grp.AddChild(mesh1);
            grp.AddChild(mesh2);

            scene.AddChild(grp);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }

        static Material LoadMaterial(string url)
        {
            var gltf = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath(url), GltfOptions);
            return gltf.Materials[0];
        }

        [Sample("Scanner")]
        public static XrEngineAppBuilder CreateScanner(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            var panel = new TextPanel();

            var window = new Window3D();

            window.Size = new Size2(0.05f, 0.02f);
            window.DpiScale = 1.1f;
            window.Content = panel;

            var mat = new TextureClipMaterial();
            mat.Alpha = AlphaMode.Blend;
            window.Materials.Clear();
            window.Materials.Add(mat);

            var isInit = false;

            window.AddBehavior((a, b) =>
            {
                if (!isInit && window.ActiveTexture != null)
                {
                    mat.MainLeftTexture = window.ActiveTexture;
                    var size = new Vector2(window.ActiveTexture.Width, window.ActiveTexture.Height);
                    var viewSize = new Vector2(scene.ActiveCamera!.ViewSize.Width, scene.ActiveCamera.ViewSize.Height);
                    var relSize = 2 * size / viewSize;
                    window.Transform.Scale = new Vector3(relSize.X, relSize.Y, 1);
                    //window.Transform.Position = new Vector3(-1 + 0.2f + relSize.X / 2, 1 - 0.2f - relSize.Y / 2, 0);
                    isInit = true;
                }

                if (panel.Text != null)
                    panel.Text.Text = b.Frame.ToString();
            });

            var points = new PointMesh();
            var depth = points.AddComponent(new DepthPointScanner
            {
                SavePath = Path.Join(XrPlatform.Current!.PersistentPath, "Scanner"),
            });

            scene.AddChild(points);
            scene.AddChild(window);

            return builder
              .UseApp(app)
              .UseDefaultHDR()
              .ConfigureSampleApp(true)
              .UseEnvironmentDepth()
              .SetGlOptions(opt =>
              {
                  opt.FrustumCulling = false;
              })
              .ConfigureApp(a =>
              {
                  depth.ScanInput = a.Inputs!.Right!.TriggerClick;
                  depth.ClearInput = a.Inputs!.Right.Button!.BClick;
                  depth.HideInput = a.Inputs!.Right.Button!.AClick;
              });
        }

        public static XrEngineAppBuilder CreateHeightMap(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            var mat = MaterialFactory.CreatePbr("#ffffff");
            mat.Roughness = 0f;

            /*
            mat.ColorMap = AssetLoader.Instance.Load<Texture2D>("res://asset/Earth/waves.png");
            mat.ColorMap.Transform = Matrix3x3.CreateScale(-1, 1);
            mat.ColorMap.WrapS = WrapMode.Repeat;
            mat.ColorMap.WrapT = WrapMode.Repeat;
            mat.ColorMap.Format = TextureFormat.SBgra32;
            */

            if (mat is IHeightMaterial hm)
            {
                hm.HeightMap = new HeightMapSettings
                {
                    Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/Earth/waves.png"),
                    ScaleFactor = 0.3f,
                    TargetTriSize = 5,
                    DebugTessellation = false,
                    NormalStrength = new Vector3(20, 20, 1),
                    NormalMode = HeightNormalMode.Sobel
                };

                hm.HeightMap.Texture.WrapS = WrapMode.Repeat;
                hm.HeightMap.Texture.WrapT = WrapMode.Repeat;
                hm.HeightMap.Texture.MagFilter = ScaleFilter.Linear;
                hm.HeightMap.Texture.MinFilter = ScaleFilter.Linear;

                //mat.NormalMap = NormalMap.FromHeightMap(hm.HeightMap, 1f);
                //mat.NormalMap.SaveAs("d:\\heightmap.png");
            }

            var quod = new QuadPatch3D(new Vector2(2, 1), 100);
            //quod.ToTriangles();

            var plane = new TriangleMesh(quod, (Material)mat);

            scene.AddChild(plane);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();

        }

        [Sample("Teleport")]
        public static XrEngineAppBuilder CreateTeleport(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            scene.ActiveCamera!.BackgroundColor = "#7C93DB";

            var mat = MaterialFactory.CreatePbr("#ffffff");
            mat.ColorMap = TextureFactory.CreateChecker();
            mat.ColorMap.Transform = Matrix3x3.CreateScale(10, 10);
            var floor = new TriangleMesh(Quad3D.Default, (Material)mat);
            floor.Transform.SetScale(10, 10, 1);
            floor.Transform.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);
            floor.AddComponent<TeleportTarget>();
            floor.Name = "Floor";

            var cube = new TriangleMesh(Cube3D.Default, (Material)mat);
            cube.Transform.SetScale(3, 3, 3);
            cube.WorldPosition = new Vector3(2, 0, 2);
            cube.AddComponent<TeleportTarget>();
            cube.Name = "Cube";

            var player = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff0000"));
            player.Transform.SetScale(0.3f, 1.7f, 0.3f);
            player.AddComponent(new XrPlayer
            {
                Height = 0f
            });
            player.Name = "Player";

            scene.AddChild(floor);
            scene.AddChild(player);
            scene.AddChild(cube);
            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp()
                .UseTeleport(ControllerHand.Left, player)
                .ConfigureApp(e =>
                {
                    e.XrApp.UseLocalSpace = false;

                    var root = e.App.ActiveScene!.Children.OfType<XrRoot>().First();
                    root.LeftController!.SetWorldPose(new Pose3()
                    {
                        Position = new Vector3(0f, 0.22f, 0f),
                        Orientation = new Quaternion(0.47238404f, -0.19674662f, -0.10905845f, 0.8522032f)
                    });
                });
        }

        [Sample("IK")]
        public static XrEngineAppBuilder CreateIk(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var sphere1 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "right"
            };

            var sphere2 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "left"
            };

            var sphere3 = new TriangleMesh(Sphere3D.Default,
                (Material)MaterialFactory.CreatePbr(new Color(1f, 1, 0, 1)))
            {
                Name = "head"
            };

            sphere3.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0f, 1.4599999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            sphere2.SetWorldPose(new Pose3()
            {
                Position = new Vector3(-0.53f, 1.1999999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            sphere1.SetWorldPose(new Pose3()
            {
                Position = new Vector3(0.53f, 1.1999999f, 0f),
                Orientation = new Quaternion(0f, 0f, 0f, 1f)
            });

            var grp = new Group3D()
            {
                Name = "Preview"
            };

            sphere1.Transform.SetScale(0.05f);
            sphere2.Transform.SetScale(0.05f);
            sphere3.Transform.SetScale(0.05f);

            scene.AddChild(sphere1);
            scene.AddChild(sphere2);
            scene.AddChild(sphere3);
            scene.AddChild(grp);

            var solver = new IkSolver();
            solver.Build(IkBodies.CreateArms());

            var updated = grp.AddComponent<IkUpdater>();
            var viewer = grp.AddComponent<IkViewer>();

            updated.Solver = solver;
            viewer.Solver = solver;

            updated.SetTarget("Head", sphere3);
            updated.SetTarget("Hand-L", sphere2);
            updated.SetTarget("Hand-R", sphere1);

            return builder
                .UseApp(app)
                //.UseEnvironmentDepth()
                //.UseDefaultHDR()
                .ConfigureSampleApp()
                .ConfigureApp(a =>
                {
                    var left = a.Inputs!.Left!.GripPose!;
                    var right = a.Inputs!.Right!.GripPose!;

                    scene.AddBehavior((scene, ctx) =>
                    {
                        solver.WorldPose = grp.GetWorldPose();

                        if (XrApp.Current?.IsStarted == false)
                            return;

                        var head = XrApp.Current!.SpacesTracker.GetLastLocation(XrApp.Current.Head)!.Pose;
                        var ofs = new Vector3(0, 1.4f, 0);

                        var leftPos = (left.Value.Position - head.Position) + ofs;
                        var rightPos = (right.Value.Position - head.Position) + ofs;

                        sphere1.WorldPosition = rightPos;
                        sphere2.WorldPosition = leftPos;
                        sphere3.WorldPosition = ofs;
                    });
                });
        }

        [Sample("Panorama Maker")]
        public static XrEngineAppBuilder CreatePanoramaMaker(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var maker = scene.AddComponent<PanoramaMaker>();

            scene.AddChild(new CubeView(maker.CubeTexture));

            var display = new TriangleMesh(Quad3D.Default, new TextureMaterial(maker.CameraTexture));
            display.Name = "camera_preview";
            display.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);

            scene.AddChild(display);

            return builder
                .UseApp(app)
                .UseClickMoveFront(display, 0.5f)
                .ConfigureApp(cfg =>
                {
                    maker.Configure(cfg.Inputs!);
                })
                .ConfigureSampleApp();
        }

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

        public static XrEngineAppBuilder CreatePoseTest(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var left = new Pose3
            {
                Position = new Vector3(-0.0320870019f, -0.0172204766f, -0.0633444414f),
                Orientation = new Quaternion(-0.995334148f, -0.00154464366f, -0.00174995663f, 0.0964596644f)
            };

            var right = new Pose3
            {
                Position = new Vector3(0.0315504968f, -0.017489884f, -0.0631345809f),
                Orientation = new Quaternion(-0.995401025f, 0.00226922776f, 0.00283159781f, 0.0957266465f)
            };

            Pose3 GetLensPose(Pose3 curPose)
            {
                var realPos = curPose.Position;
                realPos.X = -realPos.X;

                var rawRot = curPose.Orientation;

                var rot = rawRot;
                rot.Y = -rot.Y;
                rot.Z = -rot.Z;
                rot = Quaternion.Normalize(rot);

                var worldRot = Quaternion.Inverse(rot);
                var sensorFix = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);

                return new Pose3
                {
                    Position = realPos,
                    Orientation = Quaternion.Normalize(worldRot * sensorFix)
                };
            }

            var headeset = new Group3D() { Name = "Headset" };

            headeset.AddChild(new PoseView(left, "Left", "#ff00ff"));
            headeset.AddChild(new PoseView(right, "Right", "#ffff00"));
            headeset.AddChild(new PoseView(new Pose3(), "Origin", "#ffffff"));

            scene.AddChild(headeset);

            var headeset2 = new Group3D() { Name = "Headset2" };

            headeset2.AddChild(new PoseView(GetLensPose(left), "Left", "#ff00ff"));
            headeset2.AddChild(new PoseView(GetLensPose(right), "Right", "#ffff00"));
            headeset2.AddChild(new PoseView(new Pose3(), "Origin", "#ffffff"));

            scene.AddChild(headeset2);

            scene.AddChild(headeset);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }

        [Sample("Usb Camera")]
        public static XrEngineAppBuilder CreateUsbCamera(this XrEngineAppBuilder builder)
        {
            ICameraManager? manager = null;

#if __ANDROID__
            manager = Context.Require<AndroidUsbCameraManager>();
#endif

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var texture = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
            };

            var main = new TriangleMesh(Quad3D.Default, new TextureMaterial(texture)
            {
            });

            main.Name = "Usb";
            main.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);

            scene.AddChild(main);

            var cameraState = 0;

            ICameraDevice? camera = null;

            scene.AddBehavior(async (_, _) =>
            {
                var button = XrEngineApp.Current?.Inputs?.Right?.Button?.BClick;

                var aPressed = button != null && button.IsChanged && button.Value;

                if (cameraState == 0 && texture.Handle != 0)
                {
                    var cameras = manager!.GetCameras();

                    if (cameras == null || cameras.Count == 0)
                        return;

                    cameraState = 1;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            camera = await manager.OpenCameraAsync(cameras[0].Id!);

                            var formats = camera.GetSupportedFormats();

                            var curFormat = formats
                               .Where(a => a.ImageFormat == ImageFormat.Rgb32)
                               .OrderByDescending(a => a.Width * a.Height)
                               .ThenByDescending(a => a.FrameRate)
                               .FirstOrDefault();

                            var ratio = (float)curFormat.Width / curFormat.Height;
                            var height = 0.5f;
                            var width = height * ratio;

                            await EngineApp.MainThread;

                            main.Transform.Scale = new Vector3(width, height, 0.01f);

                            await camera.StartCaptureAsync(curFormat, texture);

                            cameraState = 2;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Usb", ex);
                        }

                    });
                }

                if (cameraState == 2)
                    camera?.UpdateTexture();
            });

            return builder
                .UseApp(app)
                .UseClickMoveFront(main)
                .ConfigureSampleApp();
        }

        [Sample("Capture")]
        public static XrEngineAppBuilder CreateCapture(this XrEngineAppBuilder builder)
        {
            #region HELPERS

            static Rect2 CalcSensorCropRegion(
                float sensorWidth,
                float sensorHeight,
                float currentWidth,
                float currentHeight)
            {
                var scaleX = currentWidth / sensorWidth;
                var scaleY = currentHeight / sensorHeight;

                var maxScale = MathF.Max(scaleX, scaleY);

                scaleX /= maxScale;
                scaleY /= maxScale;

                return new Rect2
                {
                    X = sensorWidth * (1.0f - scaleX) * 0.5f,
                    Y = sensorHeight * (1.0f - scaleY) * 0.5f,
                    Width = sensorWidth * scaleX,
                    Height = sensorHeight * scaleY
                };
            }

            static Matrix4x4 ComputeQuadMatrixV2(
                Matrix4x4 headMatrix,
                CameraParams cam,
                float distanceMeters)
            {

                var fx = cam.Fx;
                var fy = cam.Fy;
                var cx = cam.Cx;
                var cy = cam.Cy;

                var sensorW = cam.SensorSize!.Value.Width;
                var sensorH = cam.SensorSize.Value.Height;

                var currentW = cam.CurrentSize.Width;
                var currentH = cam.CurrentSize.Height;

                var crop = CalcSensorCropRegion(
                    sensorW,
                    sensorH,
                    currentW,
                    currentH);

                var x0 = distanceMeters * ((crop.X - cx) / fx);
                var x1 = distanceMeters * ((crop.X + crop.Width - cx) / fx);

                var y0 = distanceMeters * ((crop.Y - cy) / fy);
                var y1 = distanceMeters * ((crop.Y + crop.Height - cy) / fy);

                var centerX = (x0 + x1) * 0.5f;
                var centerY = (y0 + y1) * 0.5f;

                var scaleX = x1 - x0;
                var scaleY = y1 - y0;

                var quadToSensor =
                    Matrix4x4.CreateScale(scaleX, scaleY, 1.0f) *
                    Matrix4x4.CreateTranslation(centerX, centerY, -distanceMeters);

                var sensorToHead = cam.GetLensPose().ToMatrix();

                return quadToSensor * sensorToHead * headMatrix;
            }

            static Matrix4x4 ComputeQuadMatrixScaledFrom1m(
                Matrix4x4 headMatrix,
                Matrix4x4 eyeMatrix,
                CameraParams cam,
                float distanceMeters)
            {
                const float referenceDistance = 3.0f;

                var quadAt1m =
                    ComputeQuadMatrixV2(headMatrix, cam, referenceDistance);

                var scale =
                    distanceMeters / referenceDistance;

                var eyePos = eyeMatrix.Translation;

                var aroundEye =
                    Matrix4x4.CreateTranslation(-eyePos) *
                    Matrix4x4.CreateScale(scale) *
                    Matrix4x4.CreateTranslation(eyePos);

                return quadAt1m * aroundEye;
            }

            #endregion

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            CameraParams leftParams = new();
            CameraParams rightParams = new();

            var leftTex = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            var rightTex = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            var mainLeft = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(leftTex, rightTex)
            {
                FixedEye = 0,
                UseDepth = false
            });

            mainLeft.Name = "MainLeft";
            mainLeft.Transform.Scale = new Vector3(0.7f, 0.7f, 0.01f);

            var right = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(leftTex, rightTex)
            {
                FixedEye = 1,
                UseDepth = false
            });

            right.Name = "Right";
            right.Transform.Scale = mainLeft.Transform.Scale;

            scene.AddChild(mainLeft);
            scene.AddChild(right);

            var cameraState = 0;

            var mustTrack = false;

            ICameraDevice? cameraLeft = null;
            ICameraDevice? cameraRight = null;

            var leftPos = Vector3.Zero;

            scene.AddBehavior(async (_, _) =>
            {
                var button = XrEngineApp.Current?.Inputs?.Right?.Button?.BClick;

                var aPressed = button != null && button.IsChanged && button.Value;

                if (aPressed)
                    mustTrack = !mustTrack;

                if (cameraState == 0)
                {
                    rightTex.Generate();
                    leftTex.Generate();

                    cameraState = 1;

                    _ = Task.Run(async () =>
                    {
                        var manager = Context.Require<ILocalCameraManger>();

                        var cameras = manager.GetCameras();

                        Log.Info("", "CAMERAS: {0}", string.Join(',', cameras.Select(a => a.Id)));

                        var infoLeft = cameras.First(a => a.Source == 0 && a.Position == 0);
                        var infoRight = cameras.First(a => a.Source == 0 && a.Position == 1);

                        cameraLeft = await manager.OpenCameraAsync(infoLeft.Id!);
                        cameraRight = await manager.OpenCameraAsync(infoRight.Id!);

                        var formats = cameraLeft.GetSupportedFormats();

                        var curFormat = formats.Last();

                        await Task.WhenAll(cameraLeft.StartCaptureAsync(curFormat, leftTex),
                                           cameraRight.StartCaptureAsync(curFormat, rightTex));

                        cameraState = 2;

                        leftParams = cameraLeft.GetParams();
                        rightParams = cameraRight.GetParams();

                    });
                }

                if (cameraState == 2)
                {
                    cameraLeft?.UpdateTexture();
                    cameraRight?.UpdateTexture();

                    if (cameraLeft!.LastTimestamp == 0 || cameraRight!.LastTimestamp == 0)
                        return;

                    var headLeftTime = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                        XrApp.Current.ReferenceSpace, cameraLeft!.LastTimestamp).Pose;

                    var headRightTime = XrApp.Current!.LocateSpace(XrApp.Current.Head,
                        XrApp.Current.ReferenceSpace, cameraRight!.LastTimestamp).Pose;

                    if (mustTrack)
                    {
                        var thumb = XrEngineApp.Current?.Inputs?.Right?.Thumbstick!.Value;

                        Debug.Assert(scene.ActiveCamera?.Eyes != null);

                        mainLeft.WorldMatrix = ComputeQuadMatrixScaledFrom1m(headLeftTime.ToMatrix(), scene.ActiveCamera.Eyes[0].World, leftParams, 2f + (thumb!.Value.Y * 2f));
                        right.WorldMatrix = ComputeQuadMatrixScaledFrom1m(headRightTime.ToMatrix(), scene.ActiveCamera.Eyes[1].World, rightParams, 2f + (thumb!.Value.Y * 2f));

                    }
                }
            });

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }

        [Sample("Reconstruct Capture")]
        public static XrEngineAppBuilder CreateReconstructCapture(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var recorder = new XrReconstructRecorder();

            CameraParams cameraParams = new();

            var display = new TriangleMesh(Quad3D.Default, new EyeTextureMaterial(recorder.LeftTex, recorder.RightTex));

            display.Name = "capture_display";
            display.Transform.Scale = new Vector3(1.08f, 1.08f, 0.01f);
            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            var cameraState = 0;
            var startTime = DateTime.Now;
            var isRoomCaptured = false;
            var sharedPath = Context.Require<IPlatform>().SharedPath;

            scene.AddBehavior((_, ctx) =>
            {
                if (cameraState == 0 && recorder.LeftTex.Handle != 0)
                {
                    cameraState = 1;

                    recorder.StartCaptureAsync(Context.Require<IPlatform>().SharedPath!).ContinueWith(a =>
                    {
                        cameraState = 2;
                    });
                }

                if (!isRoomCaptured)
                {
                    var model = scene.FindByName<TriangleMesh>("Mesh");
                    if (model != null && model.Component<XrAnchorUpdate>().HasPose)
                    {
                        model.Geometry!.EnsureIndices();
                        var writer = new ObjWriter();
                        writer.Add(model);
                        File.WriteAllText(Path.Combine(sharedPath, "scene.obj"), writer.Text());
                        recorder.Stats!.ScenePosition = model.GetWorldPose();
                        model.Remove();
                        isRoomCaptured = true;
                    }

                }

                if (cameraState == 2)
                {
                    try
                    {
                        recorder.CaptureFrame(scene.ActiveCamera!);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("CreateReconstructCapture", ex, "CaptureFrame");
                    }

                    recorder.UpdateTextures();

                    if ((DateTime.Now - startTime).TotalSeconds >= 30)
                    {
                        recorder.StopCapture();
                        cameraState = 3;
                    }
                }
            });

            return builder
                .UseApp(app)
                .UseClickMoveFront(display, 0.5f)
                .UseEnvironmentDepth()
                .UseSceneMesh(true, false)
                .ConfigureSampleApp();
        }

        [Sample("Midi")]
        public static XrEngineAppBuilder CreateMidi(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var manager = Context.Require<IMidiManager>();

            var devices = manager.FindDevices();

            var usb = devices.FirstOrDefault(a => a.Name == "USB MIDI Interface" && a.Id!.StartsWith("in"));

            if (usb == null)
                usb = devices[0];

            var device = manager.GetDevice(usb.Id!);

            device!.OpenAsync().Wait();

            var inPort = device.OpenInput(0);
            inPort.DataReceived += (sender, e) =>
            {
                var span = new ReadOnlySpan<byte>(e.Data, e.Offset, e.Count);
                var msg = MidiMessageDecoder.Decode(span);
                if (msg is ActiveSensingMessage)
                    return;
                if (msg != null)
                    Log.Info(typeof(SampleScenes), $"MIDI Message: {msg}");
            };

            return builder
                .UseApp(app)
                //.UseEnvironmentDepth()
                //.UseDefaultHDR()
                .ConfigureSampleApp();
        }

        [Sample("Car")]
        public static XrEngineAppBuilder CreateCar(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            scene.ActiveCamera!.BackgroundColor = "#7C93DB";
            scene.Id = Guid.Parse("9692f695-f53c-40c4-900a-d17ac94302d8");

            //Physics
            var pm = scene.AddComponent(new PhysicsManager(60));
            pm.SetCollideGroup(RigidBodyGroup.Group1, CollideGroup.Never);
            pm.SetCollideGroup(RigidBodyGroup.Group2, CollideGroup.Always);

            scene.AddComponent(new InputPhysicsForce
            {
                InputName = "RightGripPose",
                HandlerName = "RightSqueezeClick",
                HapticName = "RightHaptic",
                Tollerance = 0.01f,
                Factor = 0.1f
            });

            scene.AddComponent(new InputPhysicsForce
            {
                InputName = "LeftGripPose",
                HandlerName = "LeftSqueezeClick",
                HapticName = "LeftHaptic",
                Factor = 0.1f
            });

            //Material
            var leather = (IPbrMaterial)LoadMaterial("Materials/xjekdbj_tier_2.gltf");
            leather.Color = "#FF6400FF";
            leather.DoubleSided = true;
            leather.Color *= 2f;

            var car = (Group3D)GltfLoader.LoadFile(GetAssetPath("car.glb"), GltfOptions, GetAssetPath);
            car.Name = "car";

            var bodyMeshes = new HashSet<TriangleMesh>();

            //Fix model
            foreach (var mat in car.DescendantsOrSelf().OfType<TriangleMesh>().SelectMany(a => a.Materials).Distinct())
            {
                if (mat is IPbrMaterial pbr)
                {

                    if (mat.Name!.Contains("glass"))
                    {
                        pbr.Color = "#00000020";
                        pbr.Alpha = AlphaMode.Blend;
                        pbr.AlphaCutoff = 0.2f;
                    }
                    if (mat.Name!.Contains("paint"))
                    {
                        pbr.Color = "#FF0100FF";
                        pbr.Roughness = 0.15f;
                        foreach (var host in mat.Hosts)
                            bodyMeshes.Add((TriangleMesh)host);
                    }
                }
            }

            //Optimize  
            foreach (var mesh in car.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                Log.Info(typeof(SampleScenes), $"Optimizing {mesh.Name}");

                if (mesh.Name != "reflect_mirrors.003" && mesh.Name != "reflect_mirror_int.003")
                    XrEngine.MeshOptimizer.Simplify(mesh.Geometry!, 0.4f, 0.005f);

                XrEngine.MeshOptimizer.OptimizeVertexCache(mesh.Geometry!);
                XrEngine.MeshOptimizer.OptimizeOverdraw(mesh.Geometry!, 1.05f);
                XrEngine.MeshOptimizer.OptimizeVertexFetch(mesh.Geometry!);

                if (mesh.Name == "leather_armrest.007")
                {
                    mesh.Materials.Clear();
                    mesh.Materials.Add((Material)leather);
                }
            }

            car.UpdateBounds(true);

            var scale = car.FindByName<Object3D>("body.003")!.Transform.Matrix;

            //Simulation
            var model = new CarModel
            {
                WheelFL = car.GroupByName("wheel.Ft.L.003", "wheelbrake.Ft.L.003"),
                WheelFR = car.GroupByName("wheel.Ft.R.003", "wheelbrake.Ft.R.003"),
                WheelBL = car.GroupByName("wheel.Bk.L.003", "wheelbrake.Bk.R.003"),
                WheelBR = car.GroupByName("wheel.Bk.R.003", "wheelbrake.Bk.R.001"),
                CarBody = car.GroupByName("body.003"),
                SteeringWheel = car.GroupByName("leatherB_steering.003", "chrome_steering.003", "chrome_logo_steering.003", "texInt_steering.003"),
                CarBodyCollisionMeshes = bodyMeshes,
                UseSteeringPhysics = false,
                GearBoxPose = new Pose3()
                {
                    Position = new Vector3(-0.1f, 0.61f, 0.1f),
                    Orientation = Quaternion.Normalize(new Quaternion(1, 0f, 0f, 0.82f))
                },
                SeatLocalPose = new Pose3
                {
                    Position = new Vector3(-0.4f, 1.1f, 0.2f),
                    Orientation = Quaternion.Identity
                },
                SteeringLocalPose = new Pose3
                {
                    Position = new Vector3(-0.428f, 0.926f, -0.062f),
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -19f / 180 * MathF.PI)
                },
            };

            var mirror = car.FindByName<TriangleMesh>("plasticInt_mirror_int.003")!;

            var splitter = new MeshSplitter(mirror)
            {
                SplittedName = "plasticInt_mirror_int_body-mirror",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 97, 100),
                Origin = new Vector3(24, -38, -42f)
            };

            splitter.ExecuteSplit();

            var mainBody = (TriangleMesh)((Group3D)((Group3D)model.CarBody).Children[0]).Children[0];

            splitter = new MeshSplitter(mainBody)
            {
                SplittedName = "mirror_left",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 150, 230),
                Origin = new Vector3(-1028, 470, -390)
            };
            splitter.ExecuteSplit();

            splitter = new MeshSplitter(mainBody)
            {
                SplittedName = "mirror_right",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(300, 150, 230),
                Origin = new Vector3(940.2f, 470, -390)
            };
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("plastic_mirrors.003")!;
            splitter = new MeshSplitter(mirror)
            {
                SplittedName = "plastic_mirrors_right",
                FullIntersection = true,
                Orientation = new Quaternion(0, 0, 0, 1),
                Bounds = new Vector3(1000, 1000, 1000),
                Origin = new Vector3(1000, 0, -500)
            };
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("glassClear_mirrors.003")!;
            splitter.SplittedName = "glassClear_mirrors_right";
            splitter.Attach(mirror);
            splitter.ExecuteSplit();

            mirror = car.FindByName<TriangleMesh>("reflect_mirrors.003")!;
            splitter.SplittedName = "reflect_mirrors_right";
            splitter.Attach(mirror);
            splitter.ExecuteSplit();

            model.AddMirror(car.GroupByName(scale, "reflect_mirror_int.003", "plasticInt_mirror_int_body-mirror"),
                new Ray3(new Vector3(-50, 640, -172), new Vector3(0, 0, 1)));
            model.AddMirror(car.GroupByName(scale, "reflect_mirrors.003", "glassClear_mirrors.003", "plastic_mirrors.003", "mirror_left"),
                new Ray3(new Vector3(-850, 390, -300), new Vector3(1, -0.87f, 0)));
            model.AddMirror(car.GroupByName(scale, "reflect_mirrors_right", "glassClear_mirrors_right", "plastic_mirrors_right", "mirror_right"),
              new Ray3(new Vector3(800, 390, -300), new Vector3(1, 0.87f, 0)));

            car.AddComponent(model);

            var checkerMat = (Material)MaterialFactory.CreatePbr(TextureFactory.CreateChecker());
            var staticMat = new PhysicsMaterialInfo()
            {
                StaticFriction = 1f,
                DynamicFriction = 1f,
                Restitution = 0.2f
            };

            //Floor
            var floor = new TriangleMesh(new Cube3D(new Vector3(20, 0.01f, 20)), checkerMat);
            floor.Name = "floor";
            floor.Transform.SetPositionY(-0.005f);
            floor.Geometry!.ScaleUV(new Vector2(20, 20));
            floor.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat
            });

            //Ramp
            var ramp = new TriangleMesh(new Cube3D(new Vector3(20, 0.01f, 20)), checkerMat);
            ramp.Name = "ramp";
            ramp.SetWorldPoseIfChanged(new Pose3()
            {
                Position = new Vector3(0f, 2.565f, -19.36f),
                Orientation = new Quaternion(0.12981941f, 0f, 0f, 0.99153763f)
            });
            ramp.Geometry!.ScaleUV(new Vector2(20, 20));
            ramp.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat
            });

            //Wall
            var wall = new TriangleMesh(new Cube3D(new Vector3(5, 3, 0.5f)), checkerMat);
            wall.Name = "wall";
            wall.Transform.Position = new Vector3(0, 1.5f, -5f);
            wall.Geometry!.ScaleUV(new Vector2(5, 3));
            wall.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                MaterialInfo = staticMat
            });

            //Add children
            scene.AddChild(floor);
            scene.AddChild(ramp);
            scene.AddChild(wall);
            scene.AddChild(car);

            //Create model
            model.Create();
            model.CarBody.Name = "car-body";

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .SetGlOptions(opt =>
                {
                    opt.UsePlanarReflection = true;
                })
                .ConfigureSampleApp(false)
                .ConfigureApp(a =>
                {
                    a.XrApp.UseLocalSpace = true;
                    model.ConfigureInput(a.Inputs!);

                    //Point light
                    var pl = scene.Descendants<PointLight>().First();
                    pl.IsVisible = true;
                    pl.Specular = new Color(0.1f, 0.1f, 0.1f, 1);
                    pl.Intensity = 1f;
                });
        }

        [Sample("Cube")]
        public static XrEngineAppBuilder CreateCube(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Sphere3D.Default, (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "mesh"
            };

            cube.Transform.SetScale(0.1f);
            cube.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI / 4f);
            cube.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(cube);

            /*
            var quad = new TriangleMesh(new Quad3D(new Vector2(2, 2)), new TextureClipMaterial
            {
                Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/check.png"),
                Alpha = AlphaMode.Opaque,
                Color = new Color(1, 1, 1, 0.7f),
                WriteDepth = false,
                UseDepth = false,
                DoubleSided = true
            });

            app.ActiveScene!.AddChild(quad);

            app.ActiveScene.AddBehavior((_, ctx) =>
            {
                if (XrApp.Current != null && XrApp.Current.IsStarted)
                {
                    var mesh = XrApp.Current.GetVisibilityMask(0, Silk.NET.OpenXR.VisibilityMaskTypeKHR.LineLoopKhr);
                    if (ctx.Scene?.ActiveCamera?.Eyes != null)
                    {
                        var v3 = mesh.Vertices.Select(a => new Vector3(a.X, a.Y, -1)).ToArray();
                        var proj = ctx.Scene.ActiveCamera.Eyes[0].Projection;
                        var projVert = v3.Select(a => a.Project(proj)).ToArray();

                        var builder = new Bounds3Builder();
                        builder.Add(projVert);
                        var bb = builder.Result;
                    }

                }

            });
            */

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }

        [Sample("Animated Cubes")]

        public static XrEngineAppBuilder CreateAnimatedCubes(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var red = new BasicMaterial() { Color = new Color(1, 0, 0) };

            /*
            var data = EtcCompressor.Encode(GetAssetPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data))
            {
                DoubleSided = true
            };

            var panel = new TriangleMesh(Quad3D.Default, text);
            scene.AddChild(panel);
            */

            var cubes = new Group3D();

            for (var y = 0f; y <= 2f; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new TriangleMesh(Cube3D.Default, red);
                    cube.Transform.Scale = new Vector3(0.1f, 0.1f, 0.1f);
                    cube.Transform.Position = new Vector3(x, y + 0.1f, z);

                    cube.AddBehavior((obj, ctx) =>
                    {
                        obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 1), (float)ctx.Time * MathF.PI / 4f);
                    });

                    cube.AddComponent<BoundsGrabbable>();

                    cubes.AddChild(cube, false);
                }
            }

            scene.AddChild(cubes);

            scene.AddChild(new AmbientLight(0.1f));

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }

        public static bool IsEditor => Context.Require<IXrEnginePlatform>().Name == "Editor";

        public static string? DefaultHDR { get; set; }

        public static bool DefaultShowHDR { get; set; }
    }
}
