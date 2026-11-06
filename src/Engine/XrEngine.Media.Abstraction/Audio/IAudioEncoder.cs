using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Media
{
    public enum AudioCompressionFormat
    {
        Mp3
    }

    public class AudioCompressionOptions
    {
        public AudioCompressionFormat Format { get; set; }

        public int BitRate { get; set; }
    }


    public interface IAudioEncoder
    {
        byte[] EncodeFromPcm(string path, in AudioFormat format, AudioCompressionOptions options);

        byte[] EncodeFromPcm(Stream stream, in AudioFormat format, AudioCompressionOptions options);
    }
}
