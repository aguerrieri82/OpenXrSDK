using Silk.NET.OpenXR;

namespace OpenXr.Framework
{
    public enum XrGraphicDriverFlags
    {
        None = 0,
        FlipAndroidSurfaceY = 0x1,
        Vulkan = 0x2,
    }

    public interface IXrGraphicDriver : IXrPlugin
    {
        GraphicsBinding CreateBinding();

        XrDynamicType SwapChainImageType { get; }

        XrGraphicDriverFlags Flags => XrGraphicDriverFlags.None;
    }
}
