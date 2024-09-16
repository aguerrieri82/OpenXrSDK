﻿using Android.Webkit;
using Java.Interop;
using OpenXr.Framework;
using OpenXr.Framework.Android;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Web;

namespace XrEngine.OpenXr.Android
{
    public class AndroidWebViewBrowser : IWebBrowser
    {
        private readonly XrWebViewLayer _webViewLayer;
        private Interface _interface;
        private bool _isInit;


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
        }
        public WebResourceResponse? HandleResponse(IWebResourceRequest req)
        {
            try
            {
                Log.Info(this, "Browser Request: {0}", req.Url);

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

                if (webResp.Headers == null || !webResp.Headers.TryGetValue("Content-Type", out var mimeType))
                    mimeType = null;
      
                Log.Debug(this, "Browser Response received: {0}, code: {2}, mime: {1}", webReq.Uri, mimeType, webResp.Code);

                var result = new WebResourceResponse(
                    mimeType,
                    null,
                    webResp.Code,
                    "OK",
                    webResp.Headers,
                    webResp.Body != null ? new MemoryStream(webResp.Body) : null);

                return result;
            }
            catch (Exception ex)
            {
                Log.Warn(this, "Browser Handler error: {0}", req.Url);
                Log.Error(this, ex);
                return null;
            }
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

        protected async Task InitAsync()
        {
            Log.Info(this, "Browser Init");

            await _webViewLayer.MainThread.ExecuteAsync(() =>
            {
                _webViewLayer.WebView!.AddJavascriptInterface(_interface, "bridge");
                _webViewLayer.ShouldInterceptRequest = HandleResponse;
            });

            _isInit = true;
        }

        public async Task NavigateAsync(string uri)
        {
            if (_webViewLayer.WebView == null)
                throw new InvalidOperationException();

            if (!_isInit)
                await InitAsync();

            Log.Info(this, "Browser NavigateAsync {0}", uri);

            await _webViewLayer.MainThread.ExecuteAsync(() =>
            {
                _webViewLayer.WebView.LoadUrl(uri);
            });
        }

        public event EventHandler<MessageReceivedArgs>? MessageReceived;

        public IWebRequestHandler? RequestHandler { get; set; }

    }
}