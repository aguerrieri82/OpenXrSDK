using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
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
            //scene.ActiveCamera.BackgroundColor = Color.White;

            var red = new StandardMaterial() { Color = new Color(1, 0, 0) };

            var text = new TextureMaterial(Texture2D.FromKtxImage(assets.OpenAsset("TestScreen.KTX")));
            text.DoubleSided = false;



            for (var y = -1f; y <= 1; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new Mesh(Cube.Instance, text);
                    cube.Transform.Scale = new Vector3(0.1f, 0.1f, 0.1f);
                    cube.Transform.Position = new Vector3(x, y, z);

                    cube.AddBehavior((obj, ctx) =>
                    {
                        obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
                    });

                    scene.AddChild(cube);
                }

             
            }

       
            var display = new Mesh(Quad.Instance, red);
            display.Transform.Scale = new Vector3(2, 2, 2);
            display.Materials[0].DoubleSided = true;
           
            scene.AddChild(display);
        
            display.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
            });
       

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);


            return app;
        }
    }
}
