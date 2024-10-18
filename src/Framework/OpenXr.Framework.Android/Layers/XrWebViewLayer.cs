using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Silk.NET.OpenXR;
using System.Numerics;
using XrInteraction;

namespace OpenXr.Framework.Android
{
    public class XrWebViewLayer : XrAndroidSurfaceQuadLayer
    {
        class WebView2 : WebView
        {
            readonly XrWebViewLayer _layer;

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
            const string TAG = nameof(WebClient);

            readonly XrWebViewLayer _layer;

            public WebClient(XrWebViewLayer layer)
            {
                _layer = layer;
            }

            public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
            {
                Log.Debug(TAG, "ShouldInterceptRequest");
                if (_layer.ShouldInterceptRequest != null)
                    return _layer.ShouldInterceptRequest(request!);
                return base.ShouldInterceptRequest(view, request);
            }

            public override bool ShouldOverrideUrlLoading(WebView? view, string? url)
            {
                Log.Debug(TAG, "ShouldOverrideUrlLoading");
                //view!.LoadUrl(url!);
                return false;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                Log.Debug(TAG, "OnPageFinished");
                //_layer.UpdateScale();
                base.OnPageFinished(view, url);
            }

        }

        class ChromeClient : WebChromeClient
        {
            const string TAG = nameof(WebChromeClient);

            public override void OnPermissionRequest(PermissionRequest? request)
            {
                Log.Debug(TAG, "OnPermissionRequest");
                base.OnPermissionRequest(request);
            }

            public override void OnReceivedTitle(WebView? view, string? title)
            {
                Log.Debug(TAG, "OnReceivedTitle");
                base.OnReceivedTitle(view, title);
            }

            public override void OnShowCustomView(global::Android.Views.View? view, ICustomViewCallback? callback)
            {
                Log.Debug(TAG, "OnShowCustomView");
                base.OnShowCustomView(view, callback);
            }

            public override void OnHideCustomView()
            {
                Log.Debug(TAG, "OnHideCustomView");
                base.OnHideCustomView();
            }

            public override bool OnCreateWindow(WebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
            {
                Log.Debug(TAG, "OnCreateWindow");
                return base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
            }
        }

        protected class InputController
        {
            protected ISurfaceInput _surfaceInput;
            protected long _lastDownTime;
            protected IXrThread _mainThread;

            public InputController(ISurfaceInput surfaceInput, IXrThread mainThread)
            {
                _surfaceInput = surfaceInput;
                _mainThread = mainThread;
            }

            public void Update(WebView webView)
            {
                if (!_surfaceInput.IsPointerValid)
                    return;

                var now = SystemClock.UptimeMillis();

                MotionEventActions actions;

                if (_surfaceInput.SecondaryButton.IsChanged && _surfaceInput.SecondaryButton.IsDown)
                {
                    _ = _mainThread.ExecuteAsync(webView.GoBack);
                }

                if (_surfaceInput.MainButton.IsChanged)
                {
                    if (_surfaceInput.MainButton.IsDown)
                    {
                        _lastDownTime = now;
                        actions = MotionEventActions.Down;
                    }
                    else
                        actions = MotionEventActions.Up;
                }
                else
                {
                    actions = MotionEventActions.Move;
                }

                var pos = _surfaceInput.Pointer * new Vector2(webView.Width, webView.Height);

                var ev = MotionEvent.Obtain(_lastDownTime, now, actions, pos.X, webView.Height - pos.Y, MetaKeyStates.None);

                webView.DispatchTouchEvent(ev);
            }
        }

        protected Context _context;
        protected WebView? _webView;
        protected HandlerXrThread _mainThread;
        protected Vector2 _lastLayerSize;
        protected InputController _input;


        public XrWebViewLayer(Context context, GetQuadDelegate getQuad, ISurfaceInput surfaceInput)
            : base(getQuad)
        {
            var quad = getQuad();

            _size.Width = AlignToMultiple((int)(quad.Size.X * 1700), 32);
            _size.Height = AlignToMultiple((int)(quad.Size.Y * 1700), 32);

            _mainThread = new HandlerXrThread(new Handler(Looper.MainLooper!));
            _context = context;
            _input = new InputController(surfaceInput, _mainThread);

            _ = _mainThread.ExecuteAsync(CreateWebView);
        }

        public static int AlignToMultiple(int number, int bitSize)
        {
            int mask = (1 << bitSize) - 1; // Create a mask with the bit size
            return (number + mask) & ~mask; // Align the number to the nearest multiple
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

                    newCanvas.DrawColor(global::Android.Graphics.Color.Transparent, PorterDuff.Mode.Clear!);
                    newCanvas.Translate(-_webView.ScrollX, -_webView.ScrollY);

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

        protected override bool Update(ref CompositionLayerQuad layer, ref Silk.NET.OpenXR.View[] views, long predTime)
        {
            var result = base.Update(ref layer, ref views, predTime);

            layer.Size.Height *= -1;

            if (_webView != null)
                _input.Update(_webView);

            return result;
        }

        private string? GetWebViewVersion()
        {
            try
            { 
                var packageInfo = _context.PackageManager!.GetPackageInfo("com.google.android.webview", 0);
                return packageInfo?.VersionName;
            }
            catch (Exception)
            {
                return "WebView not found";
            }
        }


        protected void CreateWebView()
        {
            /*
            var verName = GetWebViewVersion();

            Log.Debug(nameof(XrWebViewLayer), verName ?? "");
            */

            _webView = new WebView2(this);
            _webView.SetWebViewClient(new WebClient(this));
            _webView.SetWebChromeClient(new ChromeClient());

            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.AllowContentAccess = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            _webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            _webView.Settings.LoadsImagesAutomatically = true;
            _webView.Settings.MediaPlaybackRequiresUserGesture = false;
            _webView.Settings.SetSupportMultipleWindows(false);
            _webView.Settings.SetNeedInitialFocus(false);
            _webView.Settings.UserAgentString = "Mozilla/5.0 (Linux) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.0.0 Safari/537.36";
            _webView.Settings.CacheMode = CacheModes.CacheElseNetwork;
            _webView.SetLayerType(LayerType.Hardware, null);

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

        public HandlerXrThread MainThread => _mainThread;

        public Func<IWebResourceRequest, WebResourceResponse?>? ShouldInterceptRequest { get; set; }

        public WebView? WebView => _webView;
    }
}
