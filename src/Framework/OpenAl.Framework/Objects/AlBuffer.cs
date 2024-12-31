using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Soft;
using System.Runtime.InteropServices;

namespace OpenAl.Framework
{
    public enum InternalFormat
    {
        Mono8 = 0x1100,
        Mono16 = 0x1101,
        Mono32F = 0x10010,
        Stereo8 = 0x1102,
        Stereo16 = 0x1103,
        Stereo32F = 0x10011,
        Quad8 = 0x1204,
        Quad16 = 0x1205,
        Quad32F = 0x1206,
        Rear8 = 0x1207,
        Rear16 = 0x1208,
        Rear32F = 0x1209,
        FivePointOne8 = 0x120A,
        FivePointOne16 = 0x120B,
        FivePointOne32F = 0x120C,
        SixPointOne8 = 0x120D,
        SixPointOne16 = 0x120E,
        SixPointOne32F = 0x120F,
        SevenPointOne8 = 0x1210,
        SevenPointOne16 = 0x1211,
        SevenPointOne32F = 0x1212
    }

    public enum Channels
    {
        Mono = 0x1500,
        Stereo = 0x1501,
        Quad = 0x1502,
        Rear = 0x1503,
        FivePointOne = 0x1504,
        SixPointOne = 0x1505,
        SevenPointOne = 0x1506
    }

    public enum SampleType
    {
        Byte = 0x1400,
        UnsignedByte = 0x1401,
        Short = 0x1402,
        UnsignedShort = 0x1403,
        Int = 0x1404,
        UnsignedInt = 0x1405,
        Float = 0x1406,
        Double = 0x1407,
        Byte3 = 0x1408,
        UnsignedByte3 = 0x1409
    }


    public class AlBuffer : AlObject, IDisposable
    {

        const int AL_FORMAT_MONO_FLOAT32 = 0x10010;
        const int AL_FORMAT_STEREO_FLOAT32 = 0x10011;

        static SoftCallbackBuffer? _callback;

        static alBufferSamplesSOFTDelegate? alBufferSamplesSOFT;

        unsafe delegate void alBufferSamplesSOFTDelegate(uint buffer, uint sampleRate, uint internalFormat, uint samples, uint channels, uint type, void* data);

        static readonly Dictionary<uint, AlBuffer> _attached = [];

        public AlBuffer(AL al)
            : this(al, al.GenBuffer())
        {
            alBufferSamplesSOFT ??= Marshal.GetDelegateForFunctionPointer<alBufferSamplesSOFTDelegate>(al.Context.GetProcAddress("alBufferSamplesSOFT"));

            if (_callback == null)
                al.TryGetExtension<SoftCallbackBuffer>(out _callback);
        }

        public AlBuffer(AL al, uint handle) : base(al, handle)
        {
            _attached[handle] = this;
        }

        public unsafe void Samples(uint sampleRate, InternalFormat internalFormat, uint samples, Channels channels, SampleType type, void* data)
        {
            var data2 = new Span<byte>(new byte[samples * 2 * 2]);
            alBufferSamplesSOFT!(_handle, samples, (uint)internalFormat, samples, (uint)channels, (uint)type, &data2);
            _al.CheckError("alBufferSamplesSOFT");
        }

        public unsafe void SetCallback(AudioFormat format, Func<Span<byte>, int> callback)
        {
            _callback!.BufferCallback(_handle, GetBufferFormat(format), format.SampleRate, new PfnBufferCallback((user, samplerData, numBytes) =>
            {
                var span = new Span<byte>(samplerData, numBytes);
                return callback(span);
            }), null);

            _al.CheckError("BufferCallback");
        }

        public unsafe void ClearCallback(AudioFormat format, Func<Span<byte>, int> callback)
        {
            _callback!.BufferCallback(_handle, 0, 0, new PfnBufferCallback(), null);
        }

        public void SetData(AudioData data)
        {
            SetData(data.Buffer!, data.Format!);
        }


        protected BufferFormat GetBufferFormat(AudioFormat format)
        {
            BufferFormat bf;

            switch (format.SampleType)
            {
                case AudioSampleType.Byte:
                    if (format.Channels == 1)
                        bf = BufferFormat.Mono8;
                    else
                        bf = BufferFormat.Stereo8;
                    break;
                case AudioSampleType.Short:
                    if (format.Channels == 1)
                        bf = BufferFormat.Mono16;
                    else
                        bf = BufferFormat.Stereo16;
                    break;
                case AudioSampleType.Float:

                    if (!_al.IsExtensionPresent("AL_EXT_FLOAT32"))
                        throw new NotSupportedException();

                    if (format.Channels == 1)
                        bf = (BufferFormat)AL_FORMAT_MONO_FLOAT32;
                    else
                        bf = (BufferFormat)AL_FORMAT_STEREO_FLOAT32;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return bf;
        }

        public unsafe void SetData(byte[] data, AudioFormat format)
        {
            fixed (byte* pData = data)
                _al.BufferData(_handle, GetBufferFormat(format), pData, data.Length, format.SampleRate);

            _al.CheckError("BufferData");
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
