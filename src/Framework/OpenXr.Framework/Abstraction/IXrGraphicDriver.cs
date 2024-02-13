using Silk.NET.OpenXR;

namespace OpenXr.Framework
{

    public unsafe interface IXrGraphicDriver : IXrPlugin
    {
        GraphicsBinding CreateBinding();

        XrDynamicType SwapChainImageType { get; }
    }
}
