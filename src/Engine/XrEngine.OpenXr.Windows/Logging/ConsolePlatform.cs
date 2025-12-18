using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using Silk.NET.OpenGL;
using System.Net.NetworkInformation;
using XrEngine.OpenGL;


namespace XrEngine.OpenXr.Windows
{
    public class ConsolePlatform : IXrEnginePlatform
    {
        readonly ViewManager _viewManager;
        readonly string _basePath;
        private readonly DeviceInfo _info;

        public ConsolePlatform()
            : this(".")
        {

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

        }

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            var glOptions = options.DriverOptions as GlRenderOptions ?? new GlRenderOptions();

            renderEngine = new OpenGLRender(_viewManager.View.CreateOpenGL(), glOptions);
            xrDriver = new XrOpenGLGraphicDriver(_viewManager.View);
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            return new XrApp(NullLogger.Instance,
                     xrDriver,
                     new OculusXrPlugin());
        }

        public string PersistentPath => Path.Combine(_basePath, "Data");

        public string CachePath => Path.Combine(_basePath, "Cache");

        public string Name => "Console";

        public DeviceInfo Device => _info;
    }
}
