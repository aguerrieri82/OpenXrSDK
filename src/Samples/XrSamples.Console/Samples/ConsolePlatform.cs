using Microsoft.Extensions.Logging.Abstractions;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.Framework.OpenGL;
using Silk.NET.OpenGL;
using XrEngine;
using XrEngine.OpenGL;
using XrEngine.OpenXr;

namespace XrSamples
{
    public class ConsolePlatform : IXrEnginePlatform
    {
        ViewManager _viewManager;

        public ConsolePlatform()
        {
            _viewManager = new ViewManager();
            _viewManager.Initialize();

            Context.Implement<IProgressLogger>(new NullProgressLogger());
            Context.Implement<IAssetStore>(new LocalAssetStore("Assets"));
        }

        public void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver)
        {
            renderEngine = new OpenGLRender(_viewManager.View.CreateOpenGL());
            xrDriver = new XrOpenGLGraphicDriver(_viewManager.View);
        }

        public XrApp CreateXrApp(IXrGraphicDriver xrDriver)
        {
            return new XrApp(NullLogger.Instance,
                     xrDriver,
                     new OculusXrPlugin());
        }

        public string PersistentPath => throw new NotImplementedException();

        public string Name => "Console";
    }
}
