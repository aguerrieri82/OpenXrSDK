using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine
{

    public static class EngineNativeLib
    {
        public enum BcFormat
        {
            Bc1 = 1,
            Bc2 = 2,
            Bc3 = 3,
            Bc4 = 4,
            Bc5 = 5,
            Bc6H = 6,
            Bc7 = 7
        }

        public enum BasisTextureFormat
        {
            Etc1Rgb = 0,
            Etc2Rgba = 1,
            Bc1Rgb = 2,
            Bc3Rgba = 3,
            Bc4R = 4,
            Bc5Rg = 5,
            Bc7Rgba = 6,

            Pvrtc1_4Rgb = 8,
            Pvrtc1_4Rgba = 9,

            AstcLdr4x4Rgba = 10,

            AtcRgb = 11,
            AtcRgba = 12,

            Rgba32 = 13,
            Rgb565 = 14,
            Bgr565 = 15,
            Rgba4444 = 16,

            Fxt1Rgb = 17,
            Pvrtc2_4Rgb = 18,
            Pvrtc2_4Rgba = 19,

            Etc2EacR11 = 20,
            Etc2EacRg11 = 21,

            Bc6H = 22,
            AstcHdr4x4Rgba = 23,

            RgbHalf = 24,
            RgbaHalf = 25,
            Rgb9E5 = 26,

            AstcHdr6x6Rgba = 27,

            AstcLdr5x4Rgba = 28,
            AstcLdr5x5Rgba = 29,
            AstcLdr6x5Rgba = 30,
            AstcLdr6x6Rgba = 31,
            AstcLdr8x5Rgba = 32,
            AstcLdr8x6Rgba = 33,
            AstcLdr10x5Rgba = 34,
            AstcLdr10x6Rgba = 35,
            AstcLdr8x8Rgba = 36,
            AstcLdr10x8Rgba = 37,
            AstcLdr10x10Rgba = 38,
            AstcLdr12x10Rgba = 39,
            AstcLdr12x12Rgba = 40
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct BasisImage
        {
            public void* Data;
            public uint Size;
            public uint Width;
            public uint Height;
            public uint Level;
            public uint Layer;
            public uint Face;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct BasisTexture
        {
            public void* Memory;
            public BasisImage* Images;
            public uint ImageCount;
            public uint Width;
            public uint Height;
            public uint Levels;
            public uint Layers;
            public uint Faces;
            public uint IsSrgb;
            public uint HasAlpha;
        }

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

        [DllImport(LibName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern unsafe bool ImageDecodeBC(byte* src, int width, int height, BcFormat format, byte* dst);


        [DllImport(LibName)]
        public static extern unsafe bool BasisTranscodeKtx2(void* data, uint size, BasisTextureFormat format, out BasisTexture result);


        [DllImport(LibName)]
        public static extern void BasisFreeTexture(ref BasisTexture texture);


        [DllImport(LibName)]
        public static extern unsafe void CompressVertices(void* src, void* dst, int count, VertexComponent activeComponents, Bounds3 bounds);

        [DllImport(LibName)]
        public static extern unsafe void CompressIndices16(void* src, void* dst, int count);

        [DllImport(LibName)]
        public static extern unsafe void CompressIndices8(void* src, void* dst, int count);

    }
}
