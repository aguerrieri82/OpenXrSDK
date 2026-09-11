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
            scene.AddChild(new VirtualKeyboardView());

            return builder
                .UseApp(app)
                .UseDefaultHDR()
                .SetGlOptions(opt=> opt.UseInstanceDraw = true)
                .ConfigureSampleApp();
        }
    }
}
