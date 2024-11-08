using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public enum GraphicDriver
    {
        OpenGL,
        FilamentOpenGL,
        FilamentVulkan
    }


    public class XrEngineAppOptions
    {
        public GraphicDriver Driver { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public float ResolutionScale { get; set; }

        public uint SampleCount { get; set; }

        public object? DriverOptions { get; set; }
    }

    public class XrEngineApp
    {
        private XrApp? _xrApp;
        private EngineApp? _app;
        private readonly XrEngineAppOptions _options;
        private readonly IXrEnginePlatform _platform;

        public XrEngineApp(XrEngineAppOptions options, IXrEnginePlatform platform)
        {
            _options = options;
            _platform = platform;
        }

        public void Create(EngineApp app)
        {
            _platform.CreateDrivers(_options, out var renderEngine, out var xrDriver);

            _app = app;
            _app.Renderer = renderEngine;

            _xrApp = _platform.CreateXrApp(xrDriver);

            _xrApp.RenderOptions.SampleCount = _options.SampleCount;
            _xrApp.RenderOptions.RenderMode = _options.RenderMode;
            _xrApp.RenderOptions.ResolutionScale = _options.ResolutionScale;
        }

        public T GetInputs<T>()
        {
            return (T)(Inputs ?? throw new ArgumentNullException());
        }

        public void EnterXr()
        {
            _xrApp?.Start();
        }

        public void ExitXr()
        {
            _xrApp?.Stop();
        }

        public EngineApp App => _app!;

        public XrApp XrApp => _xrApp!;

        public IXrBasicInteractionProfile? Inputs { get; internal set; }
    }


}
