using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    [Flags]
    public enum XrLayerFlags
    {
        None = 0,
        EmptySpace = 1
    }

    public unsafe interface IXrLayer : IDisposable
    {
        void Initialize(XrApp app, IList<string> extensions);

        void Create();

        bool Render(ref View[] views, XrSwapchainInfo[] swapchains, long predTime);

        bool IsEnabled { get; set; }

        CompositionLayerBaseHeader* Header { get; }

        XrLayerFlags Flags { get; } 
    }
}
