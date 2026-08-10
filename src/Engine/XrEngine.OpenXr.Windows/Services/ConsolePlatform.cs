#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using System.Net.NetworkInformation;
using XrEngine.OpenGL;
using OpenXr.Framework.Angle;

namespace XrEngine.OpenXr.Windows
{
    public class ConsolePlatform : IXrEnginePlatform, IGlContextProvider
    {
        readonly ViewManager _viewManager;
        readonly string _basePath;
        private readonly DeviceInfo _info;

        public ConsolePlatform()
            : this(".")
        {
            Context.Implement<IGlContextProvider>(this);
        }

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

        public ConsolePlatform(string basePath)
        {
            _basePath = basePath;
            _info = new DeviceInfo
            {
                Id = GetMacAddress(),
                Name = Environment.MachineName
            };

            _viewManager = new ViewManager();
            _viewManager.Initialize();

            Context.Implement<IProgressLogger>(new ProgressLogger());
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets"));

           PersistentPath = Path.Combine(_basePath, "Data");
           CachePath = Path.Combine(_basePath, "Cache");
        }

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            var glOptions = options.DriverOptions as GlRenderOptions ?? new GlRenderOptions();

            if (options.Driver == GraphicDriver.OpenGL)
            {
#if GLES
                var gl = _viewManager.View.CreateOpenGLES();
#else
                var gl = _viewManager.View.CreateOpenGL();
#endif
                renderEngine = new OpenGLRender(gl, glOptions);

                xrDriver = new XrOpenGLGraphicDriver(_viewManager.View);

            }
            else if (options.Driver == GraphicDriver.Angle)
            {
                var ctx = new AngleVulkanContext((int)options.SampleCount);

                Context.Implement(ctx);

                var angleDriver = new XrAngleGraphicDriver(ctx);

                ctx.Initialize([], []);

                renderEngine = new OpenGLRender(ctx.Gl!, glOptions, useAngle: true);

                xrDriver = angleDriver;
            }
            else
                throw new NotSupportedException();

        }

        public XrApp CreateXrApp(IList<IXrPlugin> plugins)
        {
            return new XrApp(NullLogger.Instance, [.. plugins]);
        }

        public IGlContext CreateShared()
        {
            throw new NotSupportedException();
        }

        public string PersistentPath { get; set; }

        public string CachePath { get; set; }

        public string Name => "Console";

        public DeviceInfo Device => _info;

        public IGlContext? Current => null;
    }
}
