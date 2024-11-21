using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public class AudioRange
    {
        int _startSample;
        int _endSample;
        int _channel;
        readonly  AudioFormat _format;


        public AudioRange(AudioFormat format, int channel = 0)
        {
            _format = format;
            _channel = 0;
        }


        public void ShiftTime(float delta)
        {
            StartTime += delta;
            EndTime += delta;
        }

        public void ShiftSamples(int delta)
        {
            _startSample += delta;
            _endSample += delta;    
        }

        public float StartTime
        {
            get => _startSample / (float)_format.SampleRate;
            set => _startSample = (int)(value * _format.SampleRate);
        }

        public int StartSample
        {
            get => _startSample;
            set => _startSample = value;
        }

        public int StartOffset
        {
            get => (_startSample * (_format.BitsPerSample / 8) * _format.Channels) + 
                   (_format.BitsPerSample / 8 * _channel);
        }

        public float EndTime
        {
            get => _endSample / (float)_format.SampleRate;
            set => _endSample = (int)(value * _format.SampleRate);
        }

        public int EndSample
        {
            get => _endSample;
            set => _endSample = value;
        }

        public int EndOffset
        {
            get => (_endSample * (_format.BitsPerSample / 8) * _format.Channels) +
                    (_format.BitsPerSample / 8 * _channel);
        }

        public float Duration
        {
            get => (EndTime - StartTime);
            set => EndTime = StartTime + value;
        }

        public int Length
        {
            get => (_endSample - _startSample) + 1;
            set => _endSample = _startSample + value - 1;
        }

        public int Size
        {
            get => Length * (_format.BitsPerSample / 8) * _format.Channels;
            set
            {
                Length = value / (_format.BitsPerSample / 8 * _format.Channels);
            }
        }
    }
}
