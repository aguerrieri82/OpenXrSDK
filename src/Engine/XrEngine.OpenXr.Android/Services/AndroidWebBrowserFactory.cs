using OpenXr.Framework;
using System.Diagnostics;
using XrEngine.UI.Web;
using XrInteraction;

namespace XrEngine.OpenXr.Android
{
    public class AndroidWebBrowserFactory : IWebBrowserFactory
    {
        public IWebBrowser CreateBrowser(WebBrowserOptions options)
        {
            Debug.Assert(options.DestMesh?.Scene != null);

            var scene = options.DestMesh.Scene;

            var eApp = XrEngineApp.Current!;

            var inputs = eApp.Inputs;

            var xrInput = scene?.Components<XrInputPointer>().FirstOrDefault();

            if (xrInput == null)
            {
                scene.AddComponent(new XrInputPointer
                {
                    PoseInput = inputs!.Right!.AimPose,
                    RightButton = inputs!.Right!.SqueezeClick!,
                    LeftButton = inputs!.Right!.TriggerClick!,
                });
            }

            var controller = options.DestMesh.AddComponent<SurfaceController>();

            var mainActivity = Context.Require<IMainActivity>();

            var webViewLayer = eApp.XrApp!.Layers.AddWebView(mainActivity.Context, options.DestMesh.BindToQuad(), controller);

            var browser = new AndroidWebViewBrowser(webViewLayer);

            return browser;
        }
    }
}
