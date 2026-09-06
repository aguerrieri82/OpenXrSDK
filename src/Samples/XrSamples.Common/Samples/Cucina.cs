using System.Numerics;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Cucina")]
        public static XrEngineAppBuilder CreateCucina(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh = GltfLoader.LoadFile(GetAssetPath("cucina.glb"), GltfOptions);
            mesh.Name = "mesh";
            mesh.Transform.SetScale(0.04f);
            mesh.Transform.Position = new Vector3(-mesh.WorldBounds.Center.X, 0, -mesh.WorldBounds.Center.Z);

            var blank = (Material)MaterialFactory.CreatePbr(Color.White);

            foreach (var item in mesh.DescendantsOrSelf().OfType<TriangleMesh>())
            {
                if (IsEditor)
                    item.AddComponent<BoxCollider>();

                if (item.Name != "Obj_PolyFaceMesh_51")
                    item.IsVisible = true;

                for (var i = 0; i < item.Materials.Count; i++)
                {
                    var material = (IPbrMaterial)item.Materials[i];
                    if (material.ColorMap == null)
                        item.Materials[i] = blank;

                    material.DoubleSided = true;

                    if (material.Name == "wfnhfaq_Stucco_Facade")
                    {
                        material.Color = new Color(1.6f, 1.6f, 1.6f, 1);
                    }

                    if (material.Name == "schcbgfp_Scratched_Polyvinylpyrrolidone_Plastic")
                    {
                        //material.Metalness = 0;
                    }

                    if (material.Name == "vigjfivg_Old_Plywood")
                    {
                        //material.Metalness = 0;
                        //material.Roughness = 0.7f;
                    }

                    if (material.Name == "wjmkfbnl_Crema_Marfi_Marble")
                    {
                        // material.Metalness = 0;
                    }

                    if (material.Name == "shkaaafc_Brushed_Aluminum")
                    {
                        material.Color = new Color(0.35f, 0.3f, 0.3f, 1);
                        //material.Roughness = 1;
                    }

                    if (material.Name == "uk3kec1ew_Brown_Tiles")
                    {
                        //material.Roughness = 0.6f;
                    }

                }

            }

            string[] wallNames = ["Obj_3dSolid_912", "Obj_3dSolid_909", "Obj_3dSolid_910", "Obj_3dSolid_911", "Obj_3dSolid_419"];
            var group = new Group3D()
            {
                Name = "walls",
                IsVisible = true
            };

            foreach (var item in wallNames)
            {
                var obj = mesh.DescendantsOrSelf().Where(a => a.Name == item).FirstOrDefault();
                if (obj != null)
                    group.AddChild(obj.Parent!);
            }

            if (mesh is Group3D meshGrp)
                meshGrp.AddChild(group);
            mesh.AddComponent<ConstraintGrabbable>();

            scene.AddChild(mesh);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                //.UseSceneModel(true, false)
                .ConfigureApp(cfg =>
                {
                    scene.FindByName<Light>("point-light-1")!.IsVisible = true;
                    scene.FindByName<PointLight>("point-light-1")!.Range = 5f;
                })
                .ConfigureSampleApp();
        }
    }
}
