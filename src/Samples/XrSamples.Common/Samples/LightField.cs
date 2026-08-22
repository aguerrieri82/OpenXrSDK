using OpenXr.Framework.Oculus;
using System.Numerics;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrMath;
using XrEngine.Lighting;
using System.Xml.Linq;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Light Field")]
        public static XrEngineAppBuilder CreateLightField(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            scene.AddChild(new SpotLight()
            {
                WorldPosition = new Vector3(0, 0.63f, 1.84f),
                Direction = new Vector3(0, -0.2f, -1),
                Range = 4,
                Intensity = 5,
                InnerConeAngle = (14f).ToRadians(),
                OuterConeAngle = (20f).ToRadians(),
            });

            scene.AddChild(new AreaLight()
            {
                WorldPosition = new(0f, 1.31f, 1.6700001f),
                PlaneSize = new(1f, 0.8f),
                PlaneNormal = new(0.0348995f, 0f, -0.99939084f),
                Range = 4f,
                Direction = new(0f, -0.5344989f, -0.8451692f),
                Specular = "#00000000",
                Intensity = 5f
            });

            var voxelSize = 0.05f;
            var roomSize = new Vector3(5, 2, 5);

            var grid = new VoxelGridDesc
            {
                Origin = new Vector3(-roomSize.X / 2, 0f, -roomSize.Z / 2),
                VoxelSize = voxelSize,
                Size = new Vector3I(
                    (int)MathF.Round(roomSize.X / voxelSize),
                    (int)MathF.Round(roomSize.Y / voxelSize),
                    (int)MathF.Round(roomSize.Z / voxelSize))
            };

            var mesh = scene.AddChild((TriangleMesh)GltfLoader.LoadFile(GetAssetPath("IkeaBed.glb"), GltfOptions));
            mesh.AddComponent<LightFieldReceiver>();
            mesh.Name = "Bed";

            XrEngine.MeshOptimizer.Optimize(mesh.Geometry!);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseFloorTeleport(scene)
                .ConfigureApp(cfg =>
                {
                    foreach (var light in scene.Descendants<Light>())
                    {
                        light.IsVisible = true;
                        light.AddComponent(new LightFieldEmitter() { IsEnabled = false });
                    }

                    scene.AddComponent<LightFieldProvider>();

                    var lightField = scene.AddComponent(new LightFieldDebug(grid, XrPlatform.IsAndroid)
                    {
                        StorePath = Path.Combine(XrPlatform.Current!.SharedPath)
                    });

                    if (XrPlatform.IsAndroid)
                    {
                        lightField.Import();
                    }

                    scene.AddBehavior((_, _) =>
                    {
                        var click = cfg.Inputs!.Right!.Button!.AClick!;

                        if (click.IsChanged && click.Value)
                        {
                            var provider = Context.Require<IXrMotionVectorProvider>();
                            provider.IsActive = !provider.IsActive;
                        }
                    });
                })
                .ConfigureSampleApp(false);
        }
    }
}
