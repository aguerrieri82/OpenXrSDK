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
            const int simulationSize = 300;
            const int gridSize = 300;
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
                WaterDepth = startLevel,
            };
            var settings = new WaterFloodSettings(material);
            settings.Apply();
             
            var waterGrid = new Grid3D(new Size2I(gridSize, gridSize));
            waterGrid.ComputeTangents();

            var water = scene.AddChild(new TriangleMesh(
                waterGrid,
                material)
            {
                Name = "Flood water",
            });

            water.Flags |= EngineObjectFlags.NoFrustumCulling;
            water.CompressionMode = MeshCompressionMode.Never;

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
                material.WaterSize = floorInfo.Size;
            };

            var waterLevel = startLevel;

            water.AddBehavior((_, ctx) =>
            {
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
                .UseFloorTeleport(scene)
                .UseEnvironmentDepth()
                //.UseEnvironmentMesh(100, receiveShadow: false)
                .UseCameraRefraction(true)
                .AddPanel(new WaterFloodSettingsPanel(settings))
                .ConfigureApp(e =>
                {
                    if (e.App.Renderer is OpenGLRender render)
                    {
                        var player = scene.FindByName<Object3D>("Player")!;
                        player.AddComponent(new WaterStepAudio());
                        var simulation = new GlWaterSimulationPass(render, material, water, player);
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
