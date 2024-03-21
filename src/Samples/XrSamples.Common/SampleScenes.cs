using CanvasUI;
using glTFLoader.Schema;
using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using System.Numerics;
using System.Runtime.InteropServices;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.Materials;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.UI;
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
                Intensity = 1.5f,
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
            var assets = XrPlatform.Current!.AssetManager;

            return builder
      
            .ConfigureApp(e =>
            {
                var scene = e.App.ActiveScene!;

                scene.PerspectiveCamera().Exposure = 0.5f;

                if (showEnv)
                    scene.AddChild<EnvironmentView>();

                var light = scene.AddChild<ImageLight>();
                light.Intensity = 1f;

                foreach (var l in scene.Descendants<Light>())
                {
                    if (l != light)
                        l.IsVisible = false;
                }
                   
                if (assetPath.Contains("pisa"))
                {
                    light.Textures = new PbrMaterial.IBLTextures
                    {
                        MipCount = 10,
                        Env = new TextureCube(PvrTranscoder.Instance.Read(assets.GetFsPath("Pisa/Env.pvr"))),
                        GGXEnv = new TextureCube(PvrTranscoder.Instance.Read(assets.GetFsPath("Pisa/GGX.pvr"))),
                        LambertianEnv = new TextureCube(PvrTranscoder.Instance.Read(assets.GetFsPath("Pisa/Lambertian.pvr"))),
                        GGXLUT = new Texture2D(ImageReader.Instance.Read(assets.GetFsPath("Pisa/GGX.png"))),
                    };
                }
                else
                    light.LoadPanorama(assets.GetFsPath(assetPath));
            });
        }

        public static XrEngineAppBuilder AddPanel(this XrEngineAppBuilder builder, UIRoot uiRoot)
        {
            var panel = new Window3D();

            panel.Size = new Size2(0.8f, 0.6f);
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
            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var display = new TriangleMesh(Quad3D.Instance);
            //display.Materials.Add(new StandardMaterial { Color = Color.White, DoubleSided = false, WriteDepth = false });

            display.Name = "display";

            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);

            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            return builder.UseApp(app)
                          .ConfigureSampleApp();
        }

        public static XrEngineAppBuilder CreatePingPong(this XrEngineAppBuilder builder)
        {
            var settings = new PingPongSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "settings.json"));

            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            assets.GetFsPath("ping-pong paddle red.glb");

            var racket = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("ping-pong paddle red.glb"), assets, GltfOptions);
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
            sound.AddBuffers(audio.Device.Al, XrPlatform.Current.AssetManager, "BallSounds");

            //Grabber
            racket.AddComponent<BoundsGrabbable>();

            //Colliders
            foreach (var item in racket.DescendantsWithFeature<TriangleMesh>())
                racket.AddComponent(new MeshCollider(item.Feature.Geometry!));

            //Rigid body
            var rigidBody = racket.AddComponent<RigidBody>();
            rigidBody.Type = PhysicsActorType.Kinematic;
            //rigidBody.Tolerance = 1; //1cm
            rigidBody.EnableCCD = settings.Racket.EnableCCD;
            rigidBody.LengthToleranceScale = settings.Racket.LengthToleranceScale;
            rigidBody.ContactOffset = settings.Racket.ContactOffset;
            rigidBody.ContactReportThreshold = settings.Racket.ContactReportThreshold;

            rigidBody.Material = new PhysicsMaterialInfo
            {
                Restitution = settings.Racket.Restitution,
                DynamicFriction = 0.7f,
                StaticFriction = 0.7f
            };

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
                   .UseScene(true)
                   .ConfigureSampleApp()
                   .UseEnvironmentHDR("pisa.hdr", false)
                   .UsePhysics(new PhysicsOptions
                   {
                       LengthTolerance = settings.LengthToleranceScale,
                   })
                   .AddPanel(new PingPongSettingsPanel(settings, scene));

        }

        public static XrEngineAppBuilder CreateChess(this XrEngineAppBuilder builder)
        {

            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            assets.GetFsPath("Game/ABeautifulGame.bin");

            var mesh = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("Game/ABeautifulGame.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";


            foreach (var child in mesh.Children)
            {
                var rb = child.AddComponent<RigidBody>();

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

            mesh.Transform.SetScale(1f);
            mesh.Transform.Position = new Vector3(0, 1, 0);

            scene.AddChild(mesh);

            scene.PerspectiveCamera().Target = mesh.Transform.Position;

            return builder
                    .UseApp(app)
                    .ConfigureSampleApp()
                    .UseEnvironmentHDR("pisa.hdr")
                    .UsePhysics(new PhysicsOptions());
        }

        public static XrEngineAppBuilder CreateSponza(this XrEngineAppBuilder builder)
        {
            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            assets.GetFsPath("Sponza/Sponza.bin");

            var mesh = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("Sponza/Sponza.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            scene.AddChild(mesh);

            scene.AddChild(new SunLight()
            {
                Name = "sun-light-2",
                Intensity = 1.5f,
                Direction = new Vector3(0.1f, -0.9f, 0.15f).Normalize(),
                IsVisible = true
            });

            return builder
                .UseApp(app)
                .ConfigureSampleApp();
        }


        public static XrEngineAppBuilder CreateHelmet(this XrEngineAppBuilder builder)
        {
            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = (Object3D)GltfLoader.Instance.Load(assets.GetFsPath("DamagedHelmet.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.4f);
            mesh.Transform.SetPositionY(1);
            mesh.AddComponent<BoundsGrabbable>();

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseEnvironmentHDR("CameraEnv.jpg")
                .ConfigureSampleApp();
        }


        public static XrEngineAppBuilder CreateCube(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Cube3D.Instance, PbrMaterial.CreateDefault(new Color(1f, 0, 0, 1)))
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
            var assets = XrPlatform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var red = new BasicMaterial() { Color = new Color(1, 0, 0) };

            var data = EtcCompressor.Encode(assets.GetFsPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data))
            {
                DoubleSided = true
            };

            var cubes = new Group3D();

            for (var y = 0f; y <= 2f; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new TriangleMesh(Cube3D.Instance, red);
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
