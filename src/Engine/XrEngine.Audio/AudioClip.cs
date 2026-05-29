using OpenAl.Framework;
using System.Diagnostics;
using System.Numerics;
using XrEngine.Media;

namespace XrEngine.Audio
{
    public class AudioClip
    {
        readonly byte[] _buffer;
        readonly AudioFormat _format;

        public AudioClip(Span<byte> buffer, AudioFormat format)
        {
            Range = new AudioRange(format);
            Range.Size = buffer.Length;
            _buffer = buffer.ToArray();
            _format = format;
        }

        public AlAudioData ToAlAudio()
        {
            return new AlAudioData(_format.ToAlAudioFormat(), new Span<byte>(_buffer, Range.StartOffset, Range.Size));
        }

        public unsafe AudioClip ToMono()
        {
            if (_format.Channels == 1)
                return this;

            Debug.Assert(_format.SampleType == AudioSampleType.Short);

            var frameCount = _buffer.Length >> 2;
            var mono = new byte[frameCount << 1];

            fixed (byte* srcBytes = _buffer)
            fixed (byte* dstBytes = mono)
            {
                short* src = (short*)srcBytes;
                short* dst = (short*)dstBytes;

                for (int i = 0; i < frameCount; i++)
                {
                    var o = i << 1;

                    var left = src[o];
                    var right = src[o + 1];

                    dst[i] = (short)((left + right) >> 1);
                }
            }

            var result = new AudioClip(mono, new AudioFormat
            {
                Channels = 1,
                SampleRate = _format.SampleRate,
                SampleType = _format.SampleType,
            });
            
            result.Range.StartTime = Range.StartTime;
            result.Range.EndTime = Range.EndTime;
       
            return result;
        }

        public AudioClip SubClipTime(float startTime, float endTime)
        {
            var result = new AudioClip(_buffer, _format);
            result.Range.StartTime = startTime;
            result.Range.EndTime = endTime;
            return result;
        }

        public AudioClip SubClipDuration(float startTime, float duration)
        {
            var result = new AudioClip(_buffer, _format);
            result.Range.StartTime = startTime;
            result.Range.Duration = duration;
            return result;
        }

        public void CopyTo(byte[] buffer)
        {
            System.Buffer.BlockCopy(_buffer, Range.StartOffset, buffer, 0, buffer.Length);
        }

        public unsafe void CopyTo(float[] outData)
        {
            var dPos = _format.Channels;

            fixed (byte* pBuf = _buffer)
            fixed (float* pFloat = outData)
            {
                if (_format.BitsPerSample == 8)
                {
                    var curPos = (sbyte*)(pBuf + Range.StartOffset);
                    for (var i = 0; i < outData.Length; i++)
                    {
                        pFloat[i] = *curPos / (float)sbyte.MaxValue;
                        curPos += dPos;
                    }
                }
                if (_format.BitsPerSample == 16)
                {

                    var curPos = (short*)(pBuf + Range.StartOffset);
                    for (var i = 0; i < outData.Length; i++)
                    {
                        pFloat[i] = *curPos / (float)short.MaxValue;
                        curPos += dPos;
                    }
                }
            }
        }

        public void CopyTo(Vector2[] outData, float baseTime = 0)
        {
            var floats = new float[outData.Length];
            CopyTo(floats);
            for (var i = 0; i < outData.Length; i++)
            {
                outData[i].Y = floats[i];
                outData[i].X = baseTime + ((Range.StartSample + i) / (float)_format.SampleRate);
            }
        }

        public unsafe void CopyFrom(float[] data)
        {
            var dPos = _format.Channels;

            fixed (byte* pBuf = _buffer)
            fixed (float* pFloat = data)
            {
                if (_format.BitsPerSample == 8)
                {
                    var curPos = (sbyte*)(pBuf + Range.StartOffset);
                    for (var i = 0; i < data.Length; i++)
                    {
                        *curPos = (sbyte)(pFloat[i] * sbyte.MaxValue);
                        curPos += dPos;
                    }
                }
                if (_format.BitsPerSample == 16)
                {
                    var curPos = (short*)(pBuf + Range.StartOffset);
                    for (var i = 0; i < data.Length; i++)
                    {
                        *curPos = (short)(pFloat[i] * short.MaxValue);
                        curPos += dPos;
                    }
                }
            }
        }

        public float[] ToFloat()
        {
            var result = new float[Range.Length];
            CopyTo(result);
            return result;
        }

        public Vector2[] ToVector(float baseTime = 0)
        {
            var result = new Vector2[Range.Length];
            CopyTo(result, baseTime);
            return result;
        }

        public static AudioClip FromFloats(float[] data, AudioFormat format)
        {
            var bufSize = data.Length * (format.BitsPerSample / 8) * format.Channels;
            var buffer = new byte[bufSize];
            var result = new AudioClip(buffer, format);
            result.CopyFrom(data);
            return result;
        }

        public AudioFormat Format => _format;

        public byte[] Buffer => _buffer;

        public AudioRange Range { get; }
    }
}
