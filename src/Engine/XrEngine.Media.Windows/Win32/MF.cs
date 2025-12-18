using System.Runtime.InteropServices;

namespace XrEngine.Media.Windows
{
    internal static class MF
    {
        public const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = unchecked((int)0xFFFFFFFD);

        public const int MF_SOURCE_READER_ALL_STREAMS = unchecked((int)0xFFFFFFFE);



        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFStartup(uint version, uint flags = 0);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFShutdown();

        public const uint MF_VERSION = 0x20070; // Media Foundation version


        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateAttributes(out IMFAttributes attributes, int initialSize);

        [DllImport("mfreadwrite.dll", ExactSpelling = true)]
        public static extern int MFCreateSourceReaderFromURL(
            [MarshalAs(UnmanagedType.LPWStr)] string url,
            IMFAttributes attributes,
            out IMFSourceReader reader);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMediaType(out IMFMediaType type);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMemoryBuffer(
    int cbMaxLength,
    out IMFMediaBuffer ppBuffer);

        public static void MFCreateSourceReaderFromURL(string path, IMFAttributes attr, out object reader)
        {
            throw new NotImplementedException();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropVariant
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;

        public IntPtr ptr;
        public int int32;
        public long int64;
    }
}
