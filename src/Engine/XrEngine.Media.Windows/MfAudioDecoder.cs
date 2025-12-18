
using System.Runtime.InteropServices;

namespace XrEngine.Media.Windows
{

    public class MfAudioDecoder : IAudioDecoder, IDisposable
    {
        IMFSourceReader? _reader;

        public byte[] DecodeToPCM(string path, out AudioFormat format)
        {
            format = new AudioFormat()
            {
                Channels = 2,
                SampleRate = 48000,
                SampleType = AudioSampleType.Short
            };

            using var stream = new MemoryStream();
            Open(path);
            while (true)
            {
                var data = Read();
                if (data == null)
                    return stream.ToArray();

                stream.Write(data);
            }
        }

        public void Open(string path)
        {
            MF.MFStartup(MF.MF_VERSION);

            MF.MFCreateAttributes(out var attr, 1);

            MF.MFCreateSourceReaderFromURL(path, attr, out _reader);

            MF.MFCreateMediaType(out var pcm);

            _reader.GetNativeMediaType(0, 0, out IMFMediaType native);

            pcm.SetGUID(ref MFAttributesGuid.MajorType, ref MFMajorTypes.Audio);
            pcm.SetGUID(ref MFAttributesGuid.Subtype, ref MFSubtypes.PCM);
            pcm.SetUINT32(ref MFAttributesGuid.AudioNumChannels, 2);
            pcm.SetUINT32(ref MFAttributesGuid.AudioSamplesPerSecond, 48000);
            pcm.SetUINT32(ref MFAttributesGuid.AudioBitsPerSample, 16);
            pcm.SetUINT32(ref MFAttributesGuid.AudioBlockAlignment, 2 * (16 / 8));


            try
            {
                _reader.SetCurrentMediaType(0, IntPtr.Zero, pcm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            _reader.SetStreamSelection(0, true);
        }

        public void Dispose()
        {
            _reader = null!;
            MF.MFShutdown();
        }

        public byte[]? Read()
        {
            if (_reader == null)
                return null;

            _reader.ReadSample(
                0,
                0,
                out _,
                out _,
                out _,
                out var sample);

            if (sample == null)
                return null;

            sample.ConvertToContiguousBuffer(out var buffer);

            buffer.GetCurrentLength(out int len);

            buffer.Lock(out var ptr, out _, out _);

            var data = new byte[len];
            Marshal.Copy(ptr, data, 0, len);

            buffer.Unlock();

            return data;
        }
    }
}
