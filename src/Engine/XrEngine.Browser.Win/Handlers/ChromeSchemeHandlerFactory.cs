using CefSharp;
using System.Net;
using XrEngine.UI.Web;
using XrInteraction;

namespace XrEngine.Browser.Win
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
