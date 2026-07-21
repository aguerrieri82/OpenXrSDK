namespace XrEngine.OpenGL
{
    public class GlBufferMap<T> : IDisposable where T: class, IDisposable
    {
        public GlBufferMap(int maxBuffers)
        {
            Buffers = new T?[maxBuffers];
        }

        public void Dispose()
        {
            for (var i = 0; i < Buffers.Length; i++)
            {
                Buffers[i]?.Dispose();
                Buffers[i] = null;
            }

            GC.SuppressFinalize(this);
        }

        public readonly T?[] Buffers;
    }

}
