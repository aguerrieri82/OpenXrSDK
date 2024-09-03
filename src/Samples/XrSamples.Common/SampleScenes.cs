using CanvasUI;
using FFmpeg.AutoGen;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.Services;
using XrEngine.UI;
using XrEngine.Video;
using XrMath;



namespace XrSamples
{
    public static class SampleScenes
    {
        static readonly GltfLoaderOptions GltfOptions = new()
        {
            ConvertColorTextureSRgb = true,
        };

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
                Far = 50f,
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

                scene.PerspectiveCamera().Exposure = 0.5f;

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

        public static XrEngineAppBuilder AddPanel(this XrEngineAppBuilder builder, UIRoot uiRoot)
        {
            var panel = new Window3D();

            panel.Size = new Size2(0.8f, 0.5f);
            panel.DpiScale = 1.6f;

            panel.Content = uiRoot;
            panel.WorldPosition = new Vector3(0, 1, 0);

            //panel.AddComponent(new FollowCamera() { Offset = new Vector3(0f, -0.0f, -1f) });

            return builder
                .AddRightPointer()
                .ConfigureApp(e =>
                {
                    e.App.ActiveScene!.AddChild(panel);

                    if (RuntimeInformation.RuntimeIdentifier.StartsWith("android"))
                        panel.CreateOverlay(e.XrApp);

                    e.App.ActiveScene.AddBehavior((_, _) =>
                    {
                        var click = ((XrOculusTouchController)e.Inputs!).Right!.Button!.AClick!;
                        if (click.IsChanged && click.Value)
                        {
                            panel.WorldPosition = panel.Scene!.ActiveCamera!.WorldPosition + panel.Scene.ActiveCamera.Forward * 0.5f;
                            panel.Transform.Orientation = panel.Scene!.ActiveCamera!.Transform.Orientation;
                        }
                    });
                });
        }

        public static XrEngineAppBuilder AddPanel<T>(this XrEngineAppBuilder builder) where T : UIRoot, new()
        {
            return builder.AddPanel(new T());
        }

        static XrEngineAppBuilder ConfigureSampleApp(this XrEngineAppBuilder builder)
        {
            return builder.UseHands()
                   .UseLeftController()
                   .UseRightController()
                   .UseInputs<XrOculusTouchController>(a => a.AddAction(b => b.Right!.Haptic))
                   .AddPassthrough()
                   .UseRayCollider()
                   .UseGrabbers();
        }

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
                          .ConfigureApp(e =>
                          {
                              display.AddBehavior((_, _) =>
                              {
                                  var click = ((XrOculusTouchController)e.Inputs!).Right!.Button!.AClick!;
                                  if (click.IsChanged && click.Value)
                                  {
                                      display.WorldPosition = scene.ActiveCamera!.WorldPosition + scene.ActiveCamera.Forward * 0.5f;
                                      display.Transform.Orientation = scene.ActiveCamera!.Transform.Orientation;
                                  }
                              });
                          })
                          .ConfigureSampleApp();
        }

        public static string GetAssetPath(string name)
        {
            return Context.Require<IAssetStore>().GetPath(name);    
        }

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
            var bg = scene!.AddComponent(new BallGenerator(sound, 5f));
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
                   .UseSceneModel(true)
                   .ConfigureSampleApp()
                   .UseEnvironmentHDR("res://asset/Envs/lightroom_14b.hdr", false)
                   .UsePhysics(new PhysicsOptions
                   {

                   })
                   .AddPanel(new PingPongSettingsPanel(settings, scene))
                   .ConfigureApp(app => settings.Apply(app.App.ActiveScene!));

        }

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
                    .UseEnvironmentHDR("res://asset/Envs/pisa.hdr", false)
                    .UsePhysics(new PhysicsOptions());
        }

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
            
            var mesh = new TriangleMesh(new Quad3D(new Size2(1,1)), mat);

            mesh.Name = "mesh";

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .ConfigureSampleApp()
                //.AddPanel(new PortalSettingsPanel(settings, scene))
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

        public static XrEngineAppBuilder CreatePortalVideo(this XrEngineAppBuilder builder)
        {
            var settings = new PortalSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "portal_settings.json"));

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var videoTex = new Texture2D
            {
                Format = TextureFormat.Rgb24,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
            };


            var mat = new FishReflectionSphereMaterial(videoTex, FishReflectionMode.Stereo)
            {
                SpherRadius = 10f,
                SphereCenter = new Vector3(0, 0.68f, 0),
                Border = 0.1f,
                SurfaceSize = new Vector2(1.3f, 1.3f),
                Alpha = AlphaMode.Blend
            };

            var mesh = new TriangleMesh(new Quad3D(new Size2(1, 1)), mat);

            mesh.Transform.SetScale(1.3f);
            mesh.Transform.SetPosition(1.26f, 1.18f, 0.84f);
            mesh.Transform.Rotation = new Vector3(0, -MathF.PI / 2, 0);


            mesh.AddComponent(new VideoTexturePlayer()
            {
                Texture = videoTex,
                SrcFileName = GetAssetPath("Fish/20240308151616.mp4")
            });

            mesh.Name = "mesh";

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .ConfigureSampleApp()
                .AddPanel(new PortalSettingsPanel(settings, scene))
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
                    if (mat is PbrMaterial pbr && pbr.MetallicRoughness != null && pbr.MetallicRoughness.RoughnessFactor == 0.2f)
                    {
                        //pbr.MetallicRoughness.RoughnessFactor = 0.2f;
                        // pbr.MetallicRoughness.MetallicFactor = 0f;
                        //pbr.MetallicRoughness.MetallicRoughnessTexture = null;
                    }

                }
            }

            scene.AddChild(mesh);

            scene.PerspectiveCamera().Target = mesh.Transform.Position;
            scene.PerspectiveCamera().Transform.Position = new Vector3(0.2f, 1.4f, 0.2f);
            return builder
                .UseApp(app)
                .UseEnvironmentHDR("Envs/lightroom_14b.hdr")
                .ConfigureSampleApp();
        }


        public static XrEngineAppBuilder CreateBed(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = (TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"), GltfOptions);
            mesh.Name = "mesh";
            // mesh.AddComponent<MeshCollider>();
            mesh.AddComponent<BoundsGrabbable>();

            foreach (var mat in mesh.Materials)
            {
                if (mat is PbrMaterial pbr)
                {
                    //pbr.MetallicRoughness.BaseColorFactor = new Color(0.3f, 0.3f, 0.3f);
                    //pbr.MetallicRoughness.RoughnessFactor = 0.2f;
                    // pbr.MetallicRoughness.MetallicFactor = 0f;
                    //pbr.MetallicRoughness.MetallicRoughnessTexture = null;
                }
            }

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseSceneModel(false, false)
                .UseEnvironmentHDR("res://asset/Envs/lightroom_14b.hdr")
                .ConfigureSampleApp();
        }



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


            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                //.UseEnvironmentHDR("res://asset/Envs/CameraEnv.jpg")
                .UseEnvironmentHDR("res://asset/Envs/lightroom_14b.hdr")
                .ConfigureSampleApp();
        }


        public static XrEngineAppBuilder CreateCube(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Cube3D.Default, PbrMaterial.CreateDefault(new Color(1f, 0, 0, 1)))
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
    }
}
