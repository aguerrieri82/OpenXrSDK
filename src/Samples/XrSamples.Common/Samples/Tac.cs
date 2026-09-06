using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Tac")]
        public static XrEngineAppBuilder CreateTac(this XrEngineAppBuilder builder)
        {

            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var mesh1 = (TriangleMesh)AssetLoader.Instance.Load(new Uri("D:\\Misc\\TAC\\Head-Skin.obj"), typeof(TriangleMesh), null);
            var mesh2 = (TriangleMesh)AssetLoader.Instance.Load(new Uri("D:\\Misc\\TAC\\Head-Bone.obj"), typeof(TriangleMesh), null);

            var mat1 = (PbrMaterial)MaterialFactory.CreatePbr(Color.White);

            mat1.ClipVolume = new Bounds3()
            {
                Min = new Vector3(0, float.NegativeInfinity, float.NegativeInfinity),
                Max = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            };

            mesh1.Materials.Add(mat1);

            var mat2 = (PbrMaterial)MaterialFactory.CreatePbr(Color.White);

            mat2.ClipVolume = new Bounds3()
            {
                Min = new Vector3(0, float.NegativeInfinity, float.NegativeInfinity),
                Max = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            };

            mesh2.Materials.Add(mat2);

            var grp = new Group3D();
            grp.Name = "Tac";
            grp.Transform.SetScale(0.001f);
            grp.Transform.Rotation = new Vector3(-MathF.PI / 2, 0, 0);

            grp.AddComponent<BoundsGrabbable>();
            grp.AddChild(mesh1);
            grp.AddChild(mesh2);

            scene.AddChild(grp);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
