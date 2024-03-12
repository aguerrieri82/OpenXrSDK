using OpenXr.Framework.Oculus;
using PhysX;
using PhysX.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Components;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.Physics;
using XrEngine.UI;
using XrMath;


namespace Xr.Test
{
    public static class SampleScenes
    {
        static readonly GltfLoaderOptions GltfOptions = new()
        {
            ConvertColorTextureSRgb = true,
        };

        public static void ConfigureXrApp(XrEngineAppBuilder builder)
        {
            builder.UseHands()
                   .UseLeftController()
                   .UseRightController()
                   .UseInputs<XrOculusTouchController>(a => a.AddAction(b => b.Right!.Haptic))
                   .UsePhysics()
                   .UseScene(true)
                   .AddPassthrough()
                   .UseRayCollider()
                   .UseGrabbers();
        }

        static EngineApp CreateBaseScene()
        {
            var app = new EngineApp();

            var scene = new Scene();

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

        public static Panel3D CreateDebugPanel()
        {
            var panel = new Panel3D();
            
            panel.SetInches(19, 16f / 9f);
            panel.DpiScale = 3;

            panel.Panel = new DebugPanel();

            panel.WorldPosition = new Vector3(0, 1, -2);

            panel.AddComponent(new FollowCamera() { Offset = new Vector3(0, 0, -1) });

            return panel;
        }

        public static EngineApp CreateDisplay()
        {
            var assets = Platform.Current!.AssetManager;

            var app = CreateBaseScene();

            var display = new TriangleMesh(Quad3D.Instance);
            //display.Materials.Add(new StandardMaterial { Color = Color.White, DoubleSided = false, WriteDepth = false });

            display.Name = "display";

            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);

            display.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(display);

            return app;
        }

        public static EngineApp CreatePingPong()
        {
            var assets = Platform.Current!.AssetManager;

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            assets.GetFsPath("ping-pong paddle red.glb");

            var mesh = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("ping-pong paddle red.glb"), assets, GltfOptions);
            mesh.Name = "Racket";

            //Reposition
            mesh.Transform.LocalPivot = new Vector3(0.054f, -0.04f, 0.174f);
            mesh.Transform.Update();
            mesh.Transform.Rotation = new Vector3(-0.863f, -0.21f, -1.25f);
            mesh.Transform.Position = Vector3.Zero;

            mesh.Transform.Update();

            foreach (var geo in mesh.DescendantsWithFeature<Geometry3D>())
                geo.Feature.ApplyTransform(mesh.Transform.Matrix);

            mesh.Transform.Reset();
            mesh.Transform.Position = new Vector3(0, 1, 0);


            //Audio
            var audio = scene.Component<AudioSystem>();
            var sound = new DynamicSound();
            sound.AddBuffers(audio.Device.Al, Platform.Current.AssetManager, "BallSounds");

            //Grabber
            mesh.AddComponent<BoundsGrabbable>();

            //Colliders
            foreach (var item in mesh.DescendantsWithFeature<TriangleMesh>())
                mesh.AddComponent(new MeshCollider(item.Feature.Geometry!));

            //Rigid body
            var rigidBody = mesh.AddComponent<RigidBody>();
            rigidBody.Type = PhysicsActorType.Kinematic;
            rigidBody.Tolerance = 100; //1cm
            rigidBody.Material = new PhysicsMaterialInfo
            {
                Restitution = 0.8f,
                DynamicFriction = 0.7f,
                StaticFriction = 0.7f
            };

            //Ball generator
            var bg = scene!.AddComponent(new BallGenerator(sound, 5f));

            //Sample ball
            var ball = bg.PickBall(new Vector3(-0.5f, 1.1f, 0));

            var ballRigid = ball.Component<RigidBody>();
            ballRigid.Started += (_, _) =>
            {
                ballRigid.DynamicActor.AddForce(new Vector3(0.3f, 0, 0), PxForceMode.Force);
            };

            //Add racket
            scene!.AddChild(mesh);

            //Setup camera
            ((PerspectiveCamera)scene.ActiveCamera!).Target = mesh.Transform.Position;

            //Debug
            scene.AddChild(CreateDebugPanel());

            return app;
        }



        public static EngineApp CreateChess()
        {

            var assets = Platform.Current!.AssetManager;

            var app = CreateBaseScene();

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

            app.ActiveScene!.AddChild(mesh);
            ((PerspectiveCamera)app.ActiveScene!.ActiveCamera!).Target = mesh.Transform.Position;


            return app;
        }

        public static EngineApp CreateSponza()
        {
            var assets = Platform.Current!.AssetManager;

            var app = CreateBaseScene();

            assets.GetFsPath("Sponza/Sponza.bin");

            var mesh = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("Sponza/Sponza.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            app.ActiveScene!.AddChild(mesh);

            app.ActiveScene!.AddChild(new SunLight()
            {
                Name = "sun-light-2",
                Intensity = 1.5f,
                Direction = new Vector3(0.1f, -0.9f, 0.15f).Normalize(),
                IsVisible = true
            });

            return app;
        }

        public static EngineApp CreateRoom()
        {
            var assets = Platform.Current!.AssetManager;

            var app = CreateBaseScene();

            var mesh = (Group3D)GltfLoader.Instance.Load(assets.GetFsPath("Sponza/Sponza.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            app.ActiveScene!.AddChild(mesh);

            return app;
        }

        public static EngineApp CreateCube(IAssetManager assets)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Cube3D.Instance, new PbrMaterial() { Color = new Color(1f, 0, 0, 1) });

            cube.Name = "mesh";
            cube.Transform.SetScale(0.1f);
            cube.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI / 4f);
            cube.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(cube);

            return app;
        }


        public static EngineApp CreateDefaultScene(IAssetManager assets)
        {
            var app = new EngineApp();

            var scene = new Scene();

            scene.ActiveCamera = new PerspectiveCamera()
            {
                Far = 50f
            };

            ((PerspectiveCamera)(scene.ActiveCamera))!.LookAt(new Vector3(0, 0, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));


            var red = new StandardMaterial() { Color = new Color(1, 0, 0) };

            var data = EtcCompressor.Encode(assets.GetFsPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data))
            {
                DoubleSided = true
            };

            var display = new TriangleMesh(Quad3D.Instance);
            display.Materials.Add(text);
            display.Transform.Scale = new Vector3(1.924f * 0.5f, 0.01f, 1.08f * 0.5f);
            display.Transform.Position = new Vector3(0f, 0.5f, 0f);
            display.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation =
                Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);

            });

            display.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);
            display.Name = "display";
            display.AddComponent<MeshCollider>();


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

            //scene.AddChild(display);

            scene.AddChild(new AmbientLight(0.1f));

            app.OpenScene(scene);

            return app;
        }
    }
}
