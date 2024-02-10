using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public interface IXrSession
    {
        bool IsStarted { get; }

        ulong SystemId { get; }

        Instance Instance { get; }

        Session Session { get; }

        XR Xr { get; }
    }
}
