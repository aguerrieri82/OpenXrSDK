using OpenXr.Framework;
using PhysX.Framework;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.OpenXr.Oculus;
using static Sfizz.SfzParser;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Geo Layers")]
        public static XrEngineAppBuilder CreateGeoLayers(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var texture = AssetLoader.Instance.Load<Texture2D>("res://asset/check.png");


            var texture2 = AssetLoader.Instance.Load<Texture2D>("res://asset/Envs/CameraEnv.jpg");

            var screen = scene.AddChild(new CurvedScreen());
            screen.AddComponent(new XrScreenAttached(texture));

            var sphere = scene.AddChild(new EquirectSphere(3));
            sphere.WorldPosition = new Vector3(0, 1.3f, 0);
            sphere.AddComponent(new XrEquirectSphereAttached(texture2)
            {
                
            });

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseOculus(opt =>
                {
                })
                .ConfigureSampleApp(useHands: false);
        }
    }
}
