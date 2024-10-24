namespace XrEngine.OpenGL
{
    public class GlBufferMap : IDisposable
    {
        public GlBufferMap(int maxBuffers)
        {
            Buffers = new IGlBuffer?[maxBuffers];
        }

        public void Dispose()
        {
            for (var i = 0; i < Buffers.Length; i++)
            {
                if (Buffers[i] != null)
                {
                    Buffers[i]!.Dispose();
                    Buffers[i] = null;
                }
            }
        }

        public readonly IGlBuffer?[] Buffers;
    }

}
