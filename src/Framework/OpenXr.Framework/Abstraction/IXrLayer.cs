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

        void Destroy();

        void OnBeginFrame(Space space, long displayTime);

        bool Update(ref View[] views, long displayTime);

        void OnEndFrame();

        int Priority { get; set; }

        bool IsEnabled { get; set; }

        CompositionLayerBaseHeader* Header { get; }

        XrLayerFlags Flags { get; }
    }
}
