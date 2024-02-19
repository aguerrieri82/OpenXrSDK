using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
using Silk.NET.Maths;
using System.IO;
using System.Numerics;
using Xr.Engine.Compression;
using Xr.Engine.Gltf;
using Xr.Engine.OpenXr;

namespace OpenXr.Samples
{
    public static class Scenes
    {
        public static EngineApp CreateSimpleScene(IAssetManager assets)
        {
            var app = new EngineApp();

            var scene = new Scene();

            var cube = new Mesh(Cube.Instance, new StandardMaterial() { Color = new Color(1f, 0, 0, 1) });
            cube.Transform.Pivot = new Vector3(0, -1, 0);
            cube.Transform.SetScale(0.1f);
            cube.Transform.SetPositionX(0.5f);

            scene.AddChild(cube);

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var camera = new PerspectiveCamera() { Far = 50f };
            camera.BackgroundColor = Color.White;
            camera!.LookAt(new Vector3(2f, 2f, 2f), Vector3.Zero, new Vector3(0, 1, 0));

            scene.ActiveCamera = camera;

            app.OpenScene(scene);

            return app;
        }

        public static EngineApp CreateDefaultScene(IAssetManager assets)
        {
            var app = new EngineApp();

            var scene = new Scene();

            scene.ActiveCamera = new PerspectiveCamera() { Far = 50f };

            var red = new StandardMaterial() { Color = new Color(1, 0, 0) };

            var data = EtcCompressor.Encode(assets.FullPath("TestScreen.png"), 16);

            var text = new TextureMaterial(Texture2D.FromData(data));
            //var text = new TextureMaterial(Texture2D.FromPvrImage(assets.OpenAsset("TestScreen.pvr")));
            //var text = new TextureMaterial(Texture2D.FromImage(assets.OpenAsset("TestScreen.png")));
            text.DoubleSided = true;

            var cubes = new Group();
            cubes.Transform.SetScale(0.5f);
            cubes.Transform.SetPositionX(0.5f);

            for (var y = 0f; y <= 2f; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new Mesh(Cube.Instance, red);
                    cube.Transform.Scale = new Vector3(0.1f, 0.1f, 0.1f);
                    cube.Transform.Position = new Vector3(x, y + 0.1f, z);
                    //cube.Transform.Pivot = new Vector3(-1, -1, -1);

                    cube.AddBehavior((obj, ctx) =>
                    {
                       obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 1), (float)ctx.Time * MathF.PI / 4f);
                    });

                    cube.AddComponent<BoundsGrabbable>();

                    cubes.AddChild(cube, false);
                }
            }


            scene.AddChild(cubes);

            var display = new Mesh(Quad.Instance);
            display.Materials.Add(text);
            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);
            display.Transform.Position = new Vector3(0f, 0.5f, 0f);
            display.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
            });
            //display.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI / 2);
            display.Name = "display";
            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            scene.AddChild((Object3D)GltfLoader.Instance.Load(assets.FullPath("769508.glb"), assets));

            app.OpenScene(scene);

            return app;
        }
    }
}
