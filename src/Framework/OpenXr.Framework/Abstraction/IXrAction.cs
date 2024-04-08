using Silk.NET.OpenXR;
using Action = Silk.NET.OpenXR.Action;

namespace OpenXr.Framework
{
    public interface IXrAction : IDisposable
    {
        ActionSuggestedBinding Initialize();

        void Destroy();

        Action Action { get; }

        string Name { get; }
    }
}
