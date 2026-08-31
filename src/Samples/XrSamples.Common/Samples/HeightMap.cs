using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        public static XrEngineAppBuilder CreateHeightMap(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;
            var mat = MaterialFactory.CreatePbr("#ffffff");
            mat.Roughness = 0f;

            /*
            mat.ColorMap = AssetLoader.Instance.Load<Texture2D>("res://asset/Earth/waves.png");
            mat.ColorMap.Transform = Matrix3x3.CreateScale(-1, 1);
            mat.ColorMap.WrapS = WrapMode.Repeat;
            mat.ColorMap.WrapT = WrapMode.Repeat;
            mat.ColorMap.Format = TextureFormat.SBgra32;
            */

            if (mat is IHeightMaterial hm)
            {
                hm.DisplacmentMap = new DisplacmentMapSettings
                {
                    Texture = AssetLoader.Instance.Load<Texture2D>("res://asset/Earth/waves.png"),
                    ScaleFactor = 0.3f,
                    TargetTriSize = 5,
                    DebugTessellation = false,
                    NormalStrength = new Vector3(20, 20, 1),
                    NormalMode = HeightNormalMode.Sobel
                };

                hm.DisplacmentMap.Texture.WrapS = WrapMode.Repeat;
                hm.DisplacmentMap.Texture.WrapT = WrapMode.Repeat;
                hm.DisplacmentMap.Texture.MagFilter = ScaleFilter.Linear;
                hm.DisplacmentMap.Texture.MinFilter = ScaleFilter.Linear;

                //mat.NormalMap = NormalMap.FromHeightMap(hm.HeightMap, 1f);
                //mat.NormalMap.SaveAs("d:\\heightmap.png");
            }

            var quod = new QuadPatch3D(new Vector2(2, 1), 100);
            //quod.ToTriangles();

            var plane = new TriangleMesh(quod, (Material)mat);

            scene.AddChild(plane);

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .ConfigureSampleApp();

        }
    }
}
