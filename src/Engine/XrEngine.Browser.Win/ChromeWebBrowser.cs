﻿using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using System.Numerics;
using System.Threading.Tasks.Dataflow;
using XrMath;

namespace XrEngine.Browser.Win
{
    public class ChromeWebBrowser : IDisposable
    {
        protected ChromiumWebBrowser? _browser;
        protected IRequestContext? _requestContext;
        protected IBrowserHost? _host;
        protected byte[]? _buffer;
        private DateTime _bufferTime;
        private float _zoomLevel;

        public ChromeWebBrowser()
        {
            FrameRate = 30;
            CachePath = Path.GetFullPath("browser");
            ZoomLevel = 1;
            Size = new Size2I(800, 600);
            DpiScale = 1;
        }

        public async Task CreateAsync(string startUrl = "about:blank")
        {
            await InitAsync();

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

            _browser = new ChromiumWebBrowser(startUrl, browserSettings, _requestContext);
          
            _browser.Paint += OnPaint;
            _browser.FrameLoadStart += OnFrameLoad;

            Log.Info(this, "Wait for page load");

            await _browser.WaitForInitialLoadAsync();

            _host = _browser.GetBrowserHost();

            await UpdateAsync();

        }

        public void UpdatePointer(int id, Vector2 pos, TouchEventType eventType, CefEventFlags flags = CefEventFlags.None)
        {
            pos.Y = 1 - pos.Y;

            var viewPos = pos * new Vector2(Size.Width, Size.Height);

            _host!.SendTouchEvent(new CefSharp.Structs.TouchEvent
            {
                Id = id,
                PointerType = PointerType.Mouse,
                Modifiers = flags,
                Type = eventType,
                X = viewPos.X,
                Y = viewPos.Y,
            });
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
                Buffer.MemoryCopy((void*)e.BufferHandle, pDest, bufSize, bufSize);

           _bufferTime = DateTime.Now;
        }

        async Task InitAsync()
        {
            Log.Info(this, "Init Browser");
            var settings = new CefSettings()
            {
                CachePath = CachePath,
                Locale = "it",
                WindowlessRenderingEnabled = true,
            };

            settings.EnableAudio();
            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";

            Cef.EnableWaitForBrowsersToClose();

            var success = await Cef.InitializeAsync(settings);
            if (!success)
                throw new Exception();



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
                return;

            Log.Info(this, "Navigate {0}", uri);
            await _browser.LoadUrlAsync(uri);
        }

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

        public string CachePath { get; set; }

        public int FrameRate { get; set; }
    }
}