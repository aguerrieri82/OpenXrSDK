using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
using OpenXr.Engine.Objects;
using System.Numerics;

namespace OpenXr.Samples
{
    public static class Common
    {

        public static EngineApp CreateScene(IAssetManager assets)
        {
            var app = new EngineApp();

            var scene = new Scene();

            scene.ActiveCamera = new PerspectiveCamera() { Far = 50f };

            var red = new StandardMaterial() { Color = new Color(1, 0, 0) };

            var text = new TextureMaterial(Texture2D.FromPvrImage(assets.OpenAsset("TestScreen.pvr")));
            text.DoubleSided = false;


            for (var y = 0f; y <= 2f; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new Mesh(Cube.Instance, red );
                    cube.Transform.Scale = new Vector3(0.1f, 0.1f, 0.1f);
                    cube.Transform.Position = new Vector3(x, y + 0.1f, z);

                    cube.AddBehavior((obj, ctx) =>
                    {
                        obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
                    });

                    scene.AddChild(cube);
                }
            }

            var display = new Mesh(Quad.Instance);
            display.Transform.Scale = new Vector3(1.924f, 1.08f, 0.01f);
            display.Transform.Position = new Vector3(0, 1.2f,- 1.5f);
            //display.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathF.PI);
            display.Name = "display";
            display.AddComponent(new PlaneCollider());

            scene.AddChild(display);

            scene.AddChild(new RayView());

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);


            return app;
        }
    }
}
