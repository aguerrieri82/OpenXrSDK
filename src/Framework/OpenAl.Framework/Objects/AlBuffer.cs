using Silk.NET.OpenAL;

namespace OpenAl.Framework
{
    public class AlBuffer : AlObject, IDisposable
    {
        static Dictionary<uint, AlBuffer> _attached = [];

        public AlBuffer(AL al)
            : this(al, al.GenBuffer())
        {
        }

        public AlBuffer(AL al, uint handle) : base(al, handle)
        {
            _attached[handle] = this;   
        }

        public void SetData(AudioData data)
        {
            SetData(data.Buffer!, data.Format!);
        }

        public unsafe void SetData(byte[] data, AudioFormat format)
        {
            BufferFormat bf;

            switch (format.BitsPerSample)
            {
                case 8:
                    if (format.Channels == 1)
                        bf = BufferFormat.Mono8;
                    else
                        bf = BufferFormat.Stereo8;
                    break;
                case 16:
                    if (format.Channels == 1)
                        bf = BufferFormat.Mono16;
                    else
                        bf = BufferFormat.Stereo16;
                    break;
                default:
                    throw new NotSupportedException();
            }



            fixed (byte* pData = data)
                _al.BufferData(_handle, bf, pData, data.Length, format.SampleRate);
        }

        public void Dispose()
        {
            if (_handle != 0)
            {
                _attached.Remove(_handle);
                _al.DeleteBuffer(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }


        public static AlBuffer Attach(AL al, uint handle)
        {
            if (!_attached.TryGetValue(handle, out var result))
                result = new AlBuffer(al, handle);
            return result;  
        }
    }
}
