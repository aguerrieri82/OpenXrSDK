using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Silk.NET.OpenXR;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework.Android
{
    public class XrWebViewLayer : XrAndroidSurfaceQuadLayer
    {
        class WebView2 : WebView
        {
            XrWebViewLayer _layer;

            public WebView2(XrWebViewLayer layer)
                : base(layer._context)
            {
                _layer = layer; 
            }
           
            public override void Draw(Canvas canvas)
            {
                _layer.ScheduleDraw(base.Draw);
            }  
        }

        class WebClient : WebViewClient
        {
            XrWebViewLayer _layer;

            public WebClient(XrWebViewLayer layer)
            {
                _layer =layer;  
            }

            public override bool ShouldOverrideUrlLoading(WebView? view, string? url)
            {
                view!.LoadUrl(url!);
                return true;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                _layer.UpdateScale();
                base.OnPageFinished(view, url);
            }
        }

        class ChromeClient : WebChromeClient
        {

            public override void OnPermissionRequest(PermissionRequest? request)
            {
                base.OnPermissionRequest(request);
            }

            public override void OnReceivedTitle(WebView? view, string? title)
            {
                base.OnReceivedTitle(view, title);
            }

            public override void OnShowCustomView(global::Android.Views.View? view, ICustomViewCallback? callback)
            {
                base.OnShowCustomView(view, callback);
            }

            public override void OnHideCustomView()
            {
                base.OnHideCustomView();
            }
        }


        protected Context _context;
        protected WebView? _webView;
        protected HandlerXrThread _handler;
        protected Vector2 _lastLayerSize;


        public XrWebViewLayer(Context context, GetQuadDelegate getQuad)
            : base(getQuad)
        {
            var quad = getQuad();

            _size.Width = (int)(quad.Size.X * 1000);
            _size.Height = (int)(quad.Size.Y * 1000);

            _context = context;

            _handler = new HandlerXrThread(new Handler(Looper.MainLooper!));

            _ = _handler.ExecuteAsync(CreateWebView);
        }

        protected void ScheduleDraw(Action<Canvas> action)
        {
            Draw(action);
        }

        protected virtual internal void Draw(Action<Canvas> action)
        {
            if (_surface == null)
                return;

            var newCanvas = _surface.LockHardwareCanvas();
            try
            {
                if (newCanvas != null)
                {
                    var scaleX = _size.Width / (float)_webView!.Width;
                    var scaleY = _size.Height / (float)_webView!.Height;

                    newCanvas.Scale(scaleX, scaleY);

                    action(newCanvas);
                }
            }
            finally
            {
                if (newCanvas != null)
                    _surface.UnlockCanvasAndPost(newCanvas);
            }
        }

        protected override bool Update(ref CompositionLayerQuad layer, ref Silk.NET.OpenXR.View[] views, XrSwapchainInfo[] swapchains, long predTime)
        {
            var result = base.Update(ref layer, ref views, swapchains, predTime);
            layer.Size.Height *= -1;
            return result;
        }

        protected void CreateWebView()
        {
            _webView = new WebView2(this);
            _webView.SetWebViewClient(new WebClient(this));
            _webView.SetWebChromeClient(new ChromeClient());
            //_webView.SetLayerType(LayerType.Software, null);

            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.AllowContentAccess = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            _webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            _webView.Settings.LoadsImagesAutomatically = true;
            _webView.Settings.MediaPlaybackRequiresUserGesture = false;
            _webView.Settings.SetSupportMultipleWindows(false);
            _webView.Settings.SetNeedInitialFocus(false);
            _webView.Settings.OffscreenPreRaster = true;
            _webView.Settings.SetSupportZoom(true);


            if (_context is Activity activity)
            {
                var layout = new ViewGroup.LayoutParams(_size.Width, _size.Height);
                activity.AddContentView(_webView, layout);
                UpdateScale();
            }
        }

        void UpdateScale()
        {
            var density = _webView!.Resources!.DisplayMetrics!.Density;
            _webView.SetInitialScale((int)(1.8f / density * 100));
        }


        public WebView? WebView => _webView;
    }
}
