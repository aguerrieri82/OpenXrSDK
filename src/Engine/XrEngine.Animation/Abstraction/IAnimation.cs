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
        bool IsStarted { get; }

        bool IsCompleted { get; }

        float Duration { get; }

        float Delay { get; set; }

        AnimationDirection Direction { get; set; }

        int IterationCount { get; set; }

        void Step(AnimationContext ctx);

        float StartTime { get; }

        float Time { get; }

        string? Name { get; }

        Type ValueType { get; }



    }


    public interface IAnimation<T> : IAnimation
    {
        IList<AnimationStep<T>> Steps { get; }

        T Value { get; }

        Action<T>? SetTarget { get; }

        event EventHandler<T>? ValueChanged;
    }
}
