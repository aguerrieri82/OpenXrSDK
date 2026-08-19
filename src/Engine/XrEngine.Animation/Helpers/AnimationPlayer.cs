using System;
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
        protected IAnimationControl? _playback;
        private int _lastFrame;

        public AnimationPlayer(IAnimation animation)
        {
            var scene = EngineApp.Current.ActiveScene;

            if (scene == null)
                throw new NotSupportedException();

            if (!scene.TryComponent<AnimationManager>(out var controller))
                controller = scene.AddComponent<AnimationManager>();

            _manager = controller;
            _animation = animation;
            _state = PlayerState.Stop;
            _lastFrame = -1;

            Fps = 30;

            _ = EnsurePlabackAsync();
        }

        [MemberNotNull(nameof(_playback))]
        protected async Task EnsurePlabackAsync()
        {
            if (_playback != null)
                return;

            await EngineApp.MainThread;

            if (_playback != null)
                return;

            _playback = _manager.Create(_animation);

            _playback.Updated += OnUpdated;
        }

        private void OnUpdated(object? sender, EventArgs e)
        {
            var curFrame = Frame;
            if (curFrame != _lastFrame)
            {
                OnPropertyChanged(nameof(Frame));
                _lastFrame = curFrame;
            }

            var curState = _playback?.State switch
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

            await EnsurePlabackAsync();

            var time = value / (Fps * _animation.Duration);

            _playback.Seek(time);

            OnPropertyChanged(nameof(Frame));
        }


        public async void SetPlayState(PlayerState newState)
        {
            await EngineApp.MainThread;

            if (newState == PlayerState.Play)
            {
                await EnsurePlabackAsync();
                _playback.Play();
            }
            else if (newState == PlayerState.Pause)
            {
                _playback?.Pause();
            }
            else if (newState == PlayerState.Stop)
            {
                _playback?.Stop();
            }
        }

        protected void UpdatePlayState()
        {
            OnPropertyChanged(nameof(PlayState));

            if (_playback == null)
                return;

            if (_state == PlayerState.Stop)
            {
                _playback.Updated -= OnUpdated;
                _playback = null;
            }
        }


        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _playback?.Updated -= OnUpdated;
            GC.SuppressFinalize(this);
        }

        public int Frame
        {
            get => _playback == null
                ? 0
                : (int)MathF.Round(_playback.Time * _animation.Duration * Fps);
            set => SetFrame(value);
        }

        public int Length => (int)MathF.Ceiling(_animation.Duration * Fps);

        public PlayerState PlayState => _state;

        public float Fps { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}