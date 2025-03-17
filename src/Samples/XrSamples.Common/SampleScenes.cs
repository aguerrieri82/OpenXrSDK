using CanvasUI;
using DrumsVR.Game;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using RoomDesigner.Game;
using System.Numerics;
using System.Runtime.InteropServices;
using XrEngine;
using XrEngine.AI;
using XrEngine.Audio;
using XrEngine.Components;
using XrEngine.Compression;
using XrEngine.Devices;
using XrEngine.Gltf;
using XrEngine.Helpers;
using XrEngine.Objects;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.UI;
using XrEngine.Video;
using XrMath;
using XrSamples.Components;


#if !ANDROID
using XrEngine.Browser.Win;
using XrEngine.UI.Web;
#endif

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

        public static XrEngineAppBuilder UseDefaultHDR(this XrEngineAppBuilder builder)
        {
            if (DefaultHDR == null)
                DefaultHDR = "res://asset/Envs/StudioTomoco.hdr";
            return builder.UseEnvironmentHDR(DefaultHDR, DefaultShowHDR);
        }

        public static XrEngineAppBuilder UseClickMoveFront(this XrEngineAppBuilder builder, Object3D obj, float distance = 0.5f)
        {
            return builder.ConfigureApp(e =>
            {
                var inputs = e.GetInputs<XrOculusTouchController>();

                obj.AddBehavior((_, _) =>
                {
                    var click = inputs.Right!.Button!.AClick!;
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

        public static XrEngineAppBuilder AddPanel(this XrEngineAppBuilder builder, UIRoot uiRoot)
        {
            var panel = new Window3D();

            panel.Size = new Size2(0.8f, 0.5f);
            panel.DpiScale = 1.6f;
            panel.Content = uiRoot;
            panel.WorldPosition = new Vector3(0, 1, 0);

            return builder
                .UseClickMoveFront(panel, 0.5f)
                .ConfigureApp(e =>
                {
                    e.App.ActiveScene!.AddChild(panel);

                    if (RuntimeInformation.RuntimeIdentifier.StartsWith("android"))
                        panel.CreateOverlay(e.XrApp);

                });
        }

        public static XrEngineAppBuilder AddFloorShadow(this XrEngineAppBuilder builder, float size = 4, bool showDepth = false)
        {
            var floor = new TriangleMesh(new Cube3D(new Vector3(size, 0.01f, size)));
            floor.Name = "Floor";
            floor.Materials.Add(new ShadowOnlyMaterial
            {
                Name = "FloorMaterial",
                ShadowColor = new Color(1f, 0.1f, 0.1f, 0.7f),
            });

            floor.Transform.SetPositionY(-0.01f / 2.0f);

            var mat = new DepthViewMaterial();

            TriangleMesh? depth = null;
            if (showDepth)
            {
                depth = new TriangleMesh(Quad3D.Default, mat);
                depth.Transform.SetPositionY(1);

                depth.Name = "Depth";

                depth.AddBehavior((_, _) =>
                {
                    var sp = ((IShadowMapProvider)depth.Scene!.App!.Renderer!);

                    if (mat.Texture == null)
                    {
                        mat.Texture = sp.ShadowMap;
                        mat.NotifyChanged(ObjectChangeType.Render);
                    }

                    if (mat.Camera == null)
                    {
                        mat.Camera = sp.LightCamera;
                        mat.NotifyChanged(ObjectChangeType.Render);
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

        static XrEngineAppBuilder ConfigureSampleApp(this XrEngineAppBuilder builder, bool usePt = true)
        {
            builder.AddXrRoot()
                   .UseHands()
                   .UseLeftController()
                   .UseRightController()
                   .AddRightPointer()
                   .UseInputs<XrOculusTouchController>(a => a
                       .AddAction(b => b.Right!.Haptic)
                       .AddAction(b => b.Left!.Haptic))
                   .UseRayCollider()
                   .UseGrabbers();

            if (!IsEditor && usePt)
                builder.AddPassthrough();
            return builder;
        }

        public static XrEngineAppBuilder CreateChromeBrowser(this XrEngineAppBuilder builder)
        {
#if !ANDROID
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

            var cube = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr("#ff00000"));
            cube.Transform.SetScale(0.1f);
            cube.AddComponent<BoundsGrabbable>();
            cube.AddComponent<BoxCollider>();
            cube.AddComponent<SpeedTracker>();

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
                    Context.Require<ITimeLogger>().Clear();
                }
            });

            return builder
              .UseApp(app)
              .ConfigureSampleApp()
              .UseDefaultHDR()
              .UsePhysics(new PhysicsOptions
              {

              })
              .AddPanel(new ThrowSettingsPanel(settings, scene))
              .ConfigureApp(app => settings.Apply(app.App.ActiveScene!));
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
                   .UsePhysics(new PhysicsOptions
                   {

                   })
                   .AddPanel(new PingPongSettingsPanel(settings, scene))
                   .ConfigureApp(app => settings.Apply(app.App.ActiveScene!));

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

            var options = new TextureLoadOptions() { Format = TextureFormat.SRgba32 };

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
            };

            var mesh = new TriangleMesh(new Quad3D(), mat);

            mesh.Name = "mesh";

            scene.AddChild(mesh);


            return builder
                .UseApp(app)
                .ConfigureSampleApp()
                //.AddPanel(new PortalSettingsPanel(settings, scene))
                .UseClickMoveFront(mesh)
                .ConfigureApp(e =>
                {
                    var oculus = e.XrApp.Plugin<OculusXrPlugin>();
                    var isLoading = false;
                    DateTime lastUpdate = new DateTime();
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

                                    pos.X += 0.16f;
                                    pos.Z += 0.05f;
                                    pos.Y -= 0.05f;

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

            var videoTex = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
            };

            if (OperatingSystem.IsAndroid())
                videoTex.Type = TextureType.External;

            var mat = new FishReflectionSphereMaterial(videoTex, FishReflectionMode.Stereo)
            {
                SphereRadius = 10f,
                SphereCenter = new Vector3(0, 0.68f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend,
                TextureCenter = [c1u, c2u],
                TextureRadius = [s1u, s2u]
            };

            var mat2 = new TextureMaterial(videoTex);

            var mesh = new TriangleMesh(new Quad3D(), mat);

            mesh.Transform.SetScale(1.3f);
            mesh.Transform.SetPosition(0, 1f, 0);

            mesh.AddComponent(new VideoTexturePlayer()
            {
                Texture = videoTex,
                Source = new Uri(GetAssetPath("Fish/bandicam 2025-01-21 00-44-12-973.mp4")),
                //Source = new Uri("rtsp://admin:123@192.168.1.60:8554/live"),
                //Source = new Uri("rtsp://admin:123@192.168.1.148:8554/live"),
                //Source = new Uri("rtsp://192.168.1.89:554/videodevice"),
                //Source = new Uri("rtsp://192.168.1.97:554/onvif1"),
                //Reader = new RtspVideoReader()
            });

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

                        var loc = e.XrApp.LocateSpace(new Silk.NET.OpenXR.Space(window.Space), e.XrApp.ReferenceSpace, e.XrApp.FramePredictedDisplayTime);
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

            var mesh2 = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"),
                new GltfLoaderOptions { PbrType = typeof(PbrV1Material) });

            mesh2.Name = "Bed 2";
            mesh2.WorldPosition = new Vector3(3, 0, 0);
            mesh2.AddComponent<PyMeshCollider>();
            mesh2.AddComponent<BoundsGrabbable>();

            foreach (var material in mesh.Materials!)
            {
                material.CastShadows = true;
                material.WriteStencil = 1;
            }

            scene.AddChild(mesh);
            scene.AddChild(mesh2);


            return builder
                .UseApp(app)
                //.UseSceneModel(false, false)
                .UseDefaultHDR()
                .AddFloorShadow(4, true)
                .UsePhysics(new PhysicsOptions())
                .ConfigureSampleApp();
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
                .RemovePlaneGrid()
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
                .AddPassthrough()

            .ConfigureApp(app =>
            {
                var scene = (RoomScene)app.App.ActiveScene!;
                scene.Id = Guid.Parse("5ae3f2c6-ae6b-4c57-a885-26dc8fc9fa89");

                scene.AddComponent<DebugGizmos>();
                scene.AddComponent<XrInputRecorder>();
                scene.AddComponent<XrInputPlayer>();
                scene.AddChild(new PlaneGrid(6f, 12f, 2f));

                var ui = scene.UiPanel!;

#if !ANDROID
                var webView = new ChromeWebBrowserView
                {
                    Size = new Size2I((uint)(ui.Transform.Scale.X * 1700), (uint)(ui.Transform.Scale.Y * 1700)),
                    ZoomLevel = 0,
                    RequestHandler = new FsWebRequestHandler("main", Context.Require<RoomDesignerApp>().Settings.UiBaseUri)
                };

                ui.AddComponent<SurfaceController>();
                ui.AddComponent(webView);

                Context.Require<RoomDesignerApp>().SetUIBrowser(webView.Browser);
#endif
            });

            return builder;
        }


        [Sample("CreateDrums")]
        public static XrEngineAppBuilder CreateDrums(this XrEngineAppBuilder builder)
        {
#if WINDOWS
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets")); ;
            Context.Implement<IBleManager>(() => new XrEngine.Devices.Windows.WinBleManager());
#else
            Context.Implement<IBleManager>(() => new XrEngine.Devices.Android.AndroidBleManager());
#endif
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


                var ui = scene.UiPanel!;

#if !__ANDROID__
                var webView = new ChromeWebBrowserView
                {
                    Size = new Size2I((uint)(ui.Transform.Scale.X * 1700), (uint)(ui.Transform.Scale.Y * 1700)),
                    ZoomLevel = 0,
                    RequestHandler = new FsWebRequestHandler("main", drumApp.Settings.UiBaseUri)
                };

                ui.AddComponent<SurfaceController>();
                ui.AddComponent(webView);

                drumApp.SetUIBrowser(webView.Browser);
#endif
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
            mesh.UseEnvDepth(true);


            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentDepth()
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }

        public static Material LoadMaterial(string url)
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

            bool isInit = false;

            window.AddBehavior((a, b) =>
            {
                if (!isInit && window.ActiveTexture != null)
                {
                    mat.Texture = window.ActiveTexture;
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
            var depth = points.AddComponent(new DepthScanner
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
                    NormalStrength = new Vector3(20,20,1),
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

            var cube = new TriangleMesh(Cube3D.Default, (Material)MaterialFactory.CreatePbr(new Color(1f, 0, 0, 1)))
            {
                Name = "mesh"
            };

            cube.Transform.SetScale(0.1f);
            cube.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI / 4f);
            cube.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(cube);

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

            var data = EtcCompressor.Encode(GetAssetPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data))
            {
                DoubleSided = true
            };

            var panel = new TriangleMesh(Quad3D.Default, text);
            scene.AddChild(panel);

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
