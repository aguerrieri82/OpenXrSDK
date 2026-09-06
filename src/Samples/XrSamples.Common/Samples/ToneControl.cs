using System.Numerics;
using System.Xml.Linq;
using XrEngine;
using XrEngine.OpenXr;
using XrMath;
using XrSamples.Components;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Tone Control")]
        public static XrEngineAppBuilder CreateToneControl(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var dir = scene.Descendants<SunLight>().First();
            dir.Direction = -Vector3.UnitZ;
            dir.Intensity = 3.2f;

            foreach (var light in scene.Descendants<PointLight>())
            {
                light.IsVisible = false;
                light.AddComponent<LightViewer>();
            }

            var spot = scene.AddChild(new SpotLight());
            spot.IsVisible = false;
            spot.AddComponent<LightViewer>();

            var tc = scene.AddComponent(new ToneControl());

            var mat1 = new TextureMaterial();
            var mat2 = new PbrMaterial()
            {
                Metalness = 0
            };

            var mat3 = new ColorMaterial(new Color(0.5f, 0.5f, 0.5f));
            var mat4 = new PbrMaterial()
            {
                Color = mat3.Color,
                Metalness = 0
            };

            var quod1 = scene.AddChild(new TriangleMesh(Quad3D.Default, mat1));
            quod1.Materials.Add(mat2);

            var quod2 = scene.AddChild(new TriangleMesh(Quad3D.Default, mat3));
            quod2.Materials.Add(mat4);
            quod2.WorldPosition = new Vector3(1, 0, 0);

            void LoadTexture(bool isSrgb)
            {
                var fileName = "D:\\Projects\\XrEditor\\Cache\\Download\\493AE6FA342EE91A1979CB965B081079.jpg";

                mat1.Texture = AssetLoader.Instance.Load<Texture2D>(fileName, new TextureLoadOptions
                {
                    IsSrgb = isSrgb,
                    UseCache = false,
                    MimeType = "image/jpeg"
                });

                mat1.Texture.MipLevelCount = 10;
                mat1.Texture.MinFilter = ScaleFilter.LinearMipmapLinear;

                mat2.ColorMap = mat1.Texture;

                mat1.IsEnabled = !tc.ShowPbr;
                mat2.IsEnabled = tc.ShowPbr;

                mat3.IsEnabled = !tc.ShowPbr;
                mat4.IsEnabled = tc.ShowPbr;

                var color = new Color(0.5f, 0.5f, 0.5f)
                {
                    IsSrgb = isSrgb
                };

                mat4.Color = color;
                mat3.Color = color;

                foreach (var mesh in scene.Descendants<TriangleMesh>())
                {
                    foreach (var material in mesh.Materials)
                        material.NotifyChanged();
                }

            }

            tc.Changed = () =>
            {
                LoadTexture(tc.TexSRgb);
            };

            LoadTexture(true);

            return builder
                .UseApp(app)
                //.UseDefaultHDR()
                .ConfigureSampleApp();
        }
    }
}
