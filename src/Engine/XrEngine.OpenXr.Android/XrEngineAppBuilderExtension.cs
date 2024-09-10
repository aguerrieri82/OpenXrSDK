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

            if (display != null)
            {
                var inputs = e.Inputs as XrOculusTouchController;

                var xrInput = display.Scene!.Components<XrInputPointer>().FirstOrDefault();

                if (xrInput == null)
                {
                    display.Scene!.AddComponent(new XrInputPointer
                    {
                        PoseInput = inputs!.Right!.AimPose,
                        RightButton = inputs!.Right!.SqueezeClick!,
                        LeftButton = inputs!.Right!.TriggerClick!,
                    });
                }

                var controller = display.AddComponent<SurfaceController>();

                e.XrApp.Layers.AddWebView(context, display.BindToQuad(), controller);
            }
        });
    }
}
