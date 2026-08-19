namespace XrEngine.Animation
{
    public abstract class BaseAnimationControl<TAnim> : IAnimationControl
        where TAnim : IAnimation
    {
        protected readonly IAnimationManager _manager;
        protected readonly TAnim _animation;
        protected readonly IAnimable? _host;

        protected AnimationState _state;
        protected AnimationState _pausedState;

        protected float _startRefTime;
        protected float _referenceTime;
        protected float _pauseRefTime;

        protected float _animationTime;
        protected float _duration;
        protected float _invDuration;

        protected int _iteration;
        protected int _direction;
        private float _time;

        private EventHandler? _updated;

        protected BaseAnimationControl(IAnimationManager manager, TAnim animation, IAnimable? host)
        {
            _manager = manager;
            _animation = animation;
            _host = host;

            _state = AnimationState.Stopped;
            _direction = GetInitialDirection();

            UpdateDuration();
        }


        public virtual void Play()
        {
            var referenceTime = (float)_manager.Reference.Time;

            if (_state == AnimationState.Paused)
            {
                var pauseDuration = referenceTime - _pauseRefTime;

                _startRefTime += pauseDuration;
                _referenceTime += pauseDuration;

                SetState(_pausedState);
                return;
            }

            if (_state is AnimationState.Playing or AnimationState.Pending)
                return;

            UpdateDuration();

            _animationTime = 0;
            _iteration = 0;
            _direction = GetInitialDirection();

            _startRefTime = referenceTime + _animation.Delay;
            _referenceTime = _startRefTime;

            OnReset();

            SetState(_animation.Delay > 0
                ? AnimationState.Pending
                : AnimationState.Playing);
        }


        public virtual void Pause()
        {
            if (_state is not (AnimationState.Playing or AnimationState.Pending))
                return;

            _pauseRefTime = (float)_manager.Reference.Time;
            _pausedState = _state;

            SetState(AnimationState.Paused);
        }


        public virtual void Stop()
        {
            if (_state == AnimationState.Stopped)
                return;

            SetState(AnimationState.Stopped);

            _manager.Remove(this);
        }


        public virtual void Seek(float time)
        {
            time = Math.Clamp(time, 0f, 1f);

            UpdateDuration();

            _animationTime = time * _duration;

            var referenceTime = (float)_manager.Reference.Time;

            _referenceTime = referenceTime;

            OnSeek();

            if (!EvaluateCurrent(referenceTime))
                Stop();
        }

        public void Step(float referenceTime)
        {
            if (_state == AnimationState.Pending)
            {
                if (referenceTime < _startRefTime)
                    return;

                _referenceTime = _startRefTime;
                SetState(AnimationState.Playing);
            }

            if (_state != AnimationState.Playing)
                return;

            var deltaTime = referenceTime - _referenceTime;

            _referenceTime = referenceTime;
            _animationTime += deltaTime;

            if (_duration <= 0)
            {
                if (Evaluate(0, referenceTime))
                    Complete();
                else
                    Stop();

                return;
            }

            while (_animationTime > _duration)
            {
                var overflow = _animationTime - _duration;

                _animationTime = _duration;

                if (!EvaluateCurrent(referenceTime - overflow))
                {
                    Stop();
                    return;
                }

                if (!AdvanceIteration())
                    return;

                _animationTime = overflow;
            }

            if (!EvaluateCurrent(referenceTime))
            {
                Stop();
                return;
            }

            if (_animationTime == _duration)
            {
                _animationTime = 0;
                AdvanceIteration();
            }
        }

        protected bool AdvanceIteration()
        {
            _iteration++;

            if (_animation.IterationCount > 0 &&
                _iteration >= _animation.IterationCount)
            {
                Complete();
                return false;
            }

            if (_animation.Direction is AnimationDirection.Alternate or AnimationDirection.AlternateReverse)
                _direction = -_direction;

            OnIterationChanged();

            return true;
        }

        protected bool EvaluateCurrent(float referenceTime)
        {
            var animationTime = _direction > 0
                ? _animationTime
                : _duration - _animationTime;

            _time = _duration > 0
                ? animationTime * _invDuration
                : 0;

            var result = Evaluate(animationTime, referenceTime);

            OnUpdated();

            return result;
        }

        protected void Complete()
        {
            SetState(AnimationState.Completed);
            _manager.Remove(this);
        }

        protected void SetState(AnimationState state)
        {
            if (_state == state)
                return;

            _state = state;
            OnStateChanged(state);
            OnUpdated();
        }

        protected void UpdateDuration()
        {
            _duration = _animation.Duration;
            _invDuration = _duration > 0 ? 1f / _duration : 0;
        }

        protected int GetInitialDirection()
        {
            return _animation.Direction is AnimationDirection.Forward or AnimationDirection.Alternate
                ? 1
                : -1;
        }

        protected abstract bool Evaluate(float time, float referenceTime);

        protected virtual void OnReset()
        {
        }

        protected virtual void OnSeek()
        {
        }

        protected virtual void OnIterationChanged()
        {
        }

        protected virtual void OnStateChanged(AnimationState state)
        {
        }

        protected void OnUpdated()
        {
            _updated?.Invoke(this, EventArgs.Empty);
        }

        IAnimation IAnimationControl.Animation => _animation;

        public TAnim Animation => _animation;

        public IAnimable? Host => _host;

        public AnimationState State => _state;

        public float Time => _time;

        public float StartRefTime => _startRefTime;

        public event EventHandler Updated
        {
            add
            {
                _updated -= value;
                _updated += value;
            }
            remove
            {
                _updated -= value;
            }
        }
    }
}