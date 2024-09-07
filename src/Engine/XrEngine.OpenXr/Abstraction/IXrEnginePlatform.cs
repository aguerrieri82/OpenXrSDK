using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public interface IXrEnginePlatform
    {
        XrApp CreateXrApp(IXrGraphicDriver xrDriver);

        void CreateDrivers(XrEngineAppOptions options, out IRenderEngine renderEngine, out IXrGraphicDriver xrDriver);

        public string PersistentPath { get; }

        public string Name { get; }
    }
}
