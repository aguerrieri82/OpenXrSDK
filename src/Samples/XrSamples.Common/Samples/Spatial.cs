using OpenXr.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine;
using XrEngine.Devices;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Spatial")]
        public static XrEngineAppBuilder CreateSpatial(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var factory = new DefaultSceneModelFactory();
            factory.AddMesh(new PbrMaterial() { Color = "#ff0000" });
            factory.AddWalls(new PbrMaterial() { Color = "#00ff00" });

            scene.AddChild(new OculusSceneView()
            {
                Factory = factory,  
            });

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
