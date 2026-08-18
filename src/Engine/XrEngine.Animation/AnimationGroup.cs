namespace XrEngine.Animation
{
    public enum AnimationGroupMode
    {
        Parallel,
        Sequential
    }

    public class AnimationGroup : IAnimation
    {
        protected class State
        {
            public readonly Dictionary<IAnimation, IAnimationPlayback> Playbacks = [];

            public int Index;
            public IAnimationPlayback? Current;
        }

        protected List<IAnimation> _animations = [];
        private float _duration;
        private AnimationGroupMode _mode;

        public AnimationGroup()
        {
            _mode = AnimationGroupMode.Parallel;
        }

        public void Add(IAnimation value)
        {
            _animations.Add(value);
            Update();
        }

        public void Remove(IAnimation value)
        {
            _animations.Remove(value);
            Update();
        }

        public void Clear()
        {
            _animations.Clear();
            Update();
        }

        public bool Step(AnimationContext ctx)
        {
            if (_animations.Count == 0)
                return false;

            var state = ctx.GetState<State>();

            if (Mode == AnimationGroupMode.Sequential)
                return StepSequential(ctx, state);

            return StepParallel(ctx, state);
        }

        protected bool StepParallel(AnimationContext ctx, State state)
        {
            foreach (var anim in _animations)
            {
                if (!state.Playbacks.TryGetValue(anim, out var playback))
                {
                    playback = ctx.Controller.Start(anim, ctx.Host);
                    state.Playbacks[anim] = playback;
                }

                ctx.Controller.Step(playback);
            }

            return true;
        }

        protected bool StepSequential(AnimationContext ctx, State state)
        {
            while (state.Index < _animations.Count)
            {
                state.Current ??= ctx.Controller.Start(_animations[state.Index], ctx.Host);

                ctx.Controller.Step(state.Current);

                if (!state.Current.IsCompleted)
                    return true;

                state.Current = null;
                state.Index++;
            }

            return false;
        }

        protected virtual void Update()
        {
            _duration = Mode == AnimationGroupMode.Sequential
                ? _animations.Sum(a => a.Duration)
                : _animations.Count == 0 ? 0 : _animations.Max(a => a.Duration);
        }

        public AnimationGroupMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Update();
            }
        }

        public float Delay { get; set; }

        public AnimationDirection Direction { get; set; }

        public int IterationCount { get; set; }

        public string? Name { get; set; }

        public Type ValueType => typeof(void);

        public float Duration => _duration;

        public IReadOnlyList<IAnimation> Animations => _animations;
    }
}