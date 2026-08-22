using System.Numerics;
using XrEngine;
using XrEngine.OpenXr;
using XrEngine.UI;
using XrMath;
using XrEngine.Reconstruct;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Scanner")]
        public static XrEngineAppBuilder CreateScanner(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();
            var scene = app.ActiveScene!;

            var panel = new TextPanel();

            var window = new Window3D();

            window.Size = new Size2(0.05f, 0.02f);
            window.DpiScale = 1.1f;
            window.Content = panel;

            var mat = new TextureClipMaterial();
            mat.Alpha = AlphaMode.Blend;
            window.Materials.Clear();
            window.Materials.Add(mat);

            var isInit = false;

            window.AddBehavior((a, b) =>
            {
                if (!isInit && window.ActiveTexture != null)
                {
                    mat.MainLeftTexture = window.ActiveTexture;
                    var size = new Vector2(window.ActiveTexture.Width, window.ActiveTexture.Height);
                    var viewSize = new Vector2(scene.ActiveCamera!.ViewSize.Width, scene.ActiveCamera.ViewSize.Height);
                    var relSize = 2 * size / viewSize;
                    window.Transform.Scale = new Vector3(relSize.X, relSize.Y, 1);
                    //window.Transform.Position = new Vector3(-1 + 0.2f + relSize.X / 2, 1 - 0.2f - relSize.Y / 2, 0);
                    isInit = true;
                }

                if (panel.Text != null)
                    panel.Text.Text = b.Frame.ToString();
            });

            var points = new PointMesh();
            var depth = points.AddComponent(new DepthPointScanner
            {
                SavePath = Path.Join(XrPlatform.Current!.PersistentPath, "Scanner"),
            });

            scene.AddChild(points);
            scene.AddChild(window);

            return builder
              .UseApp(app)
              .UseDefaultHDR()
              .ConfigureSampleApp(true)
              .UseEnvironmentDepth()
              .SetGlOptions(opt =>
              {
                  opt.FrustumCulling = false;
              })
              .ConfigureApp(a =>
              {
                  depth.ScanInput = a.Inputs!.Right!.TriggerClick;
                  depth.ClearInput = a.Inputs!.Right.Button!.BClick;
                  depth.HideInput = a.Inputs!.Right.Button!.AClick;
              });
        }
    }
}
