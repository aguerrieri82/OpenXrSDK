using Microsoft.Extensions.Logging;
using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public interface IXrEnginePlatform
    {
        XrApp CreateXrApp(IXrGraphicDriver xrDriver);

        void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver);

        public IAssetManager AssetManager { get; }

        public ILogger Logger { get; }

        public string PersistentPath { get; }
    }
}
