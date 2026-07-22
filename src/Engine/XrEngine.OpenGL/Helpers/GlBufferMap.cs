namespace XrEngine.OpenGL
{
    public class GlBufferMap<T> : IDisposable where T: class, IDisposable
    {
        WeakReference<object>? _owner;

        public GlBufferMap(int maxBuffers, object? owner = null)
        {
            Buffers = new T?[maxBuffers];
            if (owner != null)
                _owner = new WeakReference<object>(owner);
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

        public object? Owner => 
            _owner != null && 
            _owner.TryGetTarget(out var result) ? result : null;

        public readonly T?[] Buffers;
    }

}
