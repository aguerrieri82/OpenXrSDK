using SkiaSharp;
using System.Numerics;
using System.Text.Json;
using Xr.Engine;
using Xr.Engine.Compression;
using Xr.Engine.Gltf;
using Xr.Engine.OpenXr;
using static System.Net.Mime.MediaTypeNames;

namespace OpenXr.Samples
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

            var scene = new Scene();

            scene.AddChild(new SunLight()
            {
                Name = "light",
                Intensity = 1.5f,
                Direction = new Vector3(-0.1f, -0.9f, -0.15f).Normalize(),
                IsVisible = true
            });

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

        public static EngineApp CreateDisplay(IAssetManager assets)
        {
            var app = CreateBaseScene();

            var display = new TriangleMesh(Quad.Instance);
            //display.Materials.Add(new StandardMaterial { Color = Color.White, DoubleSided = false, WriteDepth = false });

            display.Name = "display";

            var trans = Matrix4x4.CreateScale(0.5f) *
                        Matrix4x4.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);

            display.Geometry!.ApplyTransform(trans);

            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);

            display.AddComponent<MeshCollider>();

            app.ActiveScene!.AddChild(display);

            return app;
        }


        public static EngineApp CreateChess(IAssetManager assets)
        {
            var app = CreateBaseScene();

            assets.FullPath("Game/ABeautifulGame.bin");

            var mesh = (Group)GltfLoader.Instance.Load(assets.FullPath("Game/ABeautifulGame.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";

            app.ActiveScene!.AddChild(new PointLight()).Transform.Position = new Vector3(0, 2, 0);

            foreach (var child in mesh.Children.OfType<TriangleMesh>())
            {
                child.AddComponent<BoundsGrabbable>();
            }
        
            mesh.Transform.SetScale(1f);
            mesh.Transform.Position = new Vector3(0, 1, 0);

            app.ActiveScene!.AddChild(mesh);
            ((PerspectiveCamera)app.ActiveScene!.ActiveCamera!).Target = mesh.Transform.Position;

            return app;
        }

        public static EngineApp CreateSponza(IAssetManager assets)
        {
            var app = CreateBaseScene();

            assets.FullPath("Sponza/Sponza.bin");

            var mesh = (Group)GltfLoader.Instance.Load(assets.FullPath("Sponza/Sponza.gltf"), assets, GltfOptions);
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

        public static EngineApp CreateRoom(IAssetManager assets)
        {
            var app = CreateBaseScene();

            var mesh = (Group)GltfLoader.Instance.Load(assets.FullPath("Sponza/Sponza.gltf"), assets, GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.01f);

            app.ActiveScene!.AddChild(mesh);

            return app;
        }

        public static EngineApp CreateCube(IAssetManager assets)
        {
            var app = CreateBaseScene();

            var cube = new TriangleMesh(Cube.Instance, new PbrMaterial() { Color = new Color(1f, 0, 0, 1) });

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

            var data = EtcCompressor.Encode(assets.FullPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data))
            {
                DoubleSided = true
            };

            var display = new TriangleMesh(Quad.Instance);
            display.Materials.Add(text);
            display.Transform.Scale = new Vector3(1.924f * 0.5f, 0.01f, 1.08f * 0.5f);
            display.Transform.Position = new Vector3(0f, 0.5f, 0f);
            display.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation =
                Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f) *
                Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);

            });

            display.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);
            display.Name = "display";
            display.AddComponent<MeshCollider>();


            var cubes = new Group();

            for (var y = 0f; y <= 2f; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new TriangleMesh(Cube.Instance, red);
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
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);

            return app;
        }
    }
}
