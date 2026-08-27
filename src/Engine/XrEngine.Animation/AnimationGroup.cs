namespace XrEngine.Animation
{
    public enum AnimationGroupMode
    {
        Parallel,
        Sequential
    }

    public class AnimationGroup : EngineObject, IAnimation
    {
        #region Control

        protected class Control : BaseAnimationControl<AnimationGroup>
        {
            protected readonly List<IAnimationControl> _controls = [];

            protected int _current;

            public Control(IAnimationManager manager, AnimationGroup animation, IAnimable? host)
                : base(manager, animation, host)
            {
                foreach (var child in animation._animations)
                    _controls.Add(child.CreateControl(manager, host));
            }

            protected override bool Evaluate(float time, float referenceTime)
            {
                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _controls)
                        playback.Step(referenceTime);
                }
                else if (_current < _controls.Count)
                {
                    var playback = _controls[_current];

                    playback.Step(referenceTime);

                    if (playback.State == AnimationState.Completed)
                    {
                        _current++;

                        if (_current < _controls.Count)
                            _controls[_current].Play();
                    }
                }

                return true;
            }

            protected override void OnReset()
            {
                _current = 0;

                foreach (var playback in _controls)
                    playback.Stop();

                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _controls)
                        playback.Play();
                }
                else if (_controls.Count > 0)
                {
                    _controls[0].Play();
                }
            }

            protected override void OnIterationChanged()
            {
                OnReset();
            }

            protected override void OnStateChanged(AnimationState state)
            {
                switch (state)
                {
                    case AnimationState.Paused:
                        foreach (var playback in _controls)
                            playback.Pause();
                        break;

                    case AnimationState.Playing:
                        foreach (var playback in _controls)
                        {
                            if (playback.State == AnimationState.Paused)
                                playback.Play();
                        }
                        break;

                    case AnimationState.Stopped:
                    case AnimationState.Completed:
                        foreach (var playback in _controls)
                            playback.Stop();
                        break;
                }
            }

            protected override void OnSeek()
            {
                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _controls)
                        Seek(playback, _animationTime);
                }
                else
                {
                    SeekSequential(_animationTime);
                }
            }

            protected void SeekSequential(float time)
            {
                var offset = 0f;

                for (var i = 0; i < _controls.Count; i++)
                {
                    var playback = _controls[i];
                    var animation = playback.Animation;

                    var start = offset + animation.Delay;
                    var end = start + animation.Duration;

                    if (time < start)
                    {
                        playback.Seek(0);
                        playback.Pause();
                    }
                    else if (time >= end)
                    {
                        playback.Seek(1);
                        playback.Pause();
                    }
                    else
                    {
                        _current = i;

                        playback.Seek((time - start) / animation.Duration);

                        if (_state == AnimationState.Playing)
                            playback.Play();
                        else
                            playback.Pause();
                    }

                    offset = end;
                }
            }

            protected static void Seek(IAnimationControl playback, float time)
            {
                var animation = playback.Animation;

                var localTime = time - animation.Delay;

                if (localTime <= 0)
                {
                    playback.Seek(0);
                    playback.Pause();
                }
                else if (localTime >= animation.Duration)
                {
                    playback.Seek(1);
                    playback.Pause();
                }
                else
                {
                    playback.Seek(localTime / animation.Duration);
                }
            }
        }

        #endregion

        protected readonly List<IAnimation> _animations = [];
        protected AnimationGroupMode _mode;
        protected float _duration;

        public IAnimationControl CreateControl(IAnimationManager manager, IAnimable? host = null)
        {
            return new Control(manager, this, host);
        }

        public void Add(IAnimation animation)
        {
            _animations.Add(animation);
            UpdateDuration();
        }

        public void Remove(IAnimation animation)
        {
            _animations.Remove(animation);
            UpdateDuration();
        }

        public void Clear()
        {
            _animations.Clear();
            UpdateDuration();
        }
        protected void UpdateDuration()
        {
            if (_animations.Count == 0)
            {
                _duration = 0;
                return;
            }

            _duration = _mode == AnimationGroupMode.Parallel
                ? _animations.Max(a => a.Delay + a.Duration)
                : _animations.Sum(a => a.Delay + a.Duration);
        }

        public AnimationGroupMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                UpdateDuration();
            }
        }

        public IReadOnlyList<IAnimation> Animations => _animations;

        public float Duration => _duration;

        public float Delay { get; set; }

        public AnimationDirection Direction { get; set; }

        public int IterationCount { get; set; }

        public string? Name { get; set; }
    }
}