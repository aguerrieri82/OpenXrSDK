
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

#pragma warning disable CA1416 

namespace XrEngine.Media.Windows
{
    [AIGenerated]
    public class MfAudioEncoder : IAudioEncoder
    {
        public byte[] EncodeFromPcm(string path, in AudioFormat format, AudioCompressionOptions options)
        {
            using var stream = File.OpenRead(path);
            return EncodeFromPcm(stream, in format, options);
        }

        public byte[] EncodeFromPcm(Stream stream, in AudioFormat format, AudioCompressionOptions options)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(options);

            if (options.Format != AudioCompressionFormat.Mp3)
                throw new NotSupportedException($"Unsupported audio format: {options.Format}");

            if (format.SampleType != AudioSampleType.Short)
                throw new NotSupportedException("Only 16-bit PCM is supported");

            if (format.Channels < 1 || format.Channels > 2 || format.SampleRate <= 0)
                throw new ArgumentException("Invalid audio format");

            if (options.BitRate < 0 || options.BitRate % 8 != 0)
                throw new ArgumentOutOfRangeException(nameof(options.BitRate));

            int blockAlign = format.Channels * 2;

            Marshal.ThrowExceptionForHR(MF.MFStartup(MF.MF_VERSION));

            IStream? outputStream = null;
            IntPtr byteStream = IntPtr.Zero;
            IMFSinkWriter? writer = null;
            IMFMediaType? input = null;
            IMFMediaType? output = null;

            try
            {
                Marshal.ThrowExceptionForHR(MF.CreateStreamOnHGlobal(IntPtr.Zero, true, out outputStream));
                Marshal.ThrowExceptionForHR(MF.MFCreateMFByteStreamOnStream(outputStream, out byteStream));
                Marshal.ThrowExceptionForHR(MF.MFCreateSinkWriterFromURL("output.mp3", byteStream, IntPtr.Zero, out writer));

                Marshal.ThrowExceptionForHR(MF.MFCreateMediaType(out output));

                output.SetGUID(ref MFAttributesGuid.MajorType, ref MFMajorTypes.Audio);
                output.SetGUID(ref MFAttributesGuid.Subtype, ref MFSubtypes.MP3);
                output.SetUINT32(ref MFAttributesGuid.AudioNumChannels, (uint)format.Channels);
                output.SetUINT32(ref MFAttributesGuid.AudioSamplesPerSecond, (uint)format.SampleRate);

                if (options.BitRate > 0)
                    output.SetUINT32(ref MFAttributesGuid.AudioAvgBytesPerSecond, (uint)(options.BitRate / 8));

                Marshal.ThrowExceptionForHR(writer.AddStream(output, out var streamIndex));

                Marshal.ThrowExceptionForHR(MF.MFCreateMediaType(out input));

                input.SetGUID(ref MFAttributesGuid.MajorType, ref MFMajorTypes.Audio);
                input.SetGUID(ref MFAttributesGuid.Subtype, ref MFSubtypes.PCM);
                input.SetUINT32(ref MFAttributesGuid.AudioNumChannels, (uint)format.Channels);
                input.SetUINT32(ref MFAttributesGuid.AudioSamplesPerSecond, (uint)format.SampleRate);
                input.SetUINT32(ref MFAttributesGuid.AudioBitsPerSample, 16);
                input.SetUINT32(ref MFAttributesGuid.AudioBlockAlignment, (uint)blockAlign);
                input.SetUINT32(ref MFAttributesGuid.AudioAvgBytesPerSecond, (uint)(format.SampleRate * blockAlign));

                Marshal.ThrowExceptionForHR(writer.SetInputMediaType(streamIndex, input, IntPtr.Zero));
                Marshal.ThrowExceptionForHR(writer.BeginWriting());

                var data = new byte[format.SampleRate * blockAlign];
                long frames = 0;

                while (true)
                {
                    int length = 0;

                    while (length < data.Length)
                    {
                        int read = stream.Read(data, length, data.Length - length);
                        if (read == 0)
                            break;

                        length += read;
                    }

                    if (length == 0)
                        break;

                    if (length % blockAlign != 0)
                        throw new InvalidDataException("Invalid PCM frame alignment");

                    Marshal.ThrowExceptionForHR(MF.MFCreateSample(out var sample));

                    try
                    {
                        Marshal.ThrowExceptionForHR(MF.MFCreateMemoryBuffer(length, out var buffer));

                        try
                        {
                            Marshal.ThrowExceptionForHR(buffer.Lock(out var ptr, out _, out _));

                            try
                            {
                                Marshal.Copy(data, 0, ptr, length);
                            }
                            finally
                            {
                                Marshal.ThrowExceptionForHR(buffer.Unlock());
                            }

                            Marshal.ThrowExceptionForHR(buffer.SetCurrentLength(length));
                            Marshal.ThrowExceptionForHR(sample.AddBuffer(buffer));

                            long start = frames * 10000000L / format.SampleRate;
                            frames += length / blockAlign;
                            long end = frames * 10000000L / format.SampleRate;

                            Marshal.ThrowExceptionForHR(sample.SetSampleTime(start));
                            Marshal.ThrowExceptionForHR(sample.SetSampleDuration(end - start));
                            Marshal.ThrowExceptionForHR(writer.WriteSample(streamIndex, sample));
                        }
                        finally
                        {
                            if (buffer is IDisposable disposable)
                                disposable.Dispose();
                        }
                    }
                    finally
                    {
                        if (sample is IDisposable disposable)
                            disposable.Dispose();
                    }
                }

                Marshal.ThrowExceptionForHR(writer.Finalize_());

                outputStream.Stat(out var stat, 1);

                var result = new byte[checked((int)stat.cbSize)];

                outputStream.Seek(0, 0, IntPtr.Zero);

                var readPtr = Marshal.AllocHGlobal(sizeof(int));

                try
                {
                    outputStream.Read(result, result.Length, readPtr);

                    if (Marshal.ReadInt32(readPtr) != result.Length)
                        throw new EndOfStreamException();
                }
                finally
                {
                    Marshal.FreeHGlobal(readPtr);
                }

                return result;
            }
            finally
            {
                if (input is IDisposable inputDisposable)
                    inputDisposable.Dispose();

                if (output is IDisposable outputDisposable)
                    outputDisposable.Dispose();

                if (writer is IDisposable writerDisposable)
                    writerDisposable.Dispose();

                if (byteStream != IntPtr.Zero)
                    Marshal.Release(byteStream);

                if (outputStream != null)
                    Marshal.ReleaseComObject(outputStream);

                MF.MFShutdown();
            }
        }
    }
}
