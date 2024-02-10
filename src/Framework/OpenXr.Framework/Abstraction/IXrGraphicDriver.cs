using Silk.NET.OpenXR;

namespace OpenXr.Framework
{

    public unsafe interface IXrGraphicDriver : IXrPlugin
    {
        GraphicsBinding CreateBinding();

        long SelectSwapChainFormat(IList<long> availFormats);

        XrDynamicType SwapChainImageType { get; }
    }
}
