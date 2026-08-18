using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace XrEngine.Animation
{

    public class AnimationContext
    {
        private object? _state;

        public TState GetState<TState>() where TState : class, new()
        {
            return (TState)(_state ??= new TState());
        }

        public float Time;

        public float RefTime;

        public bool IsNewIteration;

        public IAnimable? Host;

        [AllowNull]
        public AnimationController Controller;
    }

    public class AnimationContext<T> : AnimationContext
    {
        [AllowNull]
        public T StartValue;

        [AllowNull]
        public T EndValue;
    }
}
