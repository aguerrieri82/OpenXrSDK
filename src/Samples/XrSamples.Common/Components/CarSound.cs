using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Audio;

namespace XrSamples
{
    public class CarSoundStream : IAudioStream
    {
        protected float _lastRpm;
        protected float _lastSample;
        protected bool _isStreaming;

        public CarSoundStream()
        {
            Format = new AudioFormat
            {
                BitsPerSample = 16,
                Channels = 1,
                SampleRate = 44100
            };
            FrequencyFactor = 10f;
            LowPassAlpha = 0.08f;
            GearRatios = [3.5f, 2.1f, 1.5f, 1.0f, 0.8f];
            Gear = 1;
            SmoothFactor = 0.05f;
        }

        public unsafe uint Fill(byte[] data, float timeSec)
        {
            uint samplesProvided = 0;

            fixed(byte* pData = data)
            {
                var pShort = (short*)pData;

               
                while (samplesProvided < data.Length / 2)
                {
                    //var curRpm = _lastRpm + (Rpm - _lastRpm) * (samplesProvided / (data.Length / 2));

                    _lastRpm += (Rpm - _lastRpm) * SmoothFactor;

                    var curTime = timeSec + (samplesProvided / (float)Format.SampleRate);

                    float frequency = (_lastRpm / 60f) * FrequencyFactor * GearRatios[Gear - 1];

                    // Generate the engine sound using a sawtooth wave (richer harmonics)
                    float sampleValue = GenerateSawtoothWave(frequency, curTime);

                    // Apply low-pass filter to simulate engine load
                    sampleValue = ApplyLowPassFilter(sampleValue);

                    // Apply distortion to simulate engine roughness
                    sampleValue = ApplyDistortion(sampleValue, 0.5f);

                    // Write to buffer
                    pShort[samplesProvided] = (short)(sampleValue * short.MaxValue);
                    samplesProvided++;
                }

            }
            return samplesProvided;
        }

        private float GenerateSawtoothWave(float frequency, float time)
        {
            float period = 1f / frequency;
            float t = time % period;
            return 2f * (t / period) - 1f;
        }

        private float ApplyLowPassFilter(float input)
        {
            // Simple single-pole low-pass filter
            float output = LowPassAlpha * input + (1f - LowPassAlpha) * _lastSample;
            _lastSample = output;
            return output;
        }

        private float ApplyDistortion(float input, float gain)
        {
            input *= gain;
            return (float)Math.Tanh(input);
        }

        public void Start()
        {
            _isStreaming = true;
        }

        public void Stop()
        {
            _isStreaming = false; 
        }

        public float LowPassAlpha { get; set; }

        public float FrequencyFactor { get; set; }

        public float SmoothFactor { get; set; }

        public float[] GearRatios { get; set; }

        public int Gear { get; set; }

        public int Rpm { get; set; }

        public uint PrefBufferSize => 0;

        public uint PrefBufferCount => 0;

        public float Length => 0;

        public AudioFormat Format { get; }

        public bool IsStreaming => _isStreaming;
    }

    public class CarSound : AudioEmitter
    {
        CarSoundStream _engine;

        public CarSound()
        {
            _engine = new CarSoundStream(); 
        }

        protected override void Start(RenderContext ctx)
        {
            _ = PlayAsync(_engine, () => _host!.Forward);

            base.Start(ctx);
        }

        public override void Reset(bool onlySelf = false)
        {
            Stop();
            base.Reset(onlySelf);
        }


        [Range(0, 1, 0.01f)]
        public float LowPassAlpha
        {
            get => _engine.LowPassAlpha;
            set => _engine.LowPassAlpha = value;
        }


        [Range(0, 1, 0.01f)]
        public float SmoothFactor
        {
            get => _engine.SmoothFactor;
            set => _engine.SmoothFactor = value;
        }



        public CarSoundStream Engine => _engine; 

    }
}
