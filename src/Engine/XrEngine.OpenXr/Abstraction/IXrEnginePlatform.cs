using Microsoft.Extensions.Logging;
using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public interface IXrEnginePlatform
    {
        XrApp CreateXrApp(IXrGraphicDriver xrDriver);

        void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver);

        public string PersistentPath { get; }

        public string CachePath { get; }

        public string Name { get; }
    }

    public static class XrEnginePlatformExtensions
    {
        public static IAssetStore AssetStore(this IXrEnginePlatform self) => Context.Require<IAssetStore>();

        public static ILogger Logger(this IXrEnginePlatform self) => Context.Require<ILogger>();

        public static IProgressLogger ProgressLogger(this IXrEnginePlatform self) => Context.Require<IProgressLogger>();
    }

}
