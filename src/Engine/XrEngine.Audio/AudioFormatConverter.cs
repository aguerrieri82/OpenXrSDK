using OpenAl.Framework;
using XrEngine.Media;

namespace XrEngine.Audio
{
    public static class AudioFormatConverter
    {
        public static AudioFormat ToAudioFormat(this AlAudioFormat alFormat)
        {
            return new AudioFormat
            {
                Channels = alFormat.Channels,
                SampleRate = alFormat.SampleRate,
                SampleType = (AudioSampleType)alFormat.SampleType
            };
        }

        public static AlAudioFormat ToAlAudioFormat(this AudioFormat alFormat)
        {
            return new AlAudioFormat
            {
                Channels = alFormat.Channels,
                SampleRate = alFormat.SampleRate,
                SampleType = (AlAudioSampleType)alFormat.SampleType
            };
        }
    }
}
