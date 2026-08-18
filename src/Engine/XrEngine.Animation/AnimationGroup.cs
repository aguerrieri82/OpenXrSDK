namespace XrEngine.Animation
{
    public enum AnimationGroupMode
    {
        Parallel,
        Sequential
    }


    public class AnimationGroup : IAnimation
    {
        protected readonly List<IAnimation> _animations = [];

        protected AnimationGroupMode _mode;
        protected float _duration;


        protected class Playback : BaseAnimationPlayback<AnimationGroup>
        {
            protected readonly List<IAnimationPlayback> _playbacks = [];

            protected int _current;


            public Playback(
                IAnimationController controller,
                AnimationGroup animation,
                IAnimable? host)
                : base(controller, animation, host)
            {
                foreach (var child in animation._animations)
                    _playbacks.Add(child.CreatePlayback(controller, host));
            }


            protected override bool Evaluate(float time, float referenceTime)
            {
                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _playbacks)
                        playback.Step(referenceTime);
                }
                else if (_current < _playbacks.Count)
                {
                    var playback = _playbacks[_current];

                    playback.Step(referenceTime);

                    if (playback.State == AnimationPlaybackState.Completed)
                    {
                        _current++;

                        if (_current < _playbacks.Count)
                            _playbacks[_current].Play();
                    }
                }

                return true;
            }


            protected override void OnReset()
            {
                _current = 0;

                foreach (var playback in _playbacks)
                    playback.Stop();

                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _playbacks)
                        playback.Play();
                }
                else if (_playbacks.Count > 0)
                {
                    _playbacks[0].Play();
                }
            }


            protected override void OnIterationChanged()
            {
                OnReset();
            }


            protected override void OnStateChanged(AnimationPlaybackState state)
            {
                switch (state)
                {
                    case AnimationPlaybackState.Paused:
                        foreach (var playback in _playbacks)
                            playback.Pause();
                        break;

                    case AnimationPlaybackState.Playing:
                        foreach (var playback in _playbacks)
                        {
                            if (playback.State == AnimationPlaybackState.Paused)
                                playback.Play();
                        }
                        break;

                    case AnimationPlaybackState.Stopped:
                    case AnimationPlaybackState.Completed:
                        foreach (var playback in _playbacks)
                            playback.Stop();
                        break;
                }
            }


            protected override void OnSeek()
            {
                if (_animation._mode == AnimationGroupMode.Parallel)
                {
                    foreach (var playback in _playbacks)
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

                for (var i = 0; i < _playbacks.Count; i++)
                {
                    var playback = _playbacks[i];
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

                        if (_state == AnimationPlaybackState.Playing)
                            playback.Play();
                        else
                            playback.Pause();
                    }

                    offset = end;
                }
            }


            protected static void Seek(IAnimationPlayback playback, float time)
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


        public IAnimationPlayback CreatePlayback(
            IAnimationController controller,
            IAnimable? host = null)
        {
            return new Playback(controller, this, host);
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