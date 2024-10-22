namespace OpenAl.Framework
{
    public static class StreamExtensions
    {
        public unsafe static T ReadStruct<T>(this Stream stream) where T : unmanaged
        {
            var buffer = stackalloc T[1];

            var span = new Span<byte>((byte*)buffer, sizeof(T));

            stream.ReadExactly(span);

            return *buffer;
        }

    }
}
