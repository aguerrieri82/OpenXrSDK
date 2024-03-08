using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEditor.Components;
using XrEngine;
using XrEngine.OpenXr;

namespace XrEditor
{
    public class EditorPlatform : IXrPlatform, IRenderSurfaceProvider
    {
        IRenderSurface? _renderSurface;

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            if (options.Driver == GraphicDriver.OpenGL)
                _renderSurface = new GlRenderHost();
            else if (options.Driver == GraphicDriver.FilamentOpenGL)
                _renderSurface = new FlGlRenderHost();
            else
                _renderSurface = new FlVulkanRenderHost();

            renderEngine = _renderSurface.CreateRenderEngine();

            xrDriver = ((IXrGraphicProvider)_renderSurface).CreateXrDriver();
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            return new XrApp(NullLogger.Instance,
                     xrDriver,
                     new OculusXrPlugin());
        }

        public IRenderSurface RenderSurface => _renderSurface!;

         public IAssetManager AssetManager => throw new NotImplementedException();
    }
}
