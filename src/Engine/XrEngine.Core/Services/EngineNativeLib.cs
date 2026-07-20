using System.Runtime.InteropServices;

namespace XrEngine
{

    public static class EngineNativeLib
    {

        const string LibName = "xrengine-native";

        [DllImport(LibName)]
        public static extern void ImageFlipY(nint src, nint dst, uint width, uint height, uint rowSize);

        [DllImport(LibName)]
        public static extern void ImageCopyChannel(nint src, nint dst, uint width, uint height, uint srcRowSize, uint dstRowSize, uint srcOfs, uint dstOfs, uint cSize);

        [DllImport(LibName, EntryPoint = "CopyMemory2")]
        public static extern void CopyMemory(nint src, nint dst, uint size);

        [DllImport(LibName)]
        public static extern int CompareMemory(nint src, nint dst, uint size);

        [DllImport(LibName)]
        public static extern ulong Now();

        [DllImport(LibName)]
        public static extern void SleepUntil(ulong time);

        [DllImport(LibName)]
        public static extern void SleepFor(ulong time);

        [DllImport(LibName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public unsafe static extern bool ConvertRgba16ToRgba32F(
            ushort* src,
            float* dst,
            uint width,
            uint height,
            uint srcRowBytes);

        [DllImport(LibName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public unsafe static extern bool ConvertRgb32FToRgba16F(
            float* src,
            Half* dst,
            uint srcFloatCount);

        [DllImport(LibName)]
        public static unsafe extern void ImagePack(
            uint srcWidth, uint srcHeight, byte* srcData,
            uint dstWidth, uint dstHeight, byte* dstData,
            uint pixelSize);

        [DllImport(LibName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static unsafe extern bool ImagePackToRgba8(
            byte* src,
            byte* dst,
            uint width,
            uint height,
            uint srcChannels,
            uint srcRowAlignment);

        [DllImport(LibName)]
        public static unsafe extern void ConvertRgbToBgr(uint width, uint height, byte* srcData, byte* dstData, uint pixelSizeByte);

        [DllImport(LibName)]
        public static unsafe extern void ImageResizeBilinearU8(
                uint srcW, uint srcH, byte* src,
                uint dstW, uint dstH, byte* dst,
                uint channels);

        [DllImport(LibName)]
        public static extern int RdcTriggerCapture();

        [DllImport(LibName)]
        public static extern int RdcStartFrameCapture();

        [DllImport(LibName)]
        public static extern int RdcEndFrameCapture(bool launchReplay);

        [DllImport(LibName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool RdcIsAttached();

    }
}
