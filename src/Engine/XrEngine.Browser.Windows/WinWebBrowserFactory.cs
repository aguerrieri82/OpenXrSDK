using Silk.NET.OpenGL;
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

            var gl = EngineApp.Current.Renderer.Feature<GL>();

            var webView = new ChromeWebBrowserView(gl)
            {
                Size = new Size2I((uint)(ui.Transform.Scale.X * 1700), (uint)(ui.Transform.Scale.Y * 1700)),
                ZoomLevel = 0
            };

            ui.AddComponent<SurfaceController>();
            ui.AddComponent(webView);

            if (options.UseLocalUI && !string.IsNullOrWhiteSpace(options.LocalAssetsPath))
                webView.Browser.RequestHandler = new FsWebRequestHandler("main", options.LocalAssetsPath);

            return webView.Browser;
        }
    }
}
