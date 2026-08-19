using System;
using System.Numerics;

namespace XrEngine.Animation
{
    public delegate T ComputeFunctionDelegate<T>(float t);

    public struct ComputeFunction<T>
    {
        public float Duration;

        public ComputeFunctionDelegate<T> GetValue;
    }


    public static class ComputeFunctions
    {
        public static ComputeFunction<Vector3> Sin(
            Vector3 axis,
            float amplitude = 1f,
            float frequency = 1f,
            float phase = 0f,
            Vector3 offset = default,
            float? duration = null)
        {
            axis = Vector3.Normalize(axis);

            return new ComputeFunction<Vector3>
            {
                Duration = duration ?? 1f / MathF.Abs(frequency),
                GetValue = t =>
                    offset + axis * (amplitude * MathF.Sin(MathF.Tau * frequency * t + phase))
            };
        }

        public static ComputeFunction<Vector3> Jump(
            float baseY,
            Vector3 direction,
            float intensity = 1f,
            float gravity = 9.81f)
        {
            return JumpImpulse(baseY, Vector3.Normalize(direction) * intensity, gravity);
        }

        public static ComputeFunction<Vector3> JumpImpulse(
            float baseY,
            Vector3 impulse,
            float gravity = 9.81f)
        {
            var duration = 2f * impulse.Y / gravity;
            var halfGravity = gravity * 0.5f;

            return new ComputeFunction<Vector3>
            {
                Duration = duration,
                GetValue = t => new Vector3(
                    impulse.X * t,
                    baseY + impulse.Y * t - halfGravity * t * t,
                    impulse.Z * t)
            };
        }
    }


    public static class ComputedAnimation
    {
        public static ComputedAnimation<T> Create<T>(
            ComputeFunctionDelegate<T> getValue,
            float duration = 1)
        {
            return new ComputedAnimation<T>(new ComputeFunction<T>
            {
                Duration = duration,
                GetValue = getValue
            });
        }

        public static ComputedAnimation<TValue> Create<TValue>(
            ComputeFunction<TValue> compute,
            string? name = null,
            Action<AnimationTarget<TValue>>? setTarget = null,
            Func<IAnimable?, TValue>? getTarget = null)
        {
            return new ComputedAnimation<TValue>(compute)
            {
                Name = name,
                IsRelative = getTarget != null,
                SetTarget = setTarget,
                GetTarget = getTarget
            };
        }
    }


    public class ComputedAnimation<TValue> : BaseValueAnimation<TValue>
    {
        #region Control

        protected class Control : BaseAnimationControl<ComputedAnimation<TValue>>
        {
            protected TValue _initialValue = default!;
            protected bool _hasInitialValue;

            public Control(IAnimationManager manager, ComputedAnimation<TValue> animation, IAnimable? host)
                : base(manager, animation, host)
            {
            }


            protected override bool Evaluate(float time, float referenceTime)
            {
                var value = _animation._compute.GetValue(time);

                if (_animation.IsRelative)
                {
                    EnsureInitialValue();
                    value = _animation._valueHandler.Combine(_initialValue, value);
                }

                return _animation.ApplyValue(new AnimationTarget<TValue>
                {
                    Value = value,
                    Host = _host,
                    Direction = _direction
                });
            }


            protected override void OnReset()
            {
                _hasInitialValue = false;
                EnsureInitialValue();
            }


            protected override void OnIterationChanged()
            {
                if (!_animation.IsRelative)
                    return;

                if (_animation.Direction == AnimationDirection.Alternate && _direction < 0)
                    return;

                if (_animation.Direction == AnimationDirection.AlternateReverse && _direction > 0)
                    return;

                _hasInitialValue = false;
                EnsureInitialValue();
            }


            protected override void OnSeek()
            {
                EnsureInitialValue();
            }


            protected void EnsureInitialValue()
            {
                if (_hasInitialValue || !_animation.IsRelative)
                    return;

                if (_animation.GetTarget == null)
                    throw new InvalidOperationException(
                        "GetTarget is required for relative computed animations.");

                var currentValue = _animation.GetTarget(_host);

                var startValue = _animation._compute.GetValue(
                    _direction > 0 ? 0 : _duration);

                _initialValue = _animation._valueHandler.Remove(
                    currentValue,
                    startValue);

                _hasInitialValue = true;
            }
        }

        #endregion

        protected ComputeFunction<TValue> _compute;

        public ComputedAnimation(ComputeFunction<TValue> compute)
        {
            _compute = compute;
        }

        public override IAnimationControl CreateControl(
            IAnimationManager manager,
            IAnimable? host = null)
        {
            return new Control(manager, this, host);
        }

        protected virtual bool ApplyValue(AnimationTarget<TValue> target)
        {
            _setTarget?.Invoke(target);
            return true;
        }

        public Func<IAnimable?, TValue>? GetTarget { get; set; }

        public bool IsRelative { get; set; }

        public override float Duration => _compute.Duration;
    }
}