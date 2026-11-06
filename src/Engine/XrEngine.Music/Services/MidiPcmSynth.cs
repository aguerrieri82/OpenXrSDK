
using System;
using System.IO;
using System.Runtime.InteropServices;
using ManagedBass;
using ManagedBass.Midi;

namespace XrEngine.Music
{
    public sealed class MidiPcmSynth : IDisposable
    {
        private readonly int _soundFont;
        private readonly int _sampleRate;
        private readonly bool[] _enabled = new bool[16];
        private readonly float[] _volumes = new float[16];
        private int _stream;

        public int SampleRate => _sampleRate;
        public int Channels => 2;

        public MidiPcmSynth(string soundFontPath, int sampleRate = 48000)
        {
            _sampleRate = sampleRate;

            if (!Bass.Init(0, _sampleRate))
                throw new InvalidOperationException($"BASS initialization failed: {Bass.LastError}");

            _soundFont = BassMidi.FontInit(soundFontPath, 0);
            if (_soundFont == 0)
                throw new InvalidOperationException($"Unable to load SoundFont: {Bass.LastError}");

            for (int i = 0; i < 16; i++)
            {
                _enabled[i] = true;
                _volumes[i] = 1f;
            }
        }

        public void SetVolume(float volume)
        {
            if (!Bass.ChannelSetAttribute(_stream, (ChannelAttribute)0x1200A, volume))
                throw new InvalidOperationException($"Unable to set MIDI volume: {Bass.LastError}");
        }

        public void LoadMidi(string midiPath)
        {
            if (!File.Exists(midiPath))
                throw new FileNotFoundException(midiPath);

            if (_stream != 0)
            {
                Bass.StreamFree(_stream);
                _stream = 0;
            }

            _stream = BassMidi.CreateStream(midiPath, 0, 0, BassFlags.Decode | BassFlags.Float | BassFlags.MidiDecayEnd, _sampleRate);
            if (_stream == 0)
                throw new InvalidOperationException($"Unable to load MIDI: {Bass.LastError}");

            try
            {
                var fonts = new[] { new MidiFont { Handle = _soundFont, Preset = -1, Bank = 0 } };

                if (BassMidi.StreamSetFonts(_stream, fonts, fonts.Length) == 0)
                    throw new InvalidOperationException($"Unable to configure SoundFont: {Bass.LastError}");

                for (int i = 0; i < 16; i++)
                    ApplyChannel(i);
            }
            catch
            {
                Bass.StreamFree(_stream);
                _stream = 0;
                throw;
            }
        }

        public void SetChannelEnabled(int channel, bool enabled)
        {
            CheckChannel(channel);
            _enabled[channel] = enabled;
            ApplyChannel(channel);
        }

        public void SetChannelVolume(int channel, float volume)
        {
            CheckChannel(channel);

            if (float.IsNaN(volume) || volume < 0 || volume > 2)
                throw new ArgumentOutOfRangeException(nameof(volume));

            _volumes[channel] = volume;
            ApplyChannel(channel);
        }

        private void ApplyChannel(int channel)
        {
            if (_stream == 0)
                return;

            int level = _enabled[channel] ? (int)Math.Round(_volumes[channel] * 100) : 0;

            if (!BassMidi.StreamEvent(_stream, channel, MidiEventType.Level, level))
                throw new InvalidOperationException($"Unable to set channel level: {Bass.LastError}");
        }

        public byte[] RenderPcm(bool autoGain = false, float peakCeiling = 0.89f)
        {
            if (_stream == 0)
                throw new InvalidOperationException("No MIDI loaded");

            if (autoGain && (peakCeiling <= 0 || peakCeiling > 1 || float.IsNaN(peakCeiling)))
                throw new ArgumentOutOfRangeException(nameof(peakCeiling));

            if (!Bass.ChannelSetPosition(_stream, 0))
                throw new InvalidOperationException($"Unable to reset MIDI: {Bass.LastError}");

            for (int i = 0; i < 16; i++)
                ApplyChannel(i);

            using var output = new MemoryStream();
            var buffer = new byte[65536];

            while (true)
            {
                int length = Bass.ChannelGetData(_stream, buffer, buffer.Length);

                if (length < 0)
                {
                    if (Bass.LastError == Errors.Ended)
                        break;

                    throw new InvalidOperationException($"MIDI rendering failed: {Bass.LastError}");
                }

                if (length == 0)
                    break;

                output.Write(buffer, 0, length);
            }

            var samples = MemoryMarshal.Cast<byte, float>(output.GetBuffer().AsSpan(0, (int)output.Length));

            if (samples.Length % 2 != 0)
                throw new InvalidDataException("Invalid stereo PCM length");

            var pcm = new byte[samples.Length * sizeof(short)];
            var result = MemoryMarshal.Cast<byte, short>(pcm);

            if (autoGain)
            {
                int frames = samples.Length / 2;
                int blockFrames = Math.Max(1, _sampleRate / 100);

                float globalGain = 1f;

                if (frames > 0)
                {
                    var peaks = new float[(frames + blockFrames - 1) / blockFrames];

                    for (int b = 0; b < peaks.Length; b++)
                    {
                        float peak = 0;
                        int end = Math.Min(frames, (b + 1) * blockFrames);

                        for (int i = b * blockFrames; i < end; i++)
                        {
                            int offset = i * 2;
                            peak = MathF.Max(peak, MathF.Max(MathF.Abs(samples[offset]), MathF.Abs(samples[offset + 1])));
                        }

                        peaks[b] = peak;
                    }

                    Array.Sort(peaks);

                    float referencePeak = peaks[(int)((peaks.Length - 1) * 0.90f)];

                    if (referencePeak > peakCeiling)
                        globalGain = peakCeiling * 0.9f / referencePeak;
                }

                var gains = MemoryMarshal.Cast<byte, float>(pcm);

                float attack = MathF.Exp(-1f / (_sampleRate * 0.005f));
                float release = MathF.Exp(-1f / (_sampleRate * 0.15f));
                float gain = 1f;

                for (int i = frames - 1; i >= 0; i--)
                {
                    int offset = i * 2;

                    float peak = MathF.Max(MathF.Abs(samples[offset]), MathF.Abs(samples[offset + 1])) * globalGain;
                    float required = peak > peakCeiling ? peakCeiling / peak : 1f;

                    gain = MathF.Min(required, 1f - (1f - gain) * attack);
                    gains[i] = gain;
                }

                gain = 1f;

                for (int i = 0; i < frames; i++)
                {
                    gain = MathF.Min(gains[i], 1f - (1f - gain) * release);

                    int offset = i * 2;
                    float finalGain = globalGain * gain;

                    result[offset] = ToPcm16(samples[offset] * finalGain);
                    result[offset + 1] = ToPcm16(samples[offset + 1] * finalGain);
                }
            }
            else
            {
                for (int i = 0; i < samples.Length; i++)
                    result[i] = ToPcm16(samples[i]);
            }

            return pcm;
        }

        private static short ToPcm16(float value)
        {
            return (short)MathF.Round(Math.Clamp(value, -1f, 1f) * 32767f);
        }

        private static void CheckChannel(int channel)
        {
            if (channel < 0 || channel >= 16)
                throw new ArgumentOutOfRangeException(nameof(channel));
        }

        public void Dispose()
        {
            if (_stream != 0)
            {
                Bass.StreamFree(_stream);
                _stream = 0;
            }

            if (_soundFont != 0)
                BassMidi.FontFree(_soundFont);
        }
    }
}
