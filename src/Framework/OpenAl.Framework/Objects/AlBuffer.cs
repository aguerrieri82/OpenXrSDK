using Silk.NET.OpenAL;

namespace OpenAl.Framework
{
    public class AlBuffer : AlObject, IDisposable
    {
        public AlBuffer(AL al)
            : this(al, al.GenBuffer())
        {
        }

        public AlBuffer(AL al, uint handle) : base(al, handle)
        {

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
                _al.DeleteBuffer(_handle);
                _handle = 0;
            }

            GC.SuppressFinalize(this);
        }
    }
}
