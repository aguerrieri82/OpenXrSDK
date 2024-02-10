using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public unsafe interface IXrLayer : IDisposable
    {
        bool Render(ref View[] views, XrSwapchainInfo[] swapchains, long predTime);

        bool IsEnabled { get; set; }

        CompositionLayerBaseHeader* Header { get; }
    }
}
