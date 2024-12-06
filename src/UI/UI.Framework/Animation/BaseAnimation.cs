namespace CanvasUI
{
    public abstract class BaseAnimation<T> : IAnimation where T : struct
    {
        public void Start(T from, T to, float durationMs)
        {
            Start(from, to, TimeSpan.FromMilliseconds(durationMs));
        }

        public void Start(T from, T to, TimeSpan duration)
        {
            if (IsStarted)
                Value = to;

            Duration = duration;
            From = from;
            To = to;
            Value = from;

            AnimationManager.Instance.Start(this);
        }

        public void Stop()
        {
            IsStarted = false;
        }

        public void Step(float t)
        {
            Value = Interpolate(From, To, t);

            ValueChanged?.Invoke(this, Value);
        }

        protected abstract T Interpolate(T from, T to, float t);

        public bool IsStarted { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan Duration { get; set; }

        public T From { get; set; }

        public T To { get; set; }

        public T Value { get; set; }


        public event EventHandler<T>? ValueChanged;
    }
}
