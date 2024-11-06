using System.Runtime.InteropServices;

namespace XrEngine
{
    public static class EngineNativeLib
    {
        [DllImport("xrengine-native")]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);

        [DllImport("xrengine-native")]
        public static extern void ImageCopyChannel(nint src, nint dst, uint width, uint height, uint rowSize, uint srcOfs, uint dstOfs, uint cSize);

        [DllImport("xrengine-native")]
        public static extern void CopyMemory(nint src, nint dst, uint size);
    }
}
