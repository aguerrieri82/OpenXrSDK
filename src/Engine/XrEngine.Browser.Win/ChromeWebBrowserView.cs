using CefSharp;
using System.Diagnostics;
using XrEngine.UI.Web;
using XrInteraction;
using XrMath;

namespace XrEngine.Browser.Win
{
    public class ChromeWebBrowserView : Behavior<TriangleMesh>
    {
        protected bool _isInit;
        protected ChromeWebBrowser _browser;
        protected DateTime _lastTexUpdateTime;
        protected ISurfaceInput? _input;
        protected string? _source;

        public ChromeWebBrowserView()
        {
            _browser = new ChromeWebBrowser();
            Size = new Size2I(1600, 1200);
        }

        protected override void Start(RenderContext ctx)
        {
            if (!_isInit)
            {
                _ = Task.Run(async () =>
                {
                    if (RequestHandler != null)
                        _browser.RequestHandler = RequestHandler;

                    await _browser.CreateAsync(_source);

                    //_browser.ShowDevTools();
                    _isInit = true;
                    Log.Info(this, "Browser ready");
                });
            }

            if (_host!.Materials.Count == 0 || _host.Materials[0] is not TextureMaterial)
            {
                _host.Materials.Clear();
                _host.Materials.Add(new TextureMaterial()
                {
                    Texture = new Texture2D()
                    {
                        Name = "Browser",
                        Format = TextureFormat.Rgba32
                    }
                });
            }

            _input = _host!.DescendantsOrSelfComponents<ISurfaceInput>().First();
        }

        protected override void Update(RenderContext ctx)
        {
            if (!_isInit)
                return;

            var texture = (_host?.Materials[0] as TextureMaterial)?.Texture;
            if (texture == null)
                return;

            texture.SetFlag(EngineObjectFlags.EnableDebug, false);

            var time = _browser.FrameBufferTime;

            if (_browser.FrameBuffer != null && _lastTexUpdateTime != time)
            {
                texture.LoadData(new TextureData()
                {
                    Data = new Memory<byte>(_browser.FrameBuffer),
                    Width = _browser.Size.Width,
                    Height = _browser.Size.Height,
                    Format = TextureFormat.Bgra32
                });

                _lastTexUpdateTime = time;
            }

            if (!_input!.IsPointerValid)
                return;

            if (_input.MainButton.IsChanged)
            {
                if (_input.MainButton.IsDown)
                    _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Pressed, CefEventFlags.LeftMouseButton);
                else
                    _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Released,  CefEventFlags.LeftMouseButton);
            }
            else
                _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Moved, _input.MainButton.IsDown ? CefEventFlags.LeftMouseButton : CefEventFlags.None);

        }

        [Action]
        public void ShowDevTools()
        {
            _browser.ShowDevTools();
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
