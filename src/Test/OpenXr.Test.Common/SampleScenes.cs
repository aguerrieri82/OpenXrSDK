using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
using System.Numerics;
using Xr.Engine.Compression;
using Xr.Engine.Gltf;
using Xr.Engine.OpenXr;

namespace OpenXr.Samples
{
    public static class SampleScenes
    {
        public static EngineApp CreateSimpleScene(IAssetManager assets)
        {
            var app = new EngineApp();

            var scene = new Scene();

            var cube = new Mesh(Cube.Instance, new StandardMaterial() { Color = new Color(1f, 0, 0, 1) });
            //cube.Transform.Pivot = new Vector3(0, -1, 0);
            cube.Transform.SetScale(0.1f);
            //cube.Transform.SetPositionX(0.5f);
            cube.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), MathF.PI /4f);
            cube.Name = "cube";

            //var quod = new Mesh(Quad.Instance, new StandardMaterial() { Color = new Color(1f, 0, 0, 1) });

            var quod = new Mesh(Quad.Instance, new DepthViewMaterial());
            quod.Transform.SetScale(0.5f);
            quod.Name = "quad";
            quod.IsVisible = false;
            scene.AddChild(quod);

            var contanier = new Group();
            contanier.Transform.Position = new Vector3(1f, 0, 0);
            contanier.Transform.SetScale(2);
            contanier.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI / 4f);
            contanier.AddChild(cube, false);

            scene.AddChild(contanier);

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight() { Range = 10, Intensity = 1 }).Transform.Position = new Vector3(0, 10, 10);

            scene.AddChild(new PlaneGrid(6f, 12f, 2f));

            var room = (Group)GltfLoader.Instance.Load(assets.FullPath("769508.glb"), assets);
            var mesh = room.Descendants<Mesh>().First();
            //mesh.Materials[0] = new StandardMaterial() { Color = new Color(1f, 1, 1, 1) };
            scene.AddChild(mesh);

            var camera = new PerspectiveCamera() { Far = 50f };
            camera.BackgroundColor = Color.White;
            camera!.LookAt(new Vector3(3f, 3f, 3f), Vector3.Zero, new Vector3(0, 1, 0));

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
            //cubes.Transform.SetScale(0.5f);
            //cubes.Transform.SetPositionX(0.5f);

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
            display.Transform.Scale = new Vector3(1.924f * 0.5f,  0.01f, 1.08f * 0.5f);
            display.Transform.Position = new Vector3(0f, 0.5f, 0f);
            display.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation = 
                Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f)*
                Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);

            });
            display.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(1f, 0, 0), MathF.PI / 2);
            display.Name = "display";
            display.AddComponent<MeshCollider>();

            scene.AddChild(display);

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);


            var room = (Group)GltfLoader.Instance.Load(assets.FullPath("769508.glb"), assets);
            var mesh = room.Descendants<Mesh>().First();
            //mesh.Materials[0] = new StandardMaterial() { Color = new Color(1f, 1, 1, 1) };
            scene.AddChild(mesh);

            app.OpenScene(scene);

            return app;
        }
    }
}
