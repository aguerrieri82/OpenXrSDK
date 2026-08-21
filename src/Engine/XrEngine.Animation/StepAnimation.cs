using System.Runtime.CompilerServices;

namespace XrEngine.Animation
{
    public class StepAnimation<TValue> : BaseValueAnimation<TValue>
    {

        #region Control

        protected class Control : BaseAnimationControl<StepAnimation<TValue>>
        {
            protected int _step = -1;
            protected int _nextStep = -1;
            protected float _stepDuration;
            protected float _invStepDuration;

            public Control(IAnimationManager manager, StepAnimation<TValue> animation, IAnimable? host)
                : base(manager, animation, host)
            {

            }

            protected override bool Evaluate(float time, float referenceTime)
            {
                var steps = _animation._steps;

                if (steps.Count < 2)
                    return true;

                if (_step < 0)
                {
                    SetStep(_direction > 0
                        ? 0
                        : steps.Count - 2);
                }

                while (_step > 0 && time < steps[_step].Time)
                    SetStep(_step - 1);

                while (_step < steps.Count - 2 && time > steps[_nextStep].Time)
                    SetStep(_step + 1);

                var start = steps[_step];
                var end = steps[_nextStep];

                var stepTime = Math.Clamp(time - start.Time, 0, _stepDuration);
                var normalizedTime = stepTime * _invStepDuration;

                normalizedTime = start.TimeFunction?.Invoke(
                    normalizedTime,
                    _stepDuration) ?? normalizedTime;

                var value = _animation.Interpolate(
                    start.Value,
                    end.Value,
                    normalizedTime);

                return _animation.ApplyValue(value, _host!);
            }

            protected void SetStep(int step)
            {
                _step = step;
                _nextStep = step + 1;

                var steps = _animation._steps;

                _stepDuration = steps[_nextStep].Time - steps[_step].Time;
                _invStepDuration = 1f / _stepDuration;
            }

            protected override void OnReset()
            {
                _step = -1;
                _nextStep = -1;
            }

            protected override void OnSeek()
            {
                _step = -1;
                _nextStep = -1;
            }

            protected override void OnIterationChanged()
            {
                _step = -1;
                _nextStep = -1;
            }
        }

        #endregion

        protected IList<AnimationStep<TValue>> _steps = [];

        public StepAnimation(IAnimationValueHandler<TValue>? valueHandler = null)
            : base(valueHandler)
        {
        }

        public override IAnimationControl CreateControl(IAnimationManager manager, IAnimable? host = null)
        {
            return new Control(manager, this, host);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool ApplyValue(TValue value, IAnimable host)
        {
            _setTarget?.Invoke(new AnimationTarget<TValue>
            {
                Value = value,
                Host = host
            });

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual TValue Interpolate(TValue start, TValue end, float time)
        {
            return _valueHandler.Interpolate(start, end, time);
        }

        public IList<AnimationStep<TValue>> Steps
        {
            get => _steps;
            set => _steps = value;
        }

        public override float Duration => _steps.Count > 0
            ? _steps[_steps.Count - 1].Time
            : 0;

    }
}