using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Text.Json;
using XrEngine.UI.Web;
using XrMath;

namespace XrEngine.Browser.Windows
{
    public class ChromeWebBrowser : IDisposable, UI.Web.IWebBrowser
    {

        protected ChromiumWebBrowser? _browser;
        protected IRequestContext? _requestContext;
        protected IBrowserHost? _host;
        protected byte[]? _buffer;
        protected DateTime _bufferTime;
        protected float _zoomLevel;
        protected string? _startUrl;
        protected readonly GL? _gl;
        protected long _frame;
        private GlRenderHandler? _handler;

        public ChromeWebBrowser(GL? gl = null)
        {
            FrameRate = 60;
            CachePath = Path.GetFullPath("browser");
            ZoomLevel = 1;
            Size = new Size2I(800, 600);
            DpiScale = 1;
            _startUrl = "about:blank";
            _gl = gl;

            StereoIpd = 0.064f;
            StereoPixelsPerMeter = new Vector2(1000, 1000);
        }

        public async Task CreateAsync(string? startUrl = null)
        {
            await InitAsync();

            await EngineApp.RenderThread;

            var browserSettings = new BrowserSettings
            {
                WebGl = CefState.Enabled,
                Javascript = CefState.Enabled,
                LocalStorage = CefState.Enabled,
                Databases = CefState.Enabled,
                WindowlessFrameRate = FrameRate,
            };

            var requestContextSettings = new RequestContextSettings
            {
                CachePath = Path.GetFullPath(CachePath),
            };

            _requestContext = new RequestContext(requestContextSettings);

            _browser = new ChromiumWebBrowser(
                startUrl ?? _startUrl,
                browserSettings,
                _requestContext,
                false);

            if (_gl != null)
            {
                _handler = new GlRenderHandler(
                    _gl,
                    (int)Size.Width,
                    (int)Size.Height);

                _browser.RenderHandler = _handler;
            }
            else
            {
                _browser.Paint += OnPaint;
            }

            var windowInfo = new WindowInfo();

            windowInfo.SetAsWindowless(IntPtr.Zero);
            windowInfo.SharedTextureEnabled = _gl != null;

            _browser.CreateBrowser(windowInfo);
            _browser.FrameLoadStart += OnFrameLoad;
            _browser.JavascriptMessageReceived += OnMessage;

            Log.Info(this, "Wait for page load");

            await _browser.WaitForInitialLoadAsync();

            _host = _browser.GetBrowserHost();

            if (_gl != null)
                _ = UpdateAsync();
            else
                await UpdateAsync();
        }

        private void OnMessage(object? sender, JavascriptMessageReceivedEventArgs e)
        {
            var str = e.Message.ToString();

            MessageReceived?.Invoke(this, new MessageReceivedArgs(str!));
        }

        public void UpdatePointer(int id, Vector2 pos, TouchEventType eventType, CefEventFlags flags = CefEventFlags.None)
        {
            pos.Y = 1 - pos.Y;

            var viewPos = pos * new Vector2(Size.Width, Size.Height);

            _host!.SendTouchEvent(new CefSharp.Structs.TouchEvent
            {
                Id = id,
                PointerType = PointerType.Touch,
                Modifiers = flags,
                Type = eventType,
                X = viewPos.X,
                Y = viewPos.Y,
            });

            if (eventType == TouchEventType.Moved)
            {
                _host!.SendMouseMoveEvent((int)viewPos.X, (int)viewPos.Y, false, flags);
            }
            else
                Log.Debug(this, "{0} - {1}", pos, eventType);

        }

        public async Task UpdateTextureAsync(Texture2D tex, bool force = false)
        {
            if (tex.Handle == 0)
                return;

            if (_browser?.RenderHandler is not GlRenderHandler handler)
                return;

            if (!handler.FrameReady && !force)
                return;

            if (tex.Width != Size.Width || tex.Height != Size.Height)
            {
                tex.LoadData(new TextureData()
                {
                    Width = Size.Width,
                    Height = Size.Height,
                    Format = tex.Format
                });
            }

            await EngineApp.RenderThread;

            handler.UpdateTexture((uint)tex.Handle);
        }

        private void OnFrameLoad(object? sender, FrameLoadStartEventArgs e)
        {
            if (e.Frame.IsMain)
                _browser.SetZoomLevel(ZoomLevel);
        }

        private unsafe void OnPaint(object? sender, OnPaintEventArgs e)
        {
            var bufSize = e.Width * e.Height * 4;

            if (_buffer == null || _buffer.Length != bufSize)
                _buffer = new byte[bufSize];

            fixed (byte* pDest = _buffer)
                System.Buffer.MemoryCopy((void*)e.BufferHandle, pDest, bufSize, bufSize);

            _bufferTime = DateTime.UtcNow;

            _frame++;
        }

        async Task InitAsync()
        {
            Log.Info(this, "Init Browser");

            var settings = new CefSettings()
            {
                CachePath = CachePath,
                Locale = "it",
                WindowlessRenderingEnabled = true,
                BackgroundColor = 0xFFFFFF,
            };

            settings.EnableAudio();

            if (RequestHandler != null)
            {
                settings.RegisterScheme(new CefCustomScheme()
                {
                    IsCorsEnabled = true,
                    IsFetchEnabled = true,
                    SchemeName = RequestHandler.Scheme,
                    IsSecure = true,
                    SchemeHandlerFactory = new ChromeSchemeHandlerFactory(RequestHandler)
                });
            }

            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";
            settings.CefCommandLineArgs["force-high-performance-gpu"] = "1";

            Cef.EnableWaitForBrowsersToClose();

            var success = await Cef.InitializeAsync(settings);
            if (!success)
                throw new Exception();
        }

        public void ShowDevTools()
        {
            _browser?.ShowDevTools();
        }

        public void Reload()
        {
            _browser?.Reload();
        }

        public void Dispose()
        {
            Cef.WaitForBrowsersToClose();
            Cef.Shutdown();
        }

        public async Task UpdateAsync()
        {
            if (_browser == null)
                return;

            Log.Info(this, "Update Size");

            await _browser.ResizeAsync((int)Size.Width, (int)Size.Height, DpiScale);
        }

        public async Task NavigateAsync(string uri)
        {
            if (_browser == null)
            {
                _startUrl = uri;
                return;
            }

            Log.Info(this, "Navigate {0}", uri);

            await _browser.LoadUrlAsync(uri);
        }

        public async Task PostMessageAsync(string message)
        {
            await _browser.EvaluateScriptAsync("postMessage(" + JsonSerializer.Serialize(message) + ")");
        }

        public IWebRequestHandler? RequestHandler { get; set; }

        public byte[]? FrameBuffer => _buffer;

        public DateTime FrameBufferTime => _bufferTime;

        public Size2I Size { get; set; }

        public float DpiScale { get; set; }

        public float ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (_zoomLevel == value)
                    return;

                _zoomLevel = value;
                _browser?.SetZoomLevel(_zoomLevel);
            }
        }

        public ChromiumWebBrowser? Chromium => _browser;

        public event EventHandler<MessageReceivedArgs>? MessageReceived;

        public string CachePath { get; set; }

        public int FrameRate { get; set; }

        public float StereoIpd { get; set; }

        public Vector2 StereoPixelsPerMeter { get; set; }

        public long Frame => _handler?.Frame ?? _frame;
    }
}