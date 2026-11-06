
using ManagedBass;
using ManagedBass.Midi;
using XrEngine.Media;

namespace XrEngine.Music
{
    [AIGenerated]
    public class BassMidiSynth : ISynth, IDisposable
    {
        private readonly int _soundFont;
        private readonly int _stream;
        private readonly LockDispatcher _dispatcher;
        private readonly BassMidiAudioStream _audioStream;
        private float _volume = 1f;
        private int _channel;
        private bool _disposed;

        public BassMidiSynth(string soundFontPath, int sampleRate = 48000, int channel = 0)
        {
            if (!Bass.Init(0, sampleRate) && Bass.LastError != Errors.Already)
                throw new InvalidOperationException($"BASS initialization failed: {Bass.LastError}");

            var plugin = Bass.PluginLoad("bassflac");

            if (plugin == 0 && Bass.LastError != Errors.Already)
                throw new InvalidOperationException($"Unable to load BASSFLAC: {Bass.LastError}");

            Bass.CurrentDevice = 0;

            _soundFont = BassMidi.FontInit(soundFontPath, 0);
            if (_soundFont == 0)
                throw new InvalidOperationException($"Unable to load SoundFont: {Bass.LastError}");



            try
            {
                _stream = BassMidi.CreateStream(16, BassFlags.Decode | BassFlags.Float, sampleRate);
                if (_stream == 0)
                    throw new InvalidOperationException($"Unable to create MIDI stream: {Bass.LastError}");

                var fonts = new[] { new MidiFont { Handle = _soundFont, Preset = 0, Bank = 128 } };

                if (BassMidi.StreamSetFonts(_stream, fonts, fonts.Length) == 0)
                    throw new InvalidOperationException($"Unable to configure SoundFont: {Bass.LastError}");

                _dispatcher = new LockDispatcher(this);
                _audioStream = new BassMidiAudioStream(_stream, sampleRate, _dispatcher.Lock);

                Stream = _audioStream;
                Channel = channel;
                DrumKit = DrumKits.Gm;
            }
            catch
            {
                if (_stream != 0)
                    Bass.StreamFree(_stream);

                BassMidi.FontFree(_soundFont);
                throw;
            }
        }

        public void NoteOn(int note, float velocity)
        {
            int value = Math.Clamp((int)(velocity * 127), 0, 127);
            Send(MidiEventType.Note, note | (value << 8));
        }

        public void NoteOff(int note, float velocity)
        {
            Send(MidiEventType.Note, note);
        }

        public void AllSoundOff()
        {
            _dispatcher.Post(() =>
            {
                for (int i = 0; i < 16; i++)
                {
                    if (!BassMidi.StreamEvent(_stream, i, MidiEventType.SoundOff, 0))
                        throw new InvalidOperationException($"MIDI event failed: {Bass.LastError}");
                }
            });
        }

        public void ControlCode(int number, int value)
        {
            if (number < 0 || number > 127 || value < 0 || value > 127)
                throw new ArgumentOutOfRangeException();

            _dispatcher.Post(() =>
            {
                var data = new byte[] { (byte)(0xB0 | _channel), (byte)number, (byte)value };

                if (BassMidi.StreamEvents(_stream, MidiEventsMode.Raw, data) < 0)
                    throw new InvalidOperationException($"MIDI control failed: {Bass.LastError}");
            });
        }

        private void Send(MidiEventType type, int parameter)
        {
            _dispatcher.Post(() =>
            {
                if (!BassMidi.StreamEvent(_stream, _channel, type, parameter))
                    throw new InvalidOperationException($"MIDI event failed: {Bass.LastError}");

                if (!BassMidi.FontGetInfo(_soundFont, out var info))
                    throw new Exception($"FontGetInfo: {Bass.LastError}");

                Console.WriteLine($"Presets: {info.Presets}");
                Console.WriteLine($"Sample format: {info.SampleDataType}");
                Console.WriteLine($"Sample bytes loaded: {info.SampleDataLoaded}");
            });
        }

        public IAudioStream Stream { get; }

        public float Volume
        {
            get => _volume;

            set
            {
                if (!float.IsFinite(value) || value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _dispatcher.Post(() =>
                {
                    if (!Bass.ChannelSetAttribute(_stream, (ChannelAttribute)0x1200A, value))
                        throw new InvalidOperationException($"Unable to set MIDI volume: {Bass.LastError}");

                    _volume = value;
                });
            }
        }

        public int Channel
        {
            get => _channel;

            set
            {
                if (value < 0 || value >= 16)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _dispatcher.Post(() => _channel = value);
            }
        }

        public SynthCaps Caps => SynthCaps.Volume | SynthCaps.Streamable;

        public DrumKit DrumKit { get; set; }

        public void Dispose()
        {
            lock (_dispatcher.Lock)
            {
                if (_disposed)
                    return;

                _audioStream.Close();
                Bass.StreamFree(_stream);
                BassMidi.FontFree(_soundFont);

                _disposed = true;
            }
        }
    }
}
