using OpenXr.Framework;
using Silk.NET.OpenGL;

namespace XrEngine.OpenXr
{
    public enum GraphicDriver
    {
        OpenGL,
        FilamentOpenGL,
        FilamentVulkan,
        Angle
    }

    public enum XrProjDepthMode
    {
        None,
        DepthPass,
        DepthCopy,
        DepthCopyImage
    }

    public class XrEngineAppOptions
    {
        public XrEngineAppOptions()
        {
            XrPlugins = [];
        }

        public GraphicDriver Driver { get; set; }

        public XrRenderMode RenderMode { get; set; }

        public XrProjDepthMode ProjDepthMode { get; set; }

        public float ProjDepthScale { get; set; }

        public float ResolutionScale { get; set; }

        public uint SampleCount { get; set; }

        public bool UseIntermediate { get; set; }

        public object? DriverOptions { get; set; }

        public List<IXrPlugin> XrPlugins { get; }
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
            Current = this;
        }

        public void Create(EngineApp app)
        {
            _platform.CreateDrivers(_options, out var renderEngine, out var xrDriver);

            _app = app;
            _app.Renderer = renderEngine;

            _xrApp = _platform.CreateXrApp([xrDriver, ..Options.XrPlugins]);

            _xrApp.RenderOptions.SampleCount = _options.UseIntermediate ? 1 : _options.SampleCount;
            _xrApp.RenderOptions.RenderMode = _options.RenderMode;
            _xrApp.RenderOptions.ColorScale = _options.ResolutionScale;
            _xrApp.RenderOptions.UseProjectionDepth = _options.ProjDepthMode != XrProjDepthMode.None;

            if (_xrApp.RenderOptions.SampleCount > 1)
            {
                _xrApp.RenderOptions.ProjectionDepthScale = _options.ProjDepthScale;

                if (_options.Driver == GraphicDriver.OpenGL)
                {
                    _xrApp.RenderOptions.DepthFormat = (int)GLEnum.DepthComponent16;
                }
            }
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

        public XrEngineAppOptions Options => _options;

        public static XrEngineApp? Current { get; private set; }

    }

}
