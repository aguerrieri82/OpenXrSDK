using Silk.NET.OpenAL;

namespace OpenAl.Framework
{
    public class AudioBuffer : AlObject, IDisposable
    {
        public AudioBuffer(AL al)
            : this(al, al.GenBuffer())
        {
        }

        public AudioBuffer(AL al, uint handle) : base(al, handle)
        {

        }

        public void Dispose()
        {
            if (_handle != 0)
            {
                _al.DeleteBuffer(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }
    }
}
