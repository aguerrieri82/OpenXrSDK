namespace XrEngine.Video
{
    public struct FrameBuffer
    {
        public IntPtr Pointer;

        public byte[] ByteArray;

        public int Size;

        public int Offset;

        public static FrameBuffer Allocate(int size)
        {
            return new FrameBuffer
            {
                ByteArray = new byte[size],
                Size = size
            };
        }
    }
}
