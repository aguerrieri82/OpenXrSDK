
#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using OpenXr.Framework.Angle;
using OpenXr.Framework.OpenGL;
using System.IO;
using System.Net.NetworkInformation;
using XrEditor.Services;
using XrEngine;
using XrEngine.Filament.Wpf;
using XrEngine.OpenGL.Wpf;
using XrEngine.OpenXr;

namespace XrEditor
{
    public class EditorPlatform : IXrEnginePlatform, IRenderSurfaceProvider
    {
        #region STRUCTS

        readonly struct GlHostDevice : IOpenGLDevice
        {
            readonly GlRenderHost _host;

            public GlHostDevice(GlRenderHost host)
            {
                _host = host;
            }

            public readonly nint HDc => _host.HDc;

            public readonly nint GlCtx => _host.GlCtx;

            public readonly GL Gl => _host.Gl;
        }

        readonly struct GlDxHostDevice : IOpenGLDevice
        {
            readonly GlDxRenderHost _host;

            public GlDxHostDevice(GlDxRenderHost host)
            {
                _host = host;
            }

            public readonly nint HDc => _host.HDc;

            public readonly nint GlCtx => _host.GlCtx;

            public readonly GL Gl => _host.Gl;
        }

        #endregion

        IRenderSurface? _renderSurface;
        private readonly bool _useEs;
        private readonly DeviceInfo _info;

        static string GetMacAddress()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in networkInterfaces)
            {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                    netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var macAddress = netInterface.GetPhysicalAddress().ToString();
                    return macAddress;
                }
            }
            return "";
        }

        public EditorPlatform(string persistentPath = "Data", bool useEs = false)
        {
            _useEs = useEs;
            _info = new DeviceInfo
            {
                Id = GetMacAddress(),
                Name = Environment.MachineName
            };

            PersistentPath = Path.GetFullPath(persistentPath);
        }

        public IRenderSurface CreateRenderSurface(GraphicDriver driver)
        {
            if (driver == GraphicDriver.OpenGL)
            {
                if (EngineNativeLib.RdcIsAttached() || !EditorDebug.UseDxHost)
                    _renderSurface = new GlRenderHost(true, _useEs);
                else
                    _renderSurface = new GlDxRenderHost(true, _useEs);
            }
            else if (driver == GraphicDriver.FilamentOpenGL)
                _renderSurface = new FlGlRenderHost();
            else if (driver == GraphicDriver.Angle)
                _renderSurface = new AngleGlRenderHost();
            else
                _renderSurface = new FlVulkanRenderHost();

            return _renderSurface;
        }

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            renderEngine = _renderSurface!.CreateRenderEngine(options.DriverOptions);

            if (_renderSurface is GlRenderHost glHost)
                xrDriver = new XrOpenGLGraphicDriver(new GlHostDevice(glHost));

            else if (_renderSurface is GlDxRenderHost glDxHost)
                xrDriver = new XrOpenGLGraphicDriver(new GlDxHostDevice(glDxHost));

            else if (_renderSurface is FlVulkanRenderHost flVulkan)
                xrDriver = flVulkan.CreateXrDriver();

            else if (_renderSurface is AngleGlRenderHost angleHost)
                xrDriver = new XrAngleGraphicDriver(angleHost.AngleContext);

            else
                throw new NotSupportedException();

            Context.Implement(new RenderPreviewCreator(renderEngine));
        }

        public XrApp CreateXrApp(IList<IXrPlugin> plugins)
        {
            return new XrApp(new NetLoggerProgressLogger(), [.. plugins]);
        }

        public IRenderSurface RenderSurface => _renderSurface!;

        public string PersistentPath { get; }

        public string CachePath => @"D:\Projects\XrEditor\Cache";

        public string SharedPath => @"D:\Projects\XrEditor\Storage";

        public string Name => "Editor";

        public DeviceInfo Device => _info;
    }
}
