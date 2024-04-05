using System.Runtime.InteropServices;

namespace XrEngine
{
    public static class XrNativeLib
    {
        [DllImport("xrengine-native")]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);
    }
}
