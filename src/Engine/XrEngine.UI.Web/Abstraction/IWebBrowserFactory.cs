namespace XrEngine.UI.Web
{
    public class WebBrowserOptions
    {
        public TriangleMesh? DestMesh { get; set; }
    }

    public interface IWebBrowserFactory
    {
        IWebBrowser CreateBrowser(WebBrowserOptions options);
    }
}
