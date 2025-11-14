using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Media.Android
{
    public enum AudioSampleFormat
    {
        Pcm8,
        Pcm16,
        Float
    }

    public class AudioFormat
    {
        public int SampleRate { get; set; }

        public int ChannelCount { get; set; }

        public global::Android.Media.Encoding SampleFormat { get; set; }
    }
}
