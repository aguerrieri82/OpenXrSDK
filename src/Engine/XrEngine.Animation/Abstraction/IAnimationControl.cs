namespace XrEngine.Animation
{
    public enum AnimationState
    {
        Pending,
        Playing,
        Paused,
        Completed,
        Stopped
    }

    public interface IAnimationControl 
    {
        IAnimation Animation { get; }

        IAnimable? Host { get; }

        AnimationState State { get; }

        float Time { get; }

        float StartRefTime { get; }

        void Play();

        void Pause();

        void Stop();

        void Seek(float t);

        void Step(float referenceTime);

        event EventHandler Updated;
    }
}
