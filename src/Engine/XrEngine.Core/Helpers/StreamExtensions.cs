namespace XrEngine
{
    public static class StreamExtensions
    {
        public unsafe static T ReadStruct<T>(this Stream stream) where T : unmanaged
        {
            T* buffer = stackalloc T[1];

            Span<byte> span = new Span<byte>((byte*)buffer, sizeof(T));

            stream.ReadExactly(span);

            return *buffer;
        }

        public unsafe static void WriteStruct<T>(this Stream stream, T value) where T : unmanaged
        {
            Span<byte> span = new Span<byte>((byte*)&value, sizeof(T));
            stream.Write(span);
        }

        public unsafe static MemoryStream ToMemory(this Stream stream)
        {
            if (stream is MemoryStream memory)
                return memory;
            MemoryStream result = new MemoryStream();
            stream.CopyTo(result);
            result.Position = 0;
            stream.Dispose();
            return result;
        }

        public unsafe static Stream EnsureSeek(this Stream stream)
        {
            if (!stream.CanSeek)
                return stream.ToMemory();
            return stream;
        }
    }
}
