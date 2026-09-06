using System.Numerics;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Window/Door")]
        public static XrEngineAppBuilder CreateWindow(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("Window.glb"), GltfOptions);
            mesh.Name = "Window";
            mesh.AddComponent(new GeometryScale
            {
                Min = new Vector3(0.7f, 1.1f, -0.045f),
                Max = new Vector3(0.9f, 1.5f, -0.00f),
            });

            IPbrMaterial pbr;

            foreach (var item in mesh.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                if (item.Name == "Plane")
                {
                    pbr = MaterialFactory.CreatePbr(new Color(1, 1, 1, 0.4f));
                    pbr.Alpha = AlphaMode.Blend;
                    pbr.Roughness = 0;
                    pbr.Metalness = 0;
                    pbr.DoubleSided = true;
                    item.Materials.Add((Material)pbr);
                }

                foreach (var material in item.Materials)
                {
                    if (material.Name == "Wood1024")
                    {
                        pbr = (IPbrMaterial)material;
                        pbr.Color = "#96893F";
                        pbr.Metalness = 0.8f;
                        pbr.Roughness = 0.25f;
                    }
                    if (material.Name == "Metal1024")
                    {
                        pbr = (IPbrMaterial)material;
                        pbr.Metalness = 0.9f;
                        pbr.Roughness = 0.12f;
                    }
                    material.WriteStencil = 2;
                }
            }

            var door = GltfLoader.LoadFile(GetAssetPath("Door.glb"), GltfOptions);
            door.Name = "Door";
            door.AddComponent(new GeometryScale
            {
                Min = new Vector3(-0.13f, 0.9f, -0.005f),
                Max = new Vector3(0f, 1.14f, 0.01f),
            });

            scene.AddChild(door);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
