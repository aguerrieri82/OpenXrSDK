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


        public static unsafe void ImageCopyChannel(Memory<byte> src, Memory<byte> dst, uint width, uint height, uint rowSize, uint srcOfs, uint dstOfs, uint cSize)
        {
            fixed (byte* srcPtr = &src.Span[0])
            fixed (byte* dstPtr = &dst.Span[0])
                ImageCopyChannel((nint)srcPtr, (nint)dstPtr, width, height, rowSize, srcOfs, dstOfs, cSize);
        }
    }
}
