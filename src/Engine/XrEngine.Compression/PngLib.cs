using System.Runtime.InteropServices;

namespace XrEngine.Compression
{
    public static unsafe class PngLib
    {
        private const string LibName = "etcpack";

        public const int ColorTypeGray = 0;
        public const int ColorTypeRgb = 2;
        public const int ColorTypePalette = 3;
        public const int ColorTypeGrayAlpha = 4;
        public const int ColorTypeRgba = 6;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MemoryBuffer : IDisposable
        {
            public byte* Data;
            public int Size;
            public int Capacity;
            public int Position;

            public readonly ReadOnlySpan<byte> Span =>
                Data == null || Size <= 0
                    ? ReadOnlySpan<byte>.Empty
                    : new ReadOnlySpan<byte>(Data, Size);

            public void Dispose()
            {
                this.FreeMemoryBuffer();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ImageData
        {
            public MemoryBuffer Data;
            public int Width;
            public int Height;
            public int ColorType;
            public int BitDepth;
            public int RowBytes;
        }

        [DllImport(LibName)]
        public static extern int DecodePng(
            ref MemoryBuffer input,
            bool swap16,
            ref ImageData output);

        [DllImport(LibName)]
        public static extern int EncodePng(
            void* pixels,
            int width,
            int height,
            int colorType,
            int bitDepth,
            int compressionLevel,
            bool swap16,
            ref MemoryBuffer output);

        [DllImport(LibName)]
        public static extern void FreeMemoryBuffer(this ref MemoryBuffer buffer);

        public static int EncodeGray16(
            ushort* pixels,
            int width,
            int height,
            int compressionLevel,
            ref MemoryBuffer output)
        {
            return EncodePng(
                pixels,
                width,
                height,
                ColorTypeGray,
                16,
                compressionLevel,
                true,
                ref output);
        }

        public static int EncodeRgba8(
            byte* pixels,
            int width,
            int height,
            int compressionLevel,
            ref MemoryBuffer output)
        {
            return EncodePng(
                pixels,
                width,
                height,
                ColorTypeRgba,
                8,
                compressionLevel,
                false,
                ref output);
        }
    }
}
