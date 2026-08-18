using System.Reflection;

namespace XrEngine.Animation
{
    public class AnimationController : Behavior<Scene3D>, IAnimationController
    {
        protected class AnimationState : IAnimationPlayback
        {
            protected readonly AnimationController _controller;
            protected internal readonly IAnimation _animation;

            public AnimationState(AnimationController controller, IAnimation animation)
            {
                _controller = controller;
                _animation = animation;
            }

            public float StartTime;
            public float ReferenceTime;
            public float Time;

            public float AnimationTime;
            public float StepTime;
            public float StepDuration;
            public float InvStepDuration;

            public int Iteration;
            public int Step;
            public int NextStep;
            public int Direction;

            public bool IsStarted;
            public bool IsCompleted;
            public bool IsSeeking;
            public bool IsNewIteration;

            public IAnimable? Host;

            public AnimationContext? Context;

            IAnimation IAnimationPlayback.Animation => _animation;
            float IAnimationPlayback.Time => Time;
            float IAnimationPlayback.StartRefTime => StartTime;
            bool IAnimationPlayback.IsStarted => IsStarted;
            bool IAnimationPlayback.IsCompleted => IsCompleted;
            IAnimable IAnimationPlayback.Host => Host!;

            void IAnimationPlayback.Stop()
            {
                _controller.Stop(this);
            }

            void IAnimationPlayback.Seek(float t)
            {
                _controller.Seek(this, t);
            }
        }

        delegate void ProcessInvoker(AnimationController controller, IAnimation anim, AnimationState state);

        static readonly Dictionary<Type, ProcessInvoker> _processMethods = [];

        static MethodInfo? _createMethod;

        protected readonly List<AnimationState> _animations = [];

        static ProcessInvoker CreateInvoker<TValue>()
        {
            return static (controller, anim, state) =>
                controller.Process((IAnimation<TValue>)anim, state);
        }

        protected override void Update(RenderContext ctx)
        {
            for (var i = _animations.Count - 1; i >= 0; i--)
                Step(_animations[i]);
        }

        public void Step(IAnimationPlayback playback)
        {
            Step((AnimationState)playback);
        }

        protected void Step(AnimationState state)
        {
            var anim = state._animation;

            if (anim.ValueType == typeof(void))
            {
                Process(anim, state);
                return;
            }

            _createMethod ??= GetType()
                .GetMethod(nameof(CreateInvoker), BindingFlags.NonPublic | BindingFlags.Static);

            if (!_processMethods.TryGetValue(anim.ValueType, out var process))
            {
                var craeteGenMethod = _createMethod!.MakeGenericMethod(anim.ValueType);
                process = (ProcessInvoker)craeteGenMethod.Invoke(null, [])!;
                _processMethods[anim.ValueType] = process;
            }

            process!(this, anim, state);
        }

        protected bool UpdateTime(AnimationState state, out float referenceTime, out float deltaTime)
        {
            referenceTime = (float)Reference.Time;

            if (referenceTime < state.StartTime)
            {
                deltaTime = 0;
                return false;
            }

            deltaTime = referenceTime - state.ReferenceTime;

            state.ReferenceTime = referenceTime;
            state.AnimationTime += deltaTime;
            state.IsStarted = true;

            return true;
        }

        protected bool AdvanceIteration(IAnimation anim, AnimationState state)
        {
            state.Iteration++;

            if (anim.IterationCount > 0 && state.Iteration >= anim.IterationCount)
            {
                state.IsCompleted = true;
                Stop(state);
                return false;
            }

            state.AnimationTime -= anim.Duration;

            if (anim.Direction is AnimationDirection.Alternate or AnimationDirection.AlternateReverse)
                state.Direction = -state.Direction;

            state.IsNewIteration = true;

            return true;
        }

        protected void Process(IAnimation anim, AnimationState state)
        {
            if (!UpdateTime(state, out var referenceTime, out _))
                return;

            bool Evaluate(float time, float evaluationTime)
            {
                if (state.Direction < 0)
                    time = 1f - time;

                state.Time = time;

                state.Context ??= new AnimationContext
                {
                    Controller = this,
                    Host = state.Host
                };

                state.Context.RefTime = evaluationTime;
                state.Context.Time = time;
                state.Context.IsNewIteration = state.IsNewIteration;

                state.IsNewIteration = false;

                return anim.Step(state.Context);
            }

            while (state.AnimationTime > anim.Duration)
            {
                var overflow = state.AnimationTime - anim.Duration;

                if (!state.IsSeeking && !Evaluate(1f, referenceTime - overflow))
                {
                    Stop(state);
                    return;
                }

                if (!AdvanceIteration(anim, state))
                    return;
            }

            if (!Evaluate(state.AnimationTime / anim.Duration, referenceTime))
            {
                Stop(state);
                return;
            }

            if (state.AnimationTime == anim.Duration && !state.IsSeeking)
                AdvanceIteration(anim, state);
        }

