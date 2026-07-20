using CefSharp;
using Common.Interop;
using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Text.Json;
using XrEngine.UI.Web;
using XrInteraction;
using XrMath;

namespace XrEngine.Browser.Windows
{
    public class ChromeWebBrowserView : AsyncBehavior<TriangleMesh>
    {
        static readonly JsonSerializerOptions JSON = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true
        };

        protected bool _isInit;
        protected ChromeWebBrowser _browser;
        protected DateTime _lastTexUpdateTime;
        protected ISurfaceInput? _input;
        protected string? _source;
        protected Texture2D? _texture;
        protected long _lastElevFrame;
        protected bool _injected;
        protected readonly bool _cpuMode;

        public ChromeWebBrowserView(GL? gl = null)
        {
            _cpuMode = gl == null;
            _browser = new ChromeWebBrowser(gl);

            TextureFormat = TextureFormat.Bgra32;
            Size = new Size2I(1600, 1200);
            EnableElevation = true;
        }

        protected async Task UpdateEleveationAsync()
        {
            Debug.Assert(_host?.Geometry != null);

            string? json = null;

            try
            {
                if (!_injected)
                    await InjectScripts();

                json = await _browser.Chromium.GetMainFrame().EvaluateScriptAsync<string>("domBridge.getElevatedElementsJson()");
            }
            catch
            {
            }

            json ??= "[]";

            var elevs = JsonSerializer.Deserialize<XrElevatedElement[]>(json, JSON)!;

            var bulder = new MeshBuilder();

            foreach (var ele in elevs)
            {
                var rect = ele.TextureRect
                    .Scale(1f / Size.Width, 1f / Size.Height);

                rect = new Rect2(
                    rect.X,
                    1f - rect.Y - rect.Height,
                    rect.Width,
                    rect.Height);

                bulder.AddQuad(rect.Translate(-0.5f, -0.5f), rect, ele.Elevation);
            }

            bulder.AddQuad(new Rect2(-0.5f, -0.5f, 1, 1), 0, false);

            await EngineApp.MainThread;

            bulder.ToGeometry(_host.Geometry, false);

            _host.UpdateBounds();

            var styles = elevs.Select(a => new QuadStyle
            {
                BackColor = a.Elevation < 0 ? Color.Transparent : a.Background,
                Opacity = a.Opacity
            }).Union([new()]).ToArray();

            foreach (var mat in _host.Materials.OfType<TextureCutMaterial>())
            {
                mat.Styles = styles;
                mat.Invalidate();
            }

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

            if (EnableElevation)
            {
                _host!.Geometry = new Geometry3D()
                {
                    ActiveComponents = VertexComponent.Position | VertexComponent.Normal | VertexComponent.UV0
                };

                _host!.Flags |= EngineObjectFlags.NoLogs;
                _host!.Geometry!.Flags |= EngineObjectFlags.NoLogs;

            }

            _texture ??= new Texture2D()
            {
                Name = "Browser",
                Format = TextureFormat,
            };

            if (_host!.Materials.Count == 0 || _host.Materials[0] is not TextureMaterial)
            {
                _host.Materials.Clear();

                if (EnableElevation)
                {
                    _host.Materials.Add(new TextureCutMaterial()
                    {
                        Alpha = AlphaMode.Blend,
                        Texture = _texture,
                        Mode = TextureCutMode.Layers,
                        Priority = 0
                    });

                    _host.Materials.Add(new TextureCutMaterial()
                    {
                        Alpha = AlphaMode.Blend,
                        Texture = _texture,
                        Mode = TextureCutMode.Main,
                        Priority = 1
                    });
                }
                else
                    _host.Materials.Add(new TextureMaterial(_texture));
            }

            _input = _host!.DescendantsOrSelfComponents<ISurfaceInput>().First();

        }

        private async void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
                await InjectScripts();
        }

        protected async Task InjectScripts()
        {
            if (EnableElevation)
            {
                var script = Embedded.GetString<UI.Web.IWebBrowser>("Scripts/XrDomBridge.js");

                await _browser.Chromium.GetMainFrame().EvaluateScriptAsync(script);
            }

            _injected = true;
        }

        protected override void UpdateSync(RenderContext ctx)
        {
            if (!_isInit || _input == null)
                return;

            if (_input.IsPointerValid)
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
            if (!_isInit || _texture == null)
                return;

            if (_cpuMode)
            {
                _texture.SetFlag(EngineObjectFlags.EnableDebug, false);

                _texture.Type = TextureType.Buffer;

                var time = _browser.FrameBufferTime;

                if (_browser.FrameBuffer != null && _lastTexUpdateTime != time)
                {
                    _texture.LoadData(new TextureData()
                    {
                        Data = MemoryBuffer.Create(_browser.FrameBuffer),
                        Width = _browser.Size.Width,
                        Height = _browser.Size.Height,
                        Format = TextureFormat
                    });

                    _lastTexUpdateTime = time;
                }
            }
            else
            {
                if (_texture != null)
                    await _browser.UpdateTextureAsync(_texture);
            }

            if (EnableElevation && _browser.Frame != _lastElevFrame)
            {
                await UpdateEleveationAsync();
                _lastElevFrame = _browser.Frame;
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

        public bool EnableElevation { get; set; }

        public TextureFormat TextureFormat { get; set; }

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
