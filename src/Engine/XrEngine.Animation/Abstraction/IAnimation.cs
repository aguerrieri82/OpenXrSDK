using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.Animation
{
    public delegate float TimeFunctionDelegate(float t, float duration);

    public struct AnimationStep<T>
    {
        public T Value;

        public float Time;

        public TimeFunctionDelegate? TimeFunction;
    }

    public enum AnimationDirection
    {
        Forward,
        Backward,
        Alternate,
        AlternateReverse
    }


    public interface IAnimation
    {
        float Duration { get; }

        float Delay { get; set; }

        AnimationDirection Direction { get; set; }

        int IterationCount { get; set; }

        bool Step(AnimationContext ctx);

        string? Name { get; }

        Type ValueType { get; }
    }


    public interface IAnimation<TValue> : IAnimation
    {
        IList<AnimationStep<TValue>> Steps { get; }

        Action<TValue, AnimationContext>? SetTarget { get; }

        event EventHandler<TValue>? ValueChanged;
    }
}
