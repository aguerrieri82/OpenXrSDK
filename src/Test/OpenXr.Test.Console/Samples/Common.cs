using OpenXr.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Samples
{
    public static class Common
    {

        public static EngineApp CreateScene()
        {
            var app = new EngineApp();

            var scene = new Scene();
            scene.ActiveCamera = new PerspectiveCamera() { Far = 50f };

            var material = new StandardMaterial() { Color = new Color(1, 0, 0) };

            for (var y = -1f; y <= 1; y += 0.5f)
            {
                for (var rad = 0f; rad < Math.PI * 2; rad += MathF.PI / 10f)
                {
                    var x = MathF.Sin(rad) * 1;
                    var z = MathF.Cos(rad) * 1;

                    var cube = new Mesh(Cube.Instance, material);
                    cube.Transform.Scale = new Vector3(0.2f, 0.2f, 0.2f);
                    cube.Transform.Position = new Vector3(x, y, z);
                    
                    cube.AddBehavior((obj, ctx) =>
                    {
                        obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)ctx.Time * MathF.PI / 4f);
                    });

                    scene.AddChild(cube);
                }
            }

            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);


            return app;
        }
    }
}
