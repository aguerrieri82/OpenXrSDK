using CefSharp;
using XrEngine.UI.Web;

namespace XrEngine.Browser.Windows
{
    internal class ChromeSchemeHandlerFactory : ISchemeHandlerFactory
    {
        readonly IWebRequestHandler _handler;


        public ChromeSchemeHandlerFactory(IWebRequestHandler webRequestHandler)
        {
            _handler = webRequestHandler;
        }

        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            return new ChromeWebResourceHandler(_handler);
        }
    }
}
