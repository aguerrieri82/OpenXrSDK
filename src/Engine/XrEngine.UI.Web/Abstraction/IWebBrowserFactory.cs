namespace XrEngine.UI.Web
{
    public class WebBrowserOptions
    {
        public TriangleMesh? DestMesh { get; set; }

        public bool UseLocalUI { get; set; }

        public string? LocalAssetsPath { get; set; }

        public bool UseCpu { get; set; }
    }

    public interface IWebBrowserFactory
    {
        IWebBrowser CreateBrowser(WebBrowserOptions options);
    }
}
