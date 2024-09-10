﻿using CefSharp;
using XrEngine.UI.Web;
using XrInteraction;

namespace XrEngine.Browser.Win
{
    public class WebBrowserView : Behavior<TriangleMesh>
    {
        protected bool _isInit;
        protected ChromeWebBrowser _browser;
        protected DateTime _lastUpdateTime;
        protected ISurfaceInput? _input;
        protected string? _source;

        public WebBrowserView()
        {
            _browser = new ChromeWebBrowser();
            _browser.Size = new XrMath.Size2I(1600, 1200);

        }

        protected override void Start(RenderContext ctx)
        {
            if (!_isInit)
            {
                _ = Task.Run(async () =>
                {
                    _browser.RequestHandler = RequestHandler;
                    await _browser.CreateAsync(_source ?? "about:blank");
                    _isInit = true;
                    Log.Info(this, "Browser ready");
                });
            }

            _host!.Materials.Clear();
            _host.Materials.Add(new TextureMaterial()
            {
                Texture = new Texture2D()
            });

            _input = _host!.DescendantsOrSelfComponents<ISurfaceInput>().First();

            base.Start(ctx);
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

            if (_browser.FrameBuffer != null && _lastUpdateTime != time)
            {
                texture.LoadData(new TextureData()
                {
                    Data = new Memory<byte>(_browser.FrameBuffer),
                    Width = _browser.Size.Width,
                    Height = _browser.Size.Height,
                    Format = TextureFormat.Bgra32
                });

                _lastUpdateTime = time;
            }

            if (_input!.IsPointerValid)
            {
                if (_input.MainButton.IsChanged)
                {
                    if (_input.MainButton.IsDown)
                        _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Pressed, CefEventFlags.IsLeft | CefEventFlags.LeftMouseButton);

                    else
                        _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Released, CefEventFlags.IsLeft | CefEventFlags.LeftMouseButton);
                }
         
                _browser.UpdatePointer(0, _input.Pointer, CefSharp.Enums.TouchEventType.Moved);
            }

            base.Update(ctx);
        }

        public ChromeWebBrowser Browser => _browser;

        public IWebRequestHandler? RequestHandler { get; set; }  


        [Range(-10, 10, 0.1f)]
        public float ZoomLevel
        {
            get => _browser.ZoomLevel;
            set => _browser.ZoomLevel = value;
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
