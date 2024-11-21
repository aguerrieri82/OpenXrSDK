using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.Audio
{
    public class AudioClip
    {
        readonly byte[] _buffer;
        readonly AudioFormat _format;



        public AudioClip(byte[] buffer, AudioFormat format)
        {
            Range = new AudioRange(format);
            Range.Size = buffer.Length;
            _buffer = buffer;   
            _format = format;   
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

        public unsafe void CopyTo(Vector2[] outData, float baseTime = 0)
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
