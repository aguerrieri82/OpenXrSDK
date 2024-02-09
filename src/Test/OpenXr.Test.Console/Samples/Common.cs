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
            scene.ActiveCamera = new PerspectiveCamera() { Far = 5f };

            var cube = new Mesh(Cube.Instance, new StandardMaterial() { Color = new Color(1, 0, 0) });
            cube.Transform.Scale = new Vector3(1, 1, 1);
            cube.AddBehavior((obj, ctx) =>
            {
                obj.Transform.Orientation = Quaternion.CreateFromAxisAngle(new Vector3( 0, 1, 0), (float)(ctx.Time * Math.PI / 4));
            });

            scene.AddChild(cube);
            scene.AddChild(new AmbientLight(0.3f));
            scene.AddChild(new PointLight()).Transform.Position = new Vector3(0, 10, 10);

            app.OpenScene(scene);


            return app;
        }
    }
}