        protected void Process<TValue>(IAnimation<TValue> anim, AnimationState state)
        {
            var steps = anim.Steps;

            if (steps.Count < 2)
                return;

            if (!UpdateTime(state, out var referenceTime, out _))
                return;

            bool Evaluate(float animationTime, float evaluationTime)
            {
                var time = state.Direction > 0
                    ? animationTime
                    : anim.Duration - animationTime;

                var step = state.Step;

                if (step < 0)
                    step = 0;

                while (step > 0 && time < steps[step].Time)
                    step--;

                while (step < steps.Count - 2 && time > steps[step + 1].Time)
                    step++;

                state.Step = step;
                state.NextStep = step + 1;

                var start = steps[state.Step];
                var end = steps[state.NextStep];

                state.StepDuration = end.Time - start.Time;
                state.InvStepDuration = 1f / state.StepDuration;
                state.StepTime = Math.Clamp(time - start.Time, 0, state.StepDuration);

                var normalizedTime = state.StepTime * state.InvStepDuration;

                normalizedTime = start.TimeFunction?.Invoke(normalizedTime, state.StepDuration) ?? normalizedTime;

                state.Time = anim.Duration > 0
                    ? time / anim.Duration
                    : 0;

                if (state.Context is not AnimationContext<TValue> context)
                {
                    context = new AnimationContext<TValue>
                    {
                        Controller = this,
                        Host = state.Host
                    };
                    state.Context = context;
                }

                context.StartValue = start.Value;
                context.EndValue = end.Value;
                context.RefTime = evaluationTime;
                context.Time = normalizedTime;
                context.IsNewIteration = state.IsNewIteration;

                state.IsNewIteration = false;

                return anim.Step(context);
            }

            while (state.AnimationTime > anim.Duration)
            {
                var overflow = state.AnimationTime - anim.Duration;

                if (!state.IsSeeking && !Evaluate(anim.Duration, referenceTime - overflow))
                {
                    Stop(state);
                    return;
                }

                if (!AdvanceIteration(anim, state))
                    return;

                state.Step = -1;
            }

            if (!Evaluate(state.AnimationTime, referenceTime))
            {
                Stop(state);
                return;
            }

            if (state.AnimationTime == anim.Duration && !state.IsSeeking)
            {
                if (AdvanceIteration(anim, state))
                    state.Step = -1;
            }
        }

        public IAnimationPlayback Start(IAnimation animation, IAnimable? host = null)
        {
            var state = new AnimationState(this, animation)
            {
                StartTime = (float)Reference.Time + animation.Delay,
                ReferenceTime = (float)Reference.Time + animation.Delay,

                AnimationTime = 0,
                StepTime = 0,

                Iteration = 0,
                Step = -1,

                Host = host,

                Direction = animation.Direction is
                    AnimationDirection.Forward or
                    AnimationDirection.Alternate ? 1 : -1,

                IsNewIteration = true
            };

            _animations.Add(state);

            return state;
        }

        public void Stop(IAnimationPlayback playback)
        {
            var state = (AnimationState)playback;

            if (state.IsStarted)
                state._animation.Reset(state.Context!);

            _animations.Remove(state);
        }

        public void Seek(IAnimationPlayback playback, float t)
        {
            var state = (AnimationState)playback;
            var anim = state._animation;
            var referenceTime = (float)Reference.Time;

            t = Math.Clamp(t, 0f, 1f);

            var animationTime = t * anim.Duration;

            state.StartTime = referenceTime;
            state.ReferenceTime = referenceTime;

            state.Time = t;
            state.AnimationTime = animationTime;
            state.StepTime = animationTime;

            state.Iteration = 0;
            state.Step = -1;
            state.NextStep = -1;

            state.Direction = anim.Direction is
                AnimationDirection.Forward or
                AnimationDirection.Alternate ? 1 : -1;

            state.IsStarted = true;
            state.IsCompleted = false;
            state.IsSeeking = true;
            state.IsNewIteration = true;

            Step(state);

            state.IsSeeking = false;
        }

        [Action]
        public void StopAll()
        {
            _animations.Clear();
        }

        public IReadOnlyCollection<IAnimationPlayback> ActiveAnimations => _animations;

        public IReferenceTime Reference => _host.Scene!.App!;
    }
}