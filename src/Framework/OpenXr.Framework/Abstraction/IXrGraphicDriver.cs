using Silk.NET.OpenXR;

namespace OpenXr.Framework
{

    public interface IXrGraphicDriver : IXrPlugin
    {
        GraphicsBinding CreateBinding();

        XrDynamicType SwapChainImageType { get; }
    }
}
