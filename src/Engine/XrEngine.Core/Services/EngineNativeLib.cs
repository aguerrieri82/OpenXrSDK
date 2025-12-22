using System.Runtime.InteropServices;

namespace XrEngine
{
    public static class EngineNativeLib
    {
        [DllImport("xrengine-native", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);

        [DllImport("xrengine-native")]
        public static extern void ImageCopyChannel(nint src, nint dst, uint width, uint height, uint srcRowSize, uint dstRowSize, uint srcOfs, uint dstOfs, uint cSize);

        [DllImport("xrengine-native", EntryPoint = "CopyMemory2")]
        public static extern void CopyMemory(nint src, nint dst, uint size);

        [DllImport("xrengine-native")]
        public static extern int CompareMemory(nint src, nint dst, uint size);

        [DllImport("xrengine-native")]
        public static extern ulong Now();


        [DllImport("xrengine-native")]
        public static extern void SleepUntil(ulong time);

        [DllImport("xrengine-native")]
        public static extern void SleepFor(ulong time);


        [DllImport("xrengine-native")]
        public static unsafe extern void ImagePack(uint srcWidth, uint srcHeight, byte* srcData, uint dstWidth, uint dstHeight, byte* dstData, uint pixelSize);


        [DllImport("xrengine-native")]
        public static unsafe extern void RgbToBgr(uint width, uint height, byte* srcData, byte* dstData, uint pixelSizeByte);


        [DllImport("xrengine-native")]
        public static unsafe extern void ImageResizeBilinearU8(
                uint srcW, uint srcH, byte* src,
                uint dstW, uint dstH, byte* dst,
                uint channels);
    }
}
