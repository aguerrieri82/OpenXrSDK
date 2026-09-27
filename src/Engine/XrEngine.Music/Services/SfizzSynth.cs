using Sfizz;
using XrEngine.Media;
using static Sfizz.SfizzLib;

namespace XrEngine.Music
{
    public class SfizzSynth : ISynth
    {
        private Synth _synth;
        private SfizzLib.Buffer _buffer;
        private readonly LockDispatcher _dispatcher;

        public SfizzSynth(string soundFontPath)
        {
            _synth = createSynth();
            _dispatcher = new(_synth);

            var cfg = new Config
            {
                ChannelSizeSamples = 128,
                NumVoices = 64,
                ProcessMode = ProcessMode.ProcessLive,
                SampleQuality = 2,
                OscillatorQuality = 2,
                SampleRate = 48000
            };

            _synth.loadSfzFile(soundFontPath);
            _synth.configure(ref cfg);
            _buffer = createBuffer(cfg.ChannelSizeSamples, 2);

            Stream = new SfizzAudioStream(_synth, _buffer, cfg.ChannelSizeSamples * 2, cfg.SampleRate)
            {
                Dispatcher = _dispatcher
            };
        }

        public void NoteOn(int note, float velocity)
        {
            _ = _dispatcher.ExecuteAsync(() =>
            {
                _synth.noteOn(0, note, (int)(velocity * 127));
            });
        }

        public void NoteOff(int note, float velocity)
        {
            _ = _dispatcher.ExecuteAsync(() =>
            {
                _synth.noteOff(0, note, (int)(velocity * 127));
            });
        }

        public void AllSoundOff()
        {
            _ = _dispatcher.ExecuteAsync(() =>
            {
                _synth.allSoundOff();
            });
        }

        public void ControlCode(int number, int value)
        {
            _ = _dispatcher.ExecuteAsync(() =>
            {
                _synth.controlCode(0, number, value);
            });
        }

        public IAudioStream Stream { get; }

        public float Volume
        {
            get => _synth.getVolume();

            set => _synth.setVolume(value);
        }

        public SynthCaps Caps => SynthCaps.Volume | SynthCaps.Streamable;
    }
}
