using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace XrEngine.Animation
{
#pragma warning disable CS8774

    public class AnimationPlayer : IPlayer, INotifyPropertyChanged, IDisposable
    {
        protected readonly IAnimationManager _manager;
        protected readonly IAnimation _animation;
        protected PlayerState _state;
        protected IAnimationControl? _control;
        protected IAnimable? _host;
        protected int _lastFrame;

        public AnimationPlayer(IAnimation animation, IAnimable? host)
        {
            var scene = EngineApp.Current.ActiveScene ??
                throw new NotSupportedException();

            if (!scene.TryComponent<AnimationManager>(out var controller))
                controller = scene.AddComponent<AnimationManager>();

            _manager = controller;
            _animation = animation;
            _state = PlayerState.Stop;
            _lastFrame = -1;
            _host = host;

            Fps = 30;

            _ = EnsureCreatedAsync();
        }

        [MemberNotNull(nameof(_control))]
        protected async Task EnsureCreatedAsync()
        {
            if (_control != null)
                return;

            await EngineApp.MainThread;

            if (_control != null)
                return;

            _control = _manager.Create(_animation, _host);

            _control.Updated += OnUpdated;
        }

        private void OnUpdated(object? sender, EventArgs e)
        {
            var curFrame = Frame;
            if (curFrame != _lastFrame)
            {
                OnPropertyChanged(nameof(Frame));
                _lastFrame = curFrame;
            }

            var curState = _control?.State switch
            {
                AnimationState.Playing or
                AnimationState.Pending => PlayerState.Play,

                AnimationState.Paused => PlayerState.Pause,

                _ => PlayerState.Stop
            };

            if (curState != _state)
            {
                _state = curState;
                UpdatePlayState();
            }
        }

        async void SetFrame(int value)
        {
            await EngineApp.MainThread;

            await EnsureCreatedAsync();

            var time = value / (Fps * _animation.Duration);

            _control.Seek(time);

            OnPropertyChanged(nameof(Frame));
        }

        public async void SetPlayState(PlayerState newState)
        {
            await EngineApp.MainThread;

            if (newState == PlayerState.Play)
            {
                await EnsureCreatedAsync();
                _control.Play();
            }
            else if (newState == PlayerState.Pause)
            {
                _control?.Pause();
            }
            else if (newState == PlayerState.Stop)
            {
                _control?.Stop();
            }
        }

        protected void UpdatePlayState()
        {
            OnPropertyChanged(nameof(PlayState));

            if (_control == null)
                return;

            if (_state == PlayerState.Stop)
            {
                _control.Updated -= OnUpdated;
                _control = null;
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _control?.Updated -= OnUpdated;
            GC.SuppressFinalize(this);
        }

        public int Frame
        {
            get => _control == null
                ? 0
                : (int)MathF.Round(_control.Time * _animation.Duration * Fps);
            set => SetFrame(value);
        }

        public int Length => (int)MathF.Ceiling(_animation.Duration * Fps);

        public PlayerState PlayState => _state;

        public float Fps { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}