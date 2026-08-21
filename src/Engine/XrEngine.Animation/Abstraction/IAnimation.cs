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

    public struct AnimationTarget<TValue>
    {
        public TValue Value;
        public int Direction;
        public IAnimable? Host;

    }

    public interface IAnimation
    {
        IAnimationControl CreateControl(IAnimationManager manager, IAnimable? host = null);

        float Duration { get; }

        float Delay { get; set; }

        AnimationDirection Direction { get; set; }

        int IterationCount { get; set; }

        string? Name { get; set; }
    }

    public interface IAnimation<TValue> : IAnimation
    {
        IList<AnimationStep<TValue>> Steps { get; }

        Action<AnimationTarget<TValue>>? SetTarget { get; }

    }
}
