using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Context2 = global::Android.Content.Context;


namespace XrEngine.OpenXr.Android
{
    public static class XrEngineAppBuilderExtension
    {
        public static XrEngineAppBuilder AddWebBrowser(this XrEngineAppBuilder builder, Context2 context, string objName) =>

            builder.UseRightController().
                    ConfigureApp(e =>
        {
            var display = e.App.ActiveScene!.FindByName<TriangleMesh>(objName);

            if (display == null)
                return;

            var inputs = e.GetInputs<XrOculusTouchController>();

            if (display != null)
            {
                var controller = new SurfaceController(
                    inputs.Right!.TriggerClick!,
                    inputs.Right!.SqueezeClick!,
                    inputs.Right!.Haptic!);

                display.AddComponent(controller);

                e.XrApp.Layers.AddWebView(context, display.BindToQuad(), controller);
            }

        });
    }
}
