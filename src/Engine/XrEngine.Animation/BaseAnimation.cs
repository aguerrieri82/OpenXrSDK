using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XrEngine.Animation
{
    public abstract class BaseAnimation<T> : IAnimation<T>
    {
        private float _startTime;
        private float _time;

        [AllowNull]
        private T _value;
        private bool _isStarted;

        public BaseAnimation()
        {
            _value = default;
            Steps = [];
        }

        public void Step(AnimationContext ctx)
        {
            var typedCtx = (AnimationContext<T>)ctx;

            var curValue = Interpolate(typedCtx.StartValue, typedCtx.EndValue, ctx.Time);

            _time = ctx.Time;

            if (!_isStarted)
            {
                _isStarted = true;
                _startTime = ctx.ReferenceTime;
            }

            if (!Equals(curValue, _value))
            {
                SetTarget?.Invoke(curValue);
                ValueChanged?.Invoke(this, curValue);
                _value = curValue;
            }
        }


        protected abstract T Interpolate(T startValue, T endValue, float t);

        public IList<AnimationStep<T>> Steps { get; set; }

        public float Duration => Steps == null || Steps.Count == 0 ? 0 : Steps[^1].Time;

        public float Delay { get; set; }
        
        public AnimationDirection Direction { get; set; }

        public int IterationCount { get; set; }

        public string? Name { get; set; }

        public Action<T>? SetTarget { get; set; }

        public float StartTime => _startTime;

        public float Time => _time;

        public T Value => _value;

        public bool IsStarted => _isStarted;

        public bool IsCompleted => _time >= 1;

        public Type ValueType => typeof(T);

        public event EventHandler<T>? ValueChanged;

    }
}
