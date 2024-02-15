using Silk.NET.OpenXR;
using Action = Silk.NET.OpenXR.Action;

namespace OpenXr.Framework
{
    public interface IXrAction
    {
        ActionSuggestedBinding Initialize();

        Action Action { get; }

        string Name { get; }
    }
}
