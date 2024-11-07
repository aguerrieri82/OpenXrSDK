using CanvasUI;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using RoomDesigner.Game;
using System.Numerics;
using System.Runtime.InteropServices;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.Helpers;
using XrEngine.Objects;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.Services;
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
            pl1.Name = "point-light-1";
            pl1.Transform.Position = new Vector3(0, 2, 0);
            pl1.Intensity = 0.3f;

            var pl2 = scene.AddChild(new PointLight());
            pl2.Name = "point-light-2";
            pl2.Transform.Position = new Vector3(0, -2, 0);
            pl2.Intensity = 0.3f;

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
            {
                Far = 30f,
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
                DefaultHDR = "res://asset/Envs/footprint_court.hdr";
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

        static XrEngineAppBuilder ConfigureSampleApp(this XrEngineAppBuilder builder)
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

            if (!IsEditor)
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
                Density = 10
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
            rigidBody.Material = new PhysicsMaterialInfo();


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
                SpherRadius = 6,
                SphereCenter = new Vector3(0, 1.5f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend,
            };

            var mesh = new TriangleMesh(new Quad3D(new Size2(1, 1)), mat);

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
                        if (!e.XrApp.IsStarted || isLoading || ((DateTime.Now - lastUpdate).TotalSeconds < 1000))
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
                            lastUpdate = DateTime.Now;
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
                SpherRadius = 10f,
                SphereCenter = new Vector3(0, 0.68f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend,
                TextureCenter = [c1u, c2u],
                TextureRadius = [s1u, s2u]
            };

            var mat2 = new TextureMaterial(videoTex);

            var mesh = new TriangleMesh(new Quad3D(new Size2(1, 1)), mat);

            mesh.Transform.SetScale(1.3f);
            mesh.Transform.SetPosition(0, 1f, 0);

            mesh.AddComponent(new VideoTexturePlayer()
            {
                Texture = videoTex,
                //Source = new Uri(GetAssetPath("Fish/fish_new.mp4")),
                //Source = new Uri("rtsp://admin:123@192.168.1.60:8554/live"),
                Source = new Uri("rtsp://admin:123@192.168.1.148:8554/live"),
                //Source = new Uri("rtsp://192.168.1.89:554/videodevice"),
                //Source = new Uri("rtsp://192.168.1.97:554/onvif1"),
                Reader = new RtspVideoReader()
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
                scene.AddComponent(new RayPointerRecorder { PointerName = "RightController" });
                scene.AddComponent<RayPointerPlayer>();
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

        [Sample("Car")]
        public static XrEngineAppBuilder CreateCar(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            scene.AddComponent<PhysicsManager>();

            scene.AddComponent(new InputObjectForce
            {
                InputName = "RightGripPose",
                HandlerName = "RightSqueezeClick",
                HapticName = "RightHaptic",
                Factor = 30
            });

            scene.AddComponent(new InputObjectForce
            {
                InputName = "LeftGripPose",
                HandlerName = "LeftSqueezeClick",
                HapticName = "LeftHaptic",
                Factor = 30
            });

            var car = (Group3D)GltfLoader.LoadFile(GetAssetPath("car.glb"), GltfOptions, GetAssetPath);
            car.Name = "car";
            //car.AddComponent<BoundsGrabbable>();

            var bodyMeshes = new HashSet<TriangleMesh>();

            foreach (var mat in car.DescendantsOrSelf().OfType<TriangleMesh>().SelectMany(a => a.Materials).Distinct())
            {
                if (mat is IPbrMaterial pbr)
                {
                    if (mat.Name!.Contains("glass"))
                    {
                        pbr.Color = "#00000030";
                        pbr.Alpha = AlphaMode.Blend;
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

            var model = new CarModel
            {
                WheelFL = car.GroupByName("wheel.Ft.L.003", "wheelbrake.Ft.L.003"),
                WheelFR = car.GroupByName("wheel.Ft.R.003", "wheelbrake.Ft.R.003"),
                WheelBL = car.GroupByName("wheel.Bk.L.003", "wheelbrake.Bk.R.003"),
                WheelBR = car.GroupByName("wheel.Bk.R.003", "wheelbrake.Bk.R.001"),
                CarBody = car.GroupByName("body.003"),
                CarBodyCollisionMeshes = bodyMeshes,
                AccInputName = "RightTriggerValue",
                SeatLocalPose = new Pose3
                {
                    Position = new Vector3(-0.4f, 1.1f, 0.2f),
                    Orientation = Quaternion.Identity
                },
                SteeringLocalPose = new Pose3
                {
                    Position = new Vector3(-0.43f, 0.885f, -0.08f),
                    Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -19f / 180 * MathF.PI)
                },
                SteeringWheel = car.GroupByName("leatherB_steering.003", "chrome_steering.003", "chrome_logo_steering.003", "texInt_steering.003")
            };

            model.CarBody.Name = "car-body";

            car.UpdateBounds(true);
            car.AddComponent(model);

            foreach (var mesh in car.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                Log.Info(typeof(SampleScenes), $"Optimizing {mesh.Name}");
                XrEngine.MeshOptimizer.Simplify(mesh.Geometry!, 0.4f, 0.005f);
                XrEngine.MeshOptimizer.OptimizeVertexCache(mesh.Geometry!);
                XrEngine.MeshOptimizer.OptimizeOverdraw(mesh.Geometry!, 1.05f);
                XrEngine.MeshOptimizer.OptimizeVertexFetch(mesh.Geometry!);
            }

            var checkerMat = (Material)MaterialFactory.CreatePbr(TextureFactory.CreateChecker());

            /*
            var cube = new TriangleMesh(new Cube3D(new Vector3(1, 0.2f, 1)), checkerMat);
            cube.Geometry!.ScaleUV(new Vector2(2, 2)); 
            cube.AddComponent<BoxCollider>();
            
            var rb1 = cube.AddComponent<RigidBody>();
            rb1.Type = PhysicsActorType.Static;
            rb1.AutoTeleport = true;

     
            var steer = new MeshBuilder().AddRevolve(new Circle2D
            {
                Center = new Vector2(0.4f, 0), 
                Radius = 0.05f
            }, 40).ToGeometry();


            var steerMesh = new TriangleMesh(steer, checkerMat);
            steerMesh.Transform.SetPositionY(0.35f);
            steerMesh.AddComponent<PyMeshCollider>();
            var rb2 = steerMesh.AddComponent<RigidBody>();
            rb2.Type = PhysicsActorType.Dynamic;
            rb2.AutoTeleport = true;
            rb2.AngularDamping = 5f;

            var grp = new Group3D();
            var scale = 0.4f;
            grp.AddChild(steerMesh);
            grp.AddChild(cube);
            grp.Transform.SetPositionY(0.78f);
            grp.Transform.SetScale(scale);
            grp.Transform.Rotation = new Vector3(0, -74, -46) / 180 * MathF.PI;
            */

            var floor = new TriangleMesh(new Cube3D(new Vector3(20, 0.01f, 20)), checkerMat);
            floor.Name = "foor";
            floor.Transform.SetPositionY(-0.005f);
            floor.Geometry!.ScaleUV(new Vector2(20, 20));
            floor.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                Material = new PhysicsMaterialInfo()
                {
                    StaticFriction = 1f,
                    DynamicFriction = 1f,
                    Restitution = 0.2f
                }
            });

            var wall = new TriangleMesh(new Cube3D(new Vector3(5, 3, 0.5f)), checkerMat);
            wall.Name = "wall";
            wall.Transform.Position = new Vector3(0, 1.5f, -5f);
            wall.Geometry!.ScaleUV(new Vector2(5, 3));
            wall.AddComponent(new RigidBody
            {
                Type = PhysicsActorType.Static,
                Material = new PhysicsMaterialInfo()
                {
                    StaticFriction = 1f,
                    DynamicFriction = 1f,
                    Restitution = 0.2f
                }
            });

            scene.AddChild(floor);
            scene.AddChild(wall);
            scene.AddChild(car);
            //scene.AddChild(grp);

            model.Create();
            model.CarBody!.IsVisible = false;

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureApp(a =>
                {
                    a.XrApp.UseLocalSpace = true;

                    /*
                    var manager = a.App.ActiveScene!.Component<PhysicsManager>();
                    var pose1 = new Pose3
                    {
                        Position = new Vector3(0, 0.1f, 0),
                        Orientation = Vector3.UnitX.RotationTowards(Vector3.UnitY)
                    };
                    var pose2 = new Pose3
                    {
                        Position = new Vector3(0, -0.125f, 0),
                        Orientation = pose1.Orientation
                    };
                    
                    var joint = manager.AddJoint(JointType.Revolute, cube, pose1, steerMesh, pose2);
                    joint.Damping = 100;
                    joint.Stiffness = 1000;
                    joint.BounceThreshold = 100;

                    scale = 0.01f;
                    joint.InvMassScale1 = scale;
                    joint.InvMassScale0 = scale;
                    joint.InvInertiaScale1 = scale;
                    joint.InvInertiaScale0 = scale;
                    */
                })

                .ConfigureSampleApp();
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
