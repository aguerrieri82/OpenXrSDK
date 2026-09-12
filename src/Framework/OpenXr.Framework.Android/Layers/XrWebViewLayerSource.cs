using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Webkit;
using Silk.NET.OpenXR;
using System.Numerics;
using System.Runtime.Versioning;
using XrInteraction;
using static Android.Views.MotionEvent;
using static Android.Webkit.WebSettings;

namespace OpenXr.Framework.Android
{

    public class XrWebViewLayerSource : XrAndroidSurfaceLayerSource
    {
        class WebView2 : WebView
        {
            readonly XrWebViewLayerSource _layer;

            public WebView2(XrWebViewLayerSource layer)
                : base(layer._context)
            {
                _layer = layer;
            }

            public override void Draw(Canvas canvas)
            {
                _layer.ScheduleDraw(base.Draw);
            }

            public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
            {
                var result = base.OnCreateInputConnection(outAttrs);

                _layer.SetInputConnection(result);

                return result;
            }
        }

        class WebClient : WebViewClient
        {
            const string TAG = nameof(WebClient);

            readonly XrWebViewLayerSource _layer;

            public WebClient(XrWebViewLayerSource layer)
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
                return false;
            }

            public override void OnPageFinished(WebView? view, string? url)
            {
                Log.Debug(TAG, "OnPageFinished");
                base.OnPageFinished(view, url);
            }

        }

        class ChromeClient : WebChromeClient
        {
            const string TAG = nameof(ChromeClient);

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
            private PointerProperties[]? _pointerProps;
            private PointerCoords[]? _pointerCoords;

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
                    if (_surfaceInput.MainButton.IsDown)
                        actions = MotionEventActions.Move;
                    else
                    {
                        _lastDownTime = now;
                        actions = MotionEventActions.HoverMove;
                    }
                }

                var pos = _surfaceInput.Pointer * new Vector2(webView.Width, webView.Height);

                _pointerProps ??=
                [
                    new()
                    {
                        Id = 1,
                        ToolType = MotionEventToolType.Mouse,
                    }
                ];

                _pointerCoords ??=
                [
                    new()
                    {
                        Pressure = _surfaceInput.MainButton.IsDown || _surfaceInput.SecondaryButton.IsDown ? 1 : 0,
                        Size = 1,
                    }
                ];

                _pointerCoords[0].X = pos.X;
                _pointerCoords[0].Y = webView.Height - pos.Y;

                MotionEventButtonState buttonState = 0;
                /*
                if (_surfaceInput.MainButton.IsDown)
                    buttonState |= MotionEventButtonState.Primary;
                
                if (_surfaceInput.SecondaryButton.IsDown)
                    buttonState |= MotionEventButtonState.Secondary;
               */

                var ev = MotionEvent.Obtain(
                    actions == MotionEventActions.Up ? _lastDownTime : now,
                    now,
                    actions,
                    1,
                    _pointerProps,
                    _pointerCoords,
                    MetaKeyStates.None,
                    buttonState,
                    1,
                    1,
                    0,
                    0,
                    InputSourceType.Mouse,
                    MotionEventFlags.None);

                _ = _mainThread.ExecuteAsync(() => webView.DispatchTouchEvent(ev));
            }
        }

        protected Context _context;
        protected WebView? _webView;
        protected HandlerXrThread _mainThread;
        protected Vector2 _lastLayerSize;
        protected InputController _input;

        private ITextInputProvider? _textInput;
        private IInputConnection? _inputConnection;

        public XrWebViewLayerSource(Context context, Extent2Di size, ISurfaceInput surfaceInput)
            : base(size)
        {
            _mainThread = new HandlerXrThread(new Handler(Looper.MainLooper!));
            _context = context;
            _input = new InputController(surfaceInput, _mainThread);

            _ = _mainThread.ExecuteAsync(CreateWebView);
        }


        public static int AlignToMultiple(int number, int bitSize)
        {
            var mask = (1 << bitSize) - 1; // Create a mask with the bit size
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
                    var scaleY = _size.Height / (float)_webView.Height;

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

        public override bool Update(long predTime)
        {
            UpdateTextInput();

            if (_webView != null)
                _input.Update(_webView);

            return base.Update(predTime);
        }

        private void UpdateTextInput()
        {
            var textInput = _xrApp?.TextInput;

            if (_textInput == textInput)
                return;

            if (_textInput != null)
            {
                _textInput.Input -= OnTextInput;

                if (_textInput.IsVisible)
                    _textInput.Hide();
            }

            _textInput = textInput;

            if (_textInput != null)
            {
                _textInput.Input += OnTextInput;

                if (_inputConnection != null)
                {
                    UpdateTextContext();
                    _textInput.Show();
                }
            }
        }

        private void SetInputConnection(IInputConnection? inputConnection)
        {
            _inputConnection = inputConnection;

            if (_textInput == null)
                return;

            if (_inputConnection == null)
            {
                if (_textInput.IsVisible)
                    _textInput.Hide();

                return;
            }

            UpdateTextContext();
            _textInput.Show();
        }

        private void OnTextInput(TextInputEvent input)
        {
            _ = _mainThread.ExecuteAsync(() =>
            {
                if (_inputConnection == null)
                    return;

                switch (input.Type)
                {
                    case TextInputEventType.CommitText:
                        _inputConnection.CommitText(input.Text ?? "", 1);
                        break;

                    case TextInputEventType.Backspace:
                        SendKey(Keycode.Del);
                        break;

                    case TextInputEventType.Enter:
                        SendKey(Keycode.Enter);
                        break;
                }

                //UpdateTextContext();
            });
        }

        private void SendKey(Keycode key)
        {
            if (_inputConnection == null)
                return;

            _inputConnection.SendKeyEvent(new KeyEvent(KeyEventActions.Down, key));
            _inputConnection.SendKeyEvent(new KeyEvent(KeyEventActions.Up, key));
        }

        private void UpdateTextContext()
        {
            if (_textInput == null || _inputConnection == null)
                return;

            _mainThread.Post(() =>
            {
                try
                {
                    var before = _inputConnection.GetTextBeforeCursor(2048, 0)?.ToString() ?? "";
                    var selected = _inputConnection.GetSelectedText(0)?.ToString() ?? "";
                    var after = _inputConnection.GetTextAfterCursor(2048, 0)?.ToString() ?? "";

                    _textInput.SetText(before + selected + after);
                }
                catch (Exception ex)
                {
                    Log.Warn(this.GetType().Name, ex.ToString());
                    _textInput.SetText("");

                }
            });
   
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
            _webView.Settings.CacheMode = CacheModes.NoCache;

            _webView.Settings.SetSupportZoom(false);
            _webView.Settings.DefaultZoom = ZoomDensity.Far;
            _webView.Settings.BuiltInZoomControls = false;
            //_webView.Settings.UseWideViewPort = true;
            //_webView.Settings.LoadWithOverviewMode = true;

            _webView.SetLayerType(LayerType.Hardware, null);

            if (_context is Activity activity)
            {
                var layout = new ViewGroup.LayoutParams((int)(_size.Width * 1.3f), (int)(_size.Height * 1.3f));
                activity.AddContentView(_webView, layout);
            }
        }

        public HandlerXrThread MainThread => _mainThread;

        public Func<IWebResourceRequest, WebResourceResponse?>? ShouldInterceptRequest { get; set; }

        public WebView? WebView => _webView;
    }
}