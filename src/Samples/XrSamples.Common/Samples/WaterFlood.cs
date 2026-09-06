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

            var state = new Texture2D
            {
                Name = "Water state",
                WrapS = WrapMode.ClampToEdge,
                WrapT = WrapMode.ClampToEdge,
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                NeverCompress = true
            };
            state.SetDescription(simulationSize, simulationSize, 2, TextureFormat.RgbaFloat16);

            var material = new WaterMaterial(state)
            {
                WaterSize = Vector2.One,
                WaterDepth = startLevel
            };
            var settings = new WaterFloodSettings(material);
            settings.Apply();

            var water = scene.AddChild(new TriangleMesh(
                new Grid3D(new Size2I(gridSize, gridSize)),
                material)
            {
                Name = "Flood water",
            });

            water.Flags |= EngineObjectFlags.NoFrustumCulling;

            var sceneFactory = new DefaultSceneModelFactory();
            var sceneView = scene.AddChild(new OculusSceneView
            {
                Factory = sceneFactory
            });
            var depthMaskMaterial = new DepthOnlyMaterial
            {
                DoubleSided = true
            };

            sceneFactory.AddMesh(depthMaskMaterial);
            sceneFactory.AddWalls(depthMaskMaterial);

            var floor = new TriangleMesh(new Cube3D(new Vector3(3, 3, floorThickness)));
            floor.WorldOrientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2f);

            sceneView.SceneReady += (_, _) =>
            {
                var detectedFloor = sceneView.FindByName<TriangleMesh>("Floor");

                if (detectedFloor == null)
                    return;

                var floorInfo = detectedFloor.GetProp<SceneModelInfo>("SceneModel");

                if (floorInfo.Size.X <= 0 || floorInfo.Size.Y <= 0)
                    return;

                floor = detectedFloor;
                material.WaterSize = floorInfo.Size;
            };

            var waterLevel = startLevel;
            var restartVersion = settings.RestartVersion;
            water.AddBehavior((_, ctx) =>
            {
                if (restartVersion != settings.RestartVersion)
                {
                    restartVersion = settings.RestartVersion;
                    waterLevel = startLevel;
                }

                if (!settings.PauseFlood)
                    waterLevel += settings.RiseSpeed * (float)ctx.DeltaTime;

                waterLevel = MathF.Min(settings.MaximumDepth, waterLevel);
                material.WaterDepth = waterLevel;

                var floorNormal = Vector3.Transform(Vector3.UnitZ,  floor.WorldOrientation);

                water.WorldOrientation = floor.WorldOrientation;
                water.WorldPosition = floor.WorldPosition + floorNormal * (floorThickness * 0.5f + waterLevel);
            });

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp()
                .UseCameraRefraction(true)
                .AddPanel(new WaterFloodSettingsPanel(settings))
                .ConfigureApp(e =>
                {
                    if (e.App.Renderer is OpenGLRender render)
                    {
                        var simulation = new GlWaterSimulationPass(render, material);
                        settings.AttachSimulation(simulation);
                        render.AddPass(simulation, 0);
                    }
                    var light = scene.FindByName<PointLight>("point-light-1");

                    light!.IsVisible = true;
                    light.Transform.SetPositionY(1f);
                    light.Intensity = 2f;
                });
        }
    }
}
