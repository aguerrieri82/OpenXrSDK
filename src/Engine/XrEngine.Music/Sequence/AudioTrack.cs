

namespace XrEngine.Music
{

    public enum AudioTrackState
    {
        Stop,
        Play,
        Pause,
        Record
    }

    public abstract class AudioTrack : IActiveService
    {
        protected AudioSequencer? _sequencer;
        protected List<BaseAudioBlock> _blocks = [];
        protected int _index;
        protected Thread? _playThread;
        protected float _volume;
        protected bool _isRecording;
        protected bool _isMute;
        protected bool _isSolo;
        protected BaseAudioBlock? _activeBlock;

        protected AudioTrackType _type;
        protected AudioTrackState _state;

        public AudioTrack(AudioTrackType type)
        {
            _type = type;
        }

        protected internal void AddBlock(BaseAudioBlock block)
        {
            _blocks.Add(block);
            block.Attach(this);
        }

        protected internal void Attach(AudioSequencer sequencer, int index)
        {
            _sequencer = sequencer;
            _index = index;
        }

        protected void PlayLoop()
        {
            while (_state != AudioTrackState.Stop)
            {
                if (_state == AudioTrackState.Play && IsAudible)
                    PlayWork();
                else
                    Thread.Sleep(1);
            }
        }

        public void Start()
        {
            if (IsRecording)
                Record();
            else
                Play();
        }

        public void Play()
        {
            if (_state == AudioTrackState.Play)
                return;

            if (_state == AudioTrackState.Record)
                Stop();

            if (_state != AudioTrackState.Pause)
            {
                _state = AudioTrackState.Play;
                _playThread = new Thread(PlayLoop);
                _playThread.Start();
            }
            else
                _state = AudioTrackState.Play;
        }

        public void Record()
        {
            if (_state == AudioTrackState.Record)
                return;

            if (_state == AudioTrackState.Play || _state == AudioTrackState.Pause)
                Stop();

            _state = AudioTrackState.Record;
            StartRecord();
        }

        public void Stop()
        {
            if (_state == AudioTrackState.Record)
            {
                StopRecord();
                _state = AudioTrackState.Stop;
            }
            else if (_state != AudioTrackState.Stop)
            {
                _state = AudioTrackState.Stop;

                if (_playThread != null && Thread.CurrentThread != _playThread)
                    _playThread.Join();

                _playThread = null;
                StopPlay();
            }
        }

        public void Pause()
        {
            if (_state != AudioTrackState.Play && _state != AudioTrackState.Record)
                return;

            _state = AudioTrackState.Pause;
            PauseWork();
        }

        protected virtual void StopPlay()
        {
        }

        protected virtual void StopRecord()
        {
        }

        protected virtual void StartRecord()
        {
        }

        protected virtual void PauseWork()
        {
        }

        protected virtual void PlayWork()
        {
        }

        internal virtual void SetPosition(float value)
        {
        }

        public void Dispose()
        {
            Stop();
        }

        public Guid Id { get; set; }

        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }

        public bool IsMute
        {
            get => _isMute;
            set => _isMute = value;
        }

        public bool IsSolo
        {
            get => _isSolo;
            set => _isSolo = value;
        }

        public bool IsRecording
        {
            get => _isRecording;
            set => _isRecording = value;
        }

        public bool IsAudible
        {
            get
            {
                if (_isMute)
                    return false;

                if (_sequencer == null)
                    return true;

                return !_sequencer.Tracks.Any(a => a.IsSolo) || _isSolo;
            }
        }

        public AudioTrackType Type => _type;

        public AudioTrackState State => _state;

        public AudioSequencer? Sequencer => _sequencer;

        public int Index => _index;

        public float Duration => _blocks.Count == 0 ? 0 : _blocks.Max(a => a.End);

        public IReadOnlyList<BaseAudioBlock> Blocks => _blocks;
    }
}