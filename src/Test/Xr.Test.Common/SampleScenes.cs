using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine;
using XrEngine.Audio;
using XrEngine.Components;
using XrEngine.Compression;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrEngine.Physics;
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
                   .UseInputs<XrOculusTouchController>(a=>a.AddAction(b=> b.Right!.Haptic))
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
                Name = "light",
                Intensity = 1.5f,
                Direction = new Vector3(-0.1f, -0.9f, -0.15f).Normalize(),
                IsVisible = true
            });

            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 2, 0);

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera
            {
                Far = 50f,
                Near = 0.01f,
                BackgroundColor = Color.Transparent,
                Exposure = 1
            };

            camera!.LookAt(new Vector3(1, 1.7f, 1), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            scene.ActiveCamera = camera;

            app.OpenScene(scene);

            return app;
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
            var audio = app.ActiveScene!.Component<AudioSystem>();
            var sound = new DynamicSound();
            sound.AddBuffers(audio.Device.Al, Platform.Current.AssetManager, "BallSounds");

            //Grabber
            mesh.AddComponent<BoundsGrabbable>();

            //Colliders
            foreach (var item in mesh.DescendantsWithFeature<TriangleMesh>())
                mesh.AddComponent(new MeshCollider(item.Feature.Geometry!));

            //Rigid body
            var rb = mesh.AddComponent<RigidBody>();
            rb.BodyType = PhysicsActorType.Kinematic;
            rb.Material = new PhysicsMaterialInfo
            {
                Restitution = 0.8f,
                DynamicFriction = 0.7f,
                StaticFriction = 0.7f
            };

            //Ball generator
            var bg = app.ActiveScene!.AddComponent(new BallGenerator(sound));

            //Sample ball
            var ball = bg.PickBall();
            ball.WorldPosition = new Vector3(-0.5f, 1.1f, 0);
            ball.Component<RigidBody>().Started += (s, e) =>
            {
                ball.Component<RigidBody>().Actor.AddForce(new Vector3(0.3f, 0, 0), PxForceMode.Force);
            };


            //Add racket
            app.ActiveScene!.AddChild(mesh);


            //Setup camera
            ((PerspectiveCamera)app.ActiveScene!.ActiveCamera!).Target = mesh.Transform.Position;

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
                    rb.BodyType = PhysicsActorType.Static;
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
                Name = "light2",
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
