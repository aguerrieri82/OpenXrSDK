using Android.Media;
using Android.Runtime;
using System.Diagnostics;
using System.Text.Json;


namespace XrEngine.Media.Android
{

    //[SupportedOSPlatform("android30.0")]
    public class AndroidAudioPlayer : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener, IDisposable, IMediaPlayer
    {
        private global::Android.Media.AudioFormat? _audioFormat;
        private AudioAttributes? _attributes;
        private int _durationSamples;
        private AudioTrack? _track;
        private float _duration;
        private float _volume;

        public AndroidAudioPlayer()
        {
            _volume = 1f;
        }

        public void Open(string path)
        {
            var pcmData = DecodeToPCMCache(path, out var format);

            _audioFormat = new global::Android.Media.AudioFormat.Builder()
                .SetSampleRate(format.SampleRate)!
                .SetEncoding(format.SampleType switch
                {
                    AudioSampleType.Float => Encoding.PcmFloat,
                    AudioSampleType.Byte => Encoding.Pcm8bit,
                    AudioSampleType.Short => Encoding.Pcm16bit,
                    _ => throw new NotSupportedException()
                })!
                .SetChannelMask(format.Channels == 1 ? ChannelOut.Mono : ChannelOut.Stereo)
                .Build()!;

            _attributes = new AudioAttributes.Builder()!
                .SetUsage(AudioUsageKind.Media)!
                .SetFlags(AudioFlags.LowLatency)!
                .SetContentType(AudioContentType.Music)!
                .Build()!;

            _track = new AudioTrack.Builder()
                .SetAudioAttributes(_attributes)
                .SetAudioFormat(_audioFormat)
                .SetBufferSizeInBytes(pcmData.Length)
                .SetTransferMode(AudioTrackMode.Static)
                .Build();

            _track.Write(pcmData, 0, pcmData.Length);

            _durationSamples = pcmData.Length;

            var bytesPerSample = _audioFormat.Encoding == global::Android.Media.Encoding.PcmFloat ? 4 : 2;
            var frames = _durationSamples / (_audioFormat.ChannelCount * bytesPerSample);
            _duration = FrameToTime(frames);

            _track.SetVolume(_volume);
        }

        protected void Focus()
        {
            var am = (AudioManager)Application.Context.GetSystemService(global::Android.Content.Context.AudioService)!;
            var req = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(_attributes!)
                .SetOnAudioFocusChangeListener(this)
                .Build()!;

            var ok = am.RequestAudioFocus(req) == AudioFocusRequest.Granted;
            Log.Info(this, $"Focus {ok}");
        }

        public void Play()
        {
            Focus();
            _track?.Play();
            Log.Info(this, "Track Play");
        }

        public void Pause()
        {
            _track?.Pause();
        }

        public void Stop()
        {
            _track?.Stop();
        }

        void IDisposable.Dispose()
        {
            _track?.Dispose();
            _track = null;
            GC.SuppressFinalize(this);
        }

        float FrameToTime(int value)
        {
            Debug.Assert(_audioFormat != null);
            return (float)value / _audioFormat.SampleRate;
        }

        int TimeToFrame(float value)
        {
            Debug.Assert(_audioFormat != null);
            return (int)(value * _audioFormat.SampleRate);
        }

        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                _track?.SetVolume(value);
            }
        }

        public float Position
        {
            get => FrameToTime(_track?.PlaybackHeadPosition ?? 0);
            set
            {
                _track?.SetPlaybackHeadPosition(TimeToFrame(value));
            }
        }

        public float Duration
        {
            get => _duration;
        }


        static byte[] DecodeToPCMCache(string path, out AudioFormat format)
        {
            var cachePath = Context.Require<IPlatform>().CachePath;

            var cacheFile = Path.Combine(cachePath, "Audio", Path.GetFileName(path) + ".pcm");
            var formatFile = Path.Combine(cachePath, "Audio", Path.GetFileName(path) + ".frm");

            var curEditTime = File.GetLastWriteTime(path);

            if (File.Exists(cacheFile) && File.Exists(formatFile))
            {
                var cacheEditTime = File.GetLastWriteTime(cacheFile);
                //if (cacheEditTime >= curEditTime)
                {
                    var formatStr = File.ReadAllText(formatFile);
                    format = JsonSerializer.Deserialize<AudioFormat>(formatStr)!;
                    return File.ReadAllBytes(cacheFile);
                }
            }

            var result = new AndroidAudioDecoder().DecodeToPCM(path, out format);

            Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);

            File.WriteAllBytes(cacheFile, result);
            File.WriteAllText(formatFile, JsonSerializer.Serialize(format));

            return result;
        }



        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        {
            Log.Info(this, $"Focus: {focusChange}");
        }
    }
}
