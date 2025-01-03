﻿
using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.IO;
using System.Net.NetworkInformation;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;


namespace XrEditor
{
    public class EditorPlatform : IXrEnginePlatform, IRenderSurfaceProvider
    {
        IRenderSurface? _renderSurface;
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

        public EditorPlatform(string persistentPath = "Data")
        {
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
                _renderSurface = new GlRenderHost();
            else if (driver == GraphicDriver.FilamentOpenGL)
                _renderSurface = new FlGlRenderHost();
            else
                _renderSurface = new FlVulkanRenderHost();

            return _renderSurface;
        }

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            renderEngine = _renderSurface!.CreateRenderEngine(options.DriverOptions);

            xrDriver = ((IXrGraphicProvider)_renderSurface).CreateXrDriver();

            Context.Implement(new RenderPreviewCreator(renderEngine));
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            return new XrApp(NullLogger.Instance,
                     xrDriver,
                     new OculusXrPlugin());
        }

        public IRenderSurface RenderSurface => _renderSurface!;

        public string PersistentPath { get; }

        public string CachePath => Path.GetFullPath("Cache");

        public string Name => "Editor";

        public DeviceInfo Device => _info;
    }
}
