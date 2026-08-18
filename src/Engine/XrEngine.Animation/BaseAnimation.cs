using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XrEngine.Animation
{
    public abstract class BaseAnimation<TValue> : IAnimation<TValue>
    {

        [AllowNull]
        private TValue _lastValue;

        public BaseAnimation()
        {
            _lastValue = default;
            Steps = [];
        }

        public bool Step(AnimationContext ctx)
        {
            var typedCtx = (AnimationContext<TValue>)ctx;

            var curValue = Interpolate(typedCtx.StartValue, typedCtx.EndValue, ctx.NormalizedTime);

            if (!Equals(curValue, _lastValue))
            {
                SetTarget?.Invoke(curValue, ctx);
                ValueChanged?.Invoke(this, curValue);
                _lastValue = curValue;
            }

            return true;
        }


        protected abstract TValue Interpolate(TValue startValue, TValue endValue, float t);

        public IList<AnimationStep<TValue>> Steps { get; set; }

        public float Duration => Steps == null || Steps.Count == 0 ? 0 : Steps[^1].Time;

        public float Delay { get; set; }
        
        public AnimationDirection Direction { get; set; }

        public int IterationCount { get; set; }

        public string? Name { get; set; }

        public Action<TValue, AnimationContext> SetTarget { get; set; }

        public Type ValueType => typeof(TValue);

        public event EventHandler<TValue>? ValueChanged;

    }
}
