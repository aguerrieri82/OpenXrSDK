using CefSharp;
using Common.Interop;
using Silk.NET.OpenGL;
using XrEngine.UI.Web;
using XrInteraction;
using XrMath;

namespace XrEngine.Browser.Win
{
    public class ChromeWebBrowserView : AsyncBehavior<TriangleMesh>
    {
        protected bool _isInit;
        protected ChromeWebBrowser _browser;
        protected DateTime _lastTexUpdateTime;
        protected ISurfaceInput? _input;
        protected string? _source;
        protected readonly bool _cpuMode;

        public ChromeWebBrowserView(GL? gl = null)
        {
            _cpuMode = gl == null;
            _browser = new ChromeWebBrowser(gl);
            Size = new Size2I(1600, 1200);
        }

        protected override async Task StartAsync(RenderContext ctx)
        {
            if (!_isInit)
            {
                if (RequestHandler != null)
                    _browser.RequestHandler = RequestHandler;

                await _browser.CreateAsync(_source);

                _browser.Chromium!.LoadingStateChanged += OnLoadingStateChanged;

                _isInit = true;

                Log.Info(this, "Browser ready");
            }

            if (_host!.Materials.Count == 0 || _host.Materials[0] is not TextureMaterial)
            {
                _host.Materials.Clear();

                _host.Materials.Add(new TextureMaterial()
                {
                    Texture = new Texture2D()
                    {
                        Name = "Browser",
                        Format = TextureFormat.Rgba32,
                    }
                });
            }

            _input = _host!.DescendantsOrSelfComponents<ISurfaceInput>().First();
        }

        private async void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                var script = Embedded.GetString<ChromeWebBrowserView>("stereo.js");

                await _browser.Chromium.GetMainFrame().EvaluateScriptAsync(script);
            }
        }

        protected override void UpdateSync(RenderContext ctx)
        {
            if (!_isInit || _input == null)
                return;

            if (_input!.IsPointerValid)
            {
                if (_input.MainButton.IsChanged)
                {
                    if (_input.MainButton.IsDown)
                        _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Pressed, CefEventFlags.IsLeft | CefEventFlags.LeftMouseButton);
                    else
                        _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Released, CefEventFlags.IsLeft | CefEventFlags.LeftMouseButton);
                }
                else
                    _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Moved, _input.MainButton.IsDown ? CefEventFlags.IsLeft | CefEventFlags.LeftMouseButton : CefEventFlags.None);
            }
        }

        protected override async Task UpdateAsync(RenderContext ctx)
        {
            if (!_isInit)
                return;

            if (_cpuMode)
            {
                if (_host?.Materials[0] is not TextureMaterial tex || tex.Texture == null)
                    return;

                tex.Texture.SetFlag(EngineObjectFlags.EnableDebug, false);

                tex.Texture.Type = TextureType.Buffer;

                var time = _browser.FrameBufferTime;

                if (_browser.FrameBuffer != null && _lastTexUpdateTime != time)
                {
                    tex.Texture.LoadData(new TextureData()
                    {
                        Data = MemoryBuffer.Create(_browser.FrameBuffer),
                        Width = _browser.Size.Width,
                        Height = _browser.Size.Height,
                        Format = TextureFormat.Bgra32
                    });

                    _lastTexUpdateTime = time;
                }
            }
            else
            {
                if (_host?.Materials[0] is TextureMaterial tex && tex.Texture != null)
                {
                    await _browser.UpdateTextureAsync(tex.Texture);
                }
            }
        }


        [Action]
        public void ShowDevTools()
        {
            _browser.ShowDevTools();
        }

        [Action]
        public void Reload()
        {
            _browser.Reload();
        }

        public ChromeWebBrowser Browser => _browser;

        public IWebRequestHandler? RequestHandler { get; set; }


        [Range(-10, 10, 0.1f)]
        public float ZoomLevel
        {
            get => _browser.ZoomLevel;
            set => _browser.ZoomLevel = value;
        }

        public Size2I Size
        {
            get => _browser.Size;
            set => _browser.Size = value;
        }

        public string? Source
        {
            get => _source;
            set
            {
                if (value == _source)
                    return;
                _source = value;
                _ = _browser.NavigateAsync(_source ?? "about:blank");
            }
        }
    }
}
