using System.ComponentModel;
using System.Text.Json;

namespace XrEngine
{
    public abstract class BaseFramePlayer<TFrame, TObj> : Behavior<TObj>, INotifyPropertyChanged, IPlayer, IReferenceTime
        where TObj : Object3D
        where TFrame : RecordFrame
    {

        static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
        };

        protected RecordSession<TFrame>? _session;
        protected TFrame _frame = null!;
        protected int _frameNum = -1;
        protected PlayerState _state;
        protected double _lastFrameRealTime;
        protected DateTime _startRealTime;

        protected BaseFramePlayer()
        {
            IsEnabled = true;
        }

        protected override void Start(RenderContext ctx)
        {
            _startRealTime = DateTime.Now;
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            if (_session?.Frames == null)
                return;

            if (_state == PlayerState.Play)
            {
                var curRealTime = UseReferenceTime ? (DateTime.Now - _startRealTime).TotalSeconds : ctx.Time;

                if (RealTime && _frameNum >= 0 && _frameNum + 1 < _session.Frames.Count)
                {
                    var nextFrame = _session.Frames[_frameNum + 1];
                    var frameDt = nextFrame.Time - _frame.Time;
                    var curDt = curRealTime - _lastFrameRealTime;

                    if (curDt < frameDt)
                        return;
                }

                var lastFrame = LastFrame > 0 ? LastFrame : Length - 1;

                if (_frameNum >= lastFrame)
                {
                    if (Loop)
                    {
                        Frame = FirstFrame;
                        OnLoopStart();
                    }
                    else
                        SetPlayState(PlayerState.Stop);
                }
                else
                    Frame++;

                _lastFrameRealTime = curRealTime;
            }

            if (UseReferenceTime && _host?.Scene?.App != null)
                _host.Scene.App.ReferenceTime = _state == PlayerState.Stop ? null : this;
        }

        protected void LoadFrame()
        {
            if (_session?.Frames == null || _frameNum < 0 || _frameNum >= _session.Frames.Count)
                return;

            _frame = _session.Frames[_frameNum];
            ApplyFrame(_frame);
        }

        protected virtual void OnLoopStart()
        {

        }

        protected abstract void ApplyFrame(TFrame frame);

        [Action]
        public async Task LoadAsync()
        {
            var path = Context.Require<IPlatform>().SharedPath;

            using var stream = File.OpenRead(Path.Join(path, SourceFile));
            _session = await JsonSerializer.DeserializeAsync<RecordSession<TFrame>>(stream, JSON_OPTIONS);

            _frameNum = -1;
            Frame = FirstFrame;

            OnPropertyChanged(nameof(Length));
        }

        public void SetPlayState(PlayerState state)
        {
            _state = state;

            if (state == PlayerState.Stop)
            {
                _lastFrameRealTime = 0;
                Frame = FirstFrame;
            }
            else if (state == PlayerState.Play)
            {
                _startRealTime = DateTime.Now;
                _lastFrameRealTime = 0;
            }

            OnPropertyChanged(nameof(PlayState));
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(SourceFile), SourceFile);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            SourceFile = container.Read<string>(nameof(SourceFile));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int Frame
        {
            get => _frameNum;
            set
            {
                if (Length == 0)
                    return;

                var lastFrame = LastFrame > 0 ? LastFrame : Length - 1;
                value = Math.Min(Math.Max(0, value), lastFrame);

                if (value == _frameNum)
                    return;

                _frameNum = value;
                LoadFrame();

                OnPropertyChanged(nameof(Frame));
            }
        }

        public bool Loop { get; set; }

        public int FirstFrame { get; set; }

        public int LastFrame { get; set; }

        public bool UseReferenceTime { get; set; }

        public bool RealTime { get; set; }

        public PlayerState PlayState => _state;

        public int Length => _session?.Frames?.Count ?? 0;

        [ValueType(ValueType.FileName)]
        public string? SourceFile { get; set; }

        double IReferenceTime.Time => _frame.Time;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}