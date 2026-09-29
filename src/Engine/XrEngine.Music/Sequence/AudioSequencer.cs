
using XrEngine.Devices;

namespace XrEngine.Music
{
    public class AudioSequencerEvent
    {
        public AudioSequencerEvent(AudioTrack track, DrumEvent? drumEvent, float waitTime)
        {
            Track = track;
            Event = drumEvent;
            WaitTime = waitTime;
        }

        public AudioTrack Track;

        public DrumEvent? Event;

        public float WaitTime;
    }

    public class AudioSequencerLoopEvent
    {
        public float Position;
        public float Target;
        public bool Cancel;
    }


    public class AudioSequencer
    {
        protected readonly List<AudioTrack> _tracks = [];

        protected float _position;
        protected float _timeScale;
        protected ulong _refTime;
        protected bool _isStarted;

        protected Thread? _loopThread;

        protected float _loopStart;
        protected float _loopEnd;
        protected bool _loopEnabled;

        public AudioSequencer()
        {
            _timeScale = 1f;
        }

        public void Pause()
        {
            if (!_isStarted)
                return;

            _position = Position;
            _isStarted = false;

            StopLoopThread();

            foreach (var track in _tracks)
                track.Pause();
        }

        public void Stop()
        {
            StopLoopThread();

            foreach (var track in _tracks)
                _ = Task.Run(() => track.Stop());

            _isStarted = false;

            SetPosition(0);
        }

        public void Start()
        {
            if (_isStarted)
                return;

            _refTime = ReferenceClock.Default.Now;

            foreach (var track in _tracks)
                track.Start();

            _isStarted = true;

            StartLoopThread();
        }

        protected void StartLoopThread()
        {
            if (_loopThread != null)
                return;

            _loopThread = new Thread(LoopThread)
            {
                IsBackground = true,
                Name = "AudioSequencer.Loop"
            };

            _loopThread.Start();
        }

        protected void StopLoopThread()
        {
            var thread = _loopThread;

            if (thread == null)
                return;

            _loopThread = null;

            if (Thread.CurrentThread != thread)
                thread.Join();
        }

        protected void LoopThread()
        {
            while (_loopThread != null)
            {
                if (!_loopEnabled || _loopEnd <= _loopStart)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var position = Position;

                if (position >= _loopEnd)
                {
                    var duration = _loopEnd - _loopStart;
                    var target = _loopStart + (position - _loopStart) % duration;

                    var args = new AudioSequencerLoopEvent
                    {
                        Position = position,
                        Target = target
                    };

                    LoopBoundary?.Invoke(this, args);

                    if (!args.Cancel)
                        SetPosition(args.Target);
                }

                Thread.Sleep(2);
            }
        }

        public void SetPosition(float value)
        {
            _position = value;
            _refTime = ReferenceClock.Default.Now;

            foreach (var track in _tracks)
                track.SetPosition(value);
        }

        public void AddTrack(AudioTrack track)
        {
            track.Attach(this, _tracks.Count);
            _tracks.Add(track);
        }

        public AudioTrack? GetTrack(Guid id)
        {
            return _tracks.FirstOrDefault(a => a.Id == id);
        }

        protected internal void OnEvent(AudioTrack track, DrumEvent drumEvent, float waitTime)
        {
            Event?.Invoke(this, new AudioSequencerEvent(track, drumEvent, waitTime));
        }

        protected internal void AdjustPosition(float playPosition)
        {
            _position = playPosition;
            _refTime = ReferenceClock.Default.Now;
        }

        public float Position
        {
            get
            {
                if (!_isStarted)
                    return _position;

                var elapsed = (ReferenceClock.Default.Now - _refTime) / 1000000000f;

                return _position + elapsed * _timeScale;
            }
        }


        public IAudioOut? MainOut { get; set; }

        public Guid Id { get; set; }

        public float LoopStart
        {
            get => _loopStart;
            set => _loopStart = value;
        }

        public float LoopEnd
        {
            get => _loopEnd;
            set => _loopEnd = value;
        }

        public bool LoopEnabled
        {
            get => _loopEnabled;
            set => _loopEnabled = value;
        }

        public float TimeScale
        {
            get => _timeScale;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (_timeScale == value)
                    return;

                if (_isStarted)
                    _position = Position;

                _refTime = ReferenceClock.Default.Now;
                _timeScale = value;
            }
        }

        public IReadOnlyList<AudioTrack> Tracks => _tracks;

        public event EventHandler<AudioSequencerEvent>? Event;

        public event EventHandler<AudioSequencerLoopEvent>? LoopBoundary;
    }
}