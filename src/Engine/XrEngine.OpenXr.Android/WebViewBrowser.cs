using Android.Webkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Web;

namespace XrEngine.OpenXr.Android
{
    public class WebViewBrowser : IWebBrowser
    {
        private readonly WebView _webView;

        public WebViewBrowser(WebView webView)
        {
            _webView = webView;
            Context.Implement(new WebBrowserBridge(this));
        }

        public Task PostMessageAsync(string message)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<MessageReceivedArgs> MessageReceived;

    }
}
