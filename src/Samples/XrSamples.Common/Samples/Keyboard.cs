using OpenXr.Framework;
using XrEngine;
using XrEngine.Gltf;
using XrEngine.OpenXr;

namespace XrSamples
{
    public static partial class SampleScenes
    {
        [Sample("Keyboard")]
        public static XrEngineAppBuilder CreateKeyboard(this XrEngineAppBuilder builder)
        {
            var app = CreateBaseScene();

            var scene = app.ActiveScene!;

            var quod = scene.AddChild(new TriangleMesh(new Quad3D(new System.Numerics.Vector2(0.9f, 0.5f))));
            quod.Materials.Add(new PbrMaterial() { Color = "#ff0000" });

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .UseClickMoveFront(quod)
                .UseVirtualKeyboard(quod)
                .ConfigureApp(e =>
                {
                    var key  = (ITextInputProvider)scene.DescendantsOrSelf().OfType<VirtualKeyboardView>().First();

                    var click = e.Inputs!.Right.Button.BClick;

                    scene.AddBehavior((_, _) =>
                    {
                        if (click.IsChanged && click.Value)
                        {
                            if (key.IsVisible)
                                key.Hide();
                            else
                                key.Show();
                        }
          
                    });
         
                })
                .SetGlOptions(opt=> opt.UseInstanceDraw = true)
                .ConfigureSampleApp();
        }
    }
}
