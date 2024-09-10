using Android.Webkit;
using Java.Interop;
using OpenXr.Framework.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Web;

namespace XrEngine.OpenXr.Android
{
    public class AndroidWebViewBrowser :  Java.Lang.Object, IWebBrowser
    {
        private readonly XrWebViewLayer _webViewLayer;
        private Interface _interface;

        class Interface : Java.Lang.Object
        {
            AndroidWebViewBrowser _browser;

            public Interface(AndroidWebViewBrowser browser)
            {
                _browser = browser; 
            }

            [JavascriptInterface]
            [Export("postMessage")]
            public void PostMessage(string data)
            {
                _browser.MessageReceived?.Invoke(_browser, new MessageReceivedArgs(data));  
            }
        }

        public AndroidWebViewBrowser(XrWebViewLayer webViewLayer)
        {
            _interface = new Interface(this);
            _webViewLayer = webViewLayer;
            _webViewLayer.WebView!.AddJavascriptInterface(_interface, "bridge");
            _webViewLayer.ShouldInterceptRequest = req =>
            {
                Log.Info(this, "Request: {0}", req.Url);

                if (RequestHandler == null)
                    return null;

                var webReq = new WebRequest()
                {
                    Method = req.Method,
                    Uri = new Uri(req.Url!.ToString()!),
                };

                if (!RequestHandler.CanHandle(webReq))
                    return null;

                var webResp = RequestHandler.HandleRequest(webReq)!;

                var result = new WebResourceResponse(
                    webResp.Headers?["Content-Type"],
                    null,
                    webResp.Code,
                    "OK",
                    webResp.Headers,
                    webResp.Body != null ? new MemoryStream(webResp.Body) : null);

                return result;
            };
        }

        public Task PostMessageAsync(string message)
        {
            _=  _webViewLayer.MainThread.ExecuteAsync(() =>
            {
                _webViewLayer.WebView!.PostWebMessage(new WebMessage(message), global::Android.Net.Uri.Parse("*")!);
                return true;
            });

            return Task.CompletedTask;
        }

        public Task NavigateAsync(string uri)
        {
            _webViewLayer.WebView!.LoadUrl(uri);
            return Task.CompletedTask;
        }

        public event EventHandler<MessageReceivedArgs>? MessageReceived;

        public IWebRequestHandler? RequestHandler { get; set; }

    }
}
