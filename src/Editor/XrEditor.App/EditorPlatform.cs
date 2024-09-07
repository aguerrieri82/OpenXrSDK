
using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using System.IO;
using XrEditor.Services;
using XrEngine;
using XrEngine.OpenXr;


namespace XrEditor
{
    public class EditorPlatform : IXrEnginePlatform, IRenderSurfaceProvider
    {
        IRenderSurface? _renderSurface;

        public EditorPlatform()
        {
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
            renderEngine = _renderSurface!.CreateRenderEngine();

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

        public string PersistentPath => Path.GetFullPath("Data");

        public string Name => "Editor";
    }
}
