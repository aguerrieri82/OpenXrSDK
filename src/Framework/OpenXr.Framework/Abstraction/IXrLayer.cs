using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public unsafe interface IXrLayer : IDisposable
    {
        void Initialize(XrApp app, IList<string> extensions);

        bool Render(ref View[] views, XrSwapchainInfo[] swapchains, long predTime);

        bool IsEnabled { get; set; }

        CompositionLayerBaseHeader* Header { get; }
    }
}
