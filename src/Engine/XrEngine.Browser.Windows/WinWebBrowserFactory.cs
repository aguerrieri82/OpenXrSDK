using System.Diagnostics;
using XrEngine.UI.Web;
using XrMath;

namespace XrEngine.Browser.Windows
{
    public class WinWebBrowserFactory : IWebBrowserFactory
    {
        public IWebBrowser CreateBrowser(WebBrowserOptions options)
        {
            var ui = options.DestMesh;

            Debug.Assert(ui != null);

            var webView = new ChromeWebBrowserView
            {
                Size = new Size2I((uint)(ui.Transform.Scale.X * 1700), (uint)(ui.Transform.Scale.Y * 1700)),
                ZoomLevel = 0
            };

            ui.AddComponent<SurfaceController>();
            ui.AddComponent(webView);

            return webView.Browser;
        }
    }
}
