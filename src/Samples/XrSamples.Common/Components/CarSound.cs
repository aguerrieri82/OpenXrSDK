using OpenAl.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XrEngine;
using XrEngine.Audio;
using XrEngine.OpenXr;
using XrMath;

namespace XrSamples
{
    public interface ICarSound : IAudioStream
    {
        float SmoothFactor { get; set; }

        int Rpm { get; set; }

        int Gear { get; set; }

    }


    public class CarSoundV1 : ICarSound
    {
        protected float _lastRpm;
        protected float _lastSample;
        protected bool _isStreaming;

        public CarSoundV1()
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

        public unsafe int Fill(byte[] data, float timeSec)
        {
            int samplesProvided = 0;

            fixed (byte* pData = data)
            {
                var pShort = (short*)pData;

                while (samplesProvided < data.Length / 2)
                {
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

        public int PrefBufferSize => 0;

        public int PrefBufferCount => 0;

        public float Length => 0;

        public AudioFormat Format { get; }

        public bool IsStreaming => _isStreaming;
    }


    public class CarSoundV2 : AudioLooper, ICarSound
    {
        AudioSlicer _slicer;
        byte[] _buffer = [];

        public CarSoundV2()
        {
            var path = new Path2();
            path.ParseSvgPath("M94.92343,327.20377c0,0 49.25465,-0.47464 58.92396,-1.38418c9.66931,-0.90954 19.87223,-4.79691 31.45914,-9.92189c30.0538,-13.29304 123.65848,-49.52857 146.03591,-56.0304c22.37742,-6.50183 78.53462,-16.79634 116.31143,-19.88525c16.26465,-1.32992 50.68774,-3.56868 58.35304,-3.85481c7.6653,-0.28614 16.32354,0.02712 19.03093,1.30367c2.70739,1.27655 6.71171,5.9732 9.356,10.02931c2.64429,4.05611 20.62308,30.88793 21.98103,40.79565");

            var pathBounds = path.Bounds();

            var func = path.ToFunctionY(0.1f, -1);

            var asset = Context.Require<IAssetStore>();

            var soundPath = asset.GetPath("CarSound.wav");
            var reader = new WavReader();
            using var stream = File.OpenRead(soundPath);
            var data = reader.Decode(stream);

            /*
            if (XrPlatform.IsEditor)
            {
                Context.Require<IFunctionView>()
                .ShowDft(data.ToFloat(), (uint)data.Format!.SampleRate, 1024);
            }
            */

            _slicer = new AudioSlicer
            {
                Data = data,
                Function = func,
                StartTime = pathBounds.Min.X,
                EndTime = pathBounds.Max.X,
                MinValue = 2000,
                MaxValue = 50,
                //OffsetMap = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(asset.GetPath("loops.json")).Replace("0,2","0.2"))!
            };

            var time = _slicer.TimeForValue(100);

            Loop = new AudioData(data.Format, _buffer);
            SmoothFactor = 0.02f;
            SliceLen = 0.2f;
            FadeSize = 0.05f;

            LoadNextBuffer();
        }

        protected override void LoadNextBuffer()
        {
            var rnd = (int)(10 * new Random().NextSingle());
            _slicer.FillBuffer((int)Rpm + 0, SliceLen, ref _buffer);
            LoadBuffer(_buffer);
        }

        public float SliceLen { get; set; }

        public float SmoothFactor { get; set; }

        public int Gear { get; set; }

        public int Rpm { get; set; }
    }

    public class CarSound : AudioEmitter
    {
        CarSoundV2 _engine;

        public CarSound()
        {
            _engine = new CarSoundV2();
        }

        protected override void Start(RenderContext ctx)
        {
            //_ = PlayAsync(_engine, () => _host!.Forward);

            base.Start(ctx);
        }

        public override void Reset(bool onlySelf = false)
        {
            Stop();
            base.Reset(onlySelf);
        }

        [Action()]
        public void Save()
        {
            File.WriteAllBytes("d:\\test.pcm", _engine.Loop.Buffer);
        }

        [Range(0, 1, 0.001f)]
        public float Pitch
        {
            get => _curSource!.Pitch;
            set => _curSource!.Pitch = value;
        }


        [Range(0, 1, 0.001f)]
        public float FadeSize
        {
            get => _engine.FadeSize;
            set => _engine.FadeSize = value;
        }

        [Range(0, 1, 0.01f)]
        public float SliceLen
        {
            get => _engine.SliceLen;
            set => _engine.SliceLen = value;
        }


        [Range(0, 1, 0.01f)]
        public float SmoothFactor
        {
            get => _engine.SmoothFactor;
            set => _engine.SmoothFactor = value;
        }

        [Range(40, 2000, 1)]
        public float Rpm
        {
            get => _engine.Rpm;
            set => _engine.Rpm = (int)value;
        }

        public CarSoundV2 Engine => _engine;

    }
}
