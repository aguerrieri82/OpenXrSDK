using Microsoft.Extensions.Logging;
using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public interface IXrEnginePlatform : IPlatform
    {
        XrApp CreateXrApp(IXrGraphicDriver xrDriver);

        void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver);
    }

    public static class XrEnginePlatformExtensions
    {
        public static IAssetStore AssetStore(this IXrEnginePlatform self) => Context.Require<IAssetStore>();

        public static ILogger Logger(this IXrEnginePlatform self) => Context.Require<ILogger>();

        public static IProgressLogger ProgressLogger(this IXrEnginePlatform self) => Context.Require<IProgressLogger>();
    }

}
