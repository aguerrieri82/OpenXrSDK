using System.Numerics;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {

        [Sample("Water Flood")]
        public static XrEngineAppBuilder CreateWaterFlood(this XrEngineAppBuilder builder)
        {
            const int simulationSize = 256;
            const int gridSize = 256;
            const float startLevel = 0.02f;
            const float floorThickness = 0.01f;

            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            var settings = new WaterFloodSettings();
            settings.Load(Path.Join(XrPlatform.Current!.PersistentPath, "water_flood_settings.json"));
             
            var water = scene.AddChild(new Water(simulationSize, gridSize)
            {
                Name = "Flood water"
            });
            water.Material.WaterDepth = startLevel;
            settings.Apply(water);

            var sceneFactory = new DefaultSceneModelFactory();
            var sceneView = scene.AddChild(new OculusSceneView
            {
                Factory = sceneFactory
            });

            var depthMaskMaterial = new DepthOnlyMaterial
            {
                DoubleSided = true
            };

            //sceneFactory.AddMesh(depthMaskMaterial);
            sceneFactory.AddWalls(depthMaskMaterial);

            var floor = new TriangleMesh(new Cube3D(new Vector3(3, 3, floorThickness)))
            {
                WorldOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f)
            };

            sceneView.SceneReady += (_, _) =>
            {
                var detectedFloor = sceneView.FindByName<TriangleMesh>("Floor");

                if (detectedFloor == null)
                    return;

                var floorInfo = detectedFloor.GetProp<SceneModelInfo>("SceneModel");

                if (floorInfo.Size.X <= 0 || floorInfo.Size.Y <= 0)
                    return;

                floor = detectedFloor;
                water.Material.WaterSize = floorInfo.Size;
            };

            var waterLevel = startLevel;

            water.AddBehavior((_, ctx) =>
            {
                if (!settings.PauseFlood)
                    waterLevel += settings.RiseSpeed * (float)ctx.DeltaTime;

                waterLevel = MathF.Min(settings.MaximumDepth, waterLevel);
                water.Material.WaterDepth = waterLevel;

                var floorNormal = Vector3.Transform(Vector3.UnitZ,  floor.WorldOrientation);

                water.WorldOrientation = floor.WorldOrientation;
                water.WorldPosition = floor.WorldPosition + floorNormal * (floorThickness * 0.5f + waterLevel);
            });

            var light = scene.FindByName<PointLight>("point-light-1")!;
            light.IsVisible = true;
            light.Transform.SetPositionY(1f);
            light.Intensity = 2f;

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp()
                .UseFloorTeleport(scene)
                .UseEnvironmentDepth()
                //.UseEnvironmentMesh(100, receiveShadow: false)
                .UseCameraRefraction(true)
                .AddPanel(new WaterFloodSettingsPanel(settings, water))
                .ConfigureApp(e =>
                {
                    if (e.App.Renderer is OpenGLRender render)
                    {
                        render.AddPass(new GlWaterSimulationPass(render, water), 0);
                    }
                });
        }
    }
}
