namespace XrEngine
{
    public static class StreamExtensions
    {
        public unsafe static T ReadStruct<T>(this Stream stream) where T : unmanaged
        {
            var buffer = stackalloc T[1];

            var span = new Span<byte>((byte*)buffer, sizeof(T));

            stream.Read(span);

            return *buffer;
        }

        public unsafe static void WriteStruct<T>(this Stream stream, T value) where T : unmanaged
        {
            var span = new Span<byte>((byte*)&value, sizeof(T));
            stream.Write(span);
        }

        public unsafe static MemoryStream ToMemory(this Stream stream)
        {
            if (stream is MemoryStream memory)
                return memory;
            var result = new MemoryStream();
            stream.CopyTo(result);
            result.Position = 0;
            stream.Dispose();
            return result;
        }
    }
}
