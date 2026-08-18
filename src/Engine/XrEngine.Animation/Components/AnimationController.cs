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

            public IAnimable? Host;

            public AnimationContext? Context;

            IAnimation IAnimationPlayback.Animation => _animation;
            float IAnimationPlayback.Time => Time;
            float IAnimationPlayback.StartTime => StartTime;
            bool IAnimationPlayback.IsStarted => IsStarted;
            bool IAnimationPlayback.IsCompleted => IsCompleted;
            IAnimable IAnimationPlayback.Host => Host!;

            void IAnimationPlayback.Stop()
            {
                _controller.Stop(this);
            }
        }

        static readonly Dictionary<Type, MethodInfo> _processMethods = [];

        static MethodInfo? _processMethod;

        protected readonly List<AnimationState> _animations = [];

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

            _processMethod ??= GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(m =>
                    m.Name == nameof(Process) &&
                    m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters() is var p &&
                    p.Length == 2 &&
                    p[0].ParameterType.IsGenericType &&
                    p[0].ParameterType.GetGenericTypeDefinition() == typeof(IAnimation<>) &&
                    p[1].ParameterType == typeof(AnimationState));

            if (!_processMethods.TryGetValue(anim.ValueType, out var method))
            {
                method = _processMethod.MakeGenericMethod(anim.ValueType);
                _processMethods[anim.ValueType] = method;
            }

            method.Invoke(this, [anim, state]);
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

                state.Context.ReferenceTime = evaluationTime;
                state.Context.NormalizedTime = time;

                return anim.Step(state.Context);
            }

            while (state.AnimationTime > anim.Duration)
            {
                var overflow = state.AnimationTime - anim.Duration;

                Evaluate(1f, referenceTime - overflow);

                if (!AdvanceIteration(anim, state))
                    return;
            }

            Evaluate(state.AnimationTime / anim.Duration, referenceTime);

            if (state.AnimationTime == anim.Duration)
            {
                state.AnimationTime = 0;
                AdvanceIteration(anim, state);
            }
        }

        protected void Process<TValue>(IAnimation<TValue> anim, AnimationState state)
        {
            var steps = anim.Steps;

            if (steps.Count < 2)
                return;

            if (!UpdateTime(state, out var referenceTime, out var deltaTime))
                return;

            void EnterStep()
            {
                state.NextStep = state.Step + state.Direction;
                state.StepDuration = MathF.Abs(steps[state.NextStep].Time - steps[state.Step].Time);
                state.InvStepDuration = 1f / state.StepDuration;
            }

            bool Evaluate(float time, float evaluationTime)
            {
                var start = steps[state.Step];
                var end = steps[state.NextStep];

                time = start.TimeFunction?.Invoke(time, state.StepDuration) ?? time;

                state.Time = state.AnimationTime / anim.Duration;

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
                context.ReferenceTime = evaluationTime;
                context.NormalizedTime = time;

                return anim.Step(context);
            }

            bool AdvanceStep()
            {
                state.Step = state.NextStep;

                var nextStep = state.Step + state.Direction;

                if (nextStep >= 0 && nextStep < steps.Count)
                {
                    EnterStep();
                    return true;
                }

                if (!AdvanceIteration(anim, state))
                    return false;

                state.Step = state.Direction > 0 ? 0 : steps.Count - 1;
                EnterStep();

                return true;
            }

            if (state.Step < 0)
            {
                state.Step = state.Direction > 0 ? 0 : steps.Count - 1;
                EnterStep();
            }

            state.StepTime += deltaTime;

            while (state.StepTime > state.StepDuration)
            {
                var overflow = state.StepTime - state.StepDuration;

                Evaluate(1f, referenceTime - overflow);

                state.StepTime = overflow;

                if (!AdvanceStep())
                    return;
            }

            Evaluate(state.StepTime * state.InvStepDuration, referenceTime);

            if (state.StepTime == state.StepDuration)
            {
                state.StepTime = 0;
                AdvanceStep();
            }
        }

        public IAnimationPlayback Start(IAnimation animation, IAnimable? host =  null)
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
                    AnimationDirection.Alternate ? 1 : -1
            };

            _animations.Add(state);

            return state;
        }

        protected void Stop(AnimationState state)
        {
            _animations.Remove(state);
        }

        public void Stop(IAnimationPlayback playback)
        {
            Stop((AnimationState)playback);
        }

        public void StopAll()
        {
            _animations.Clear();
        }

        public IReadOnlyCollection<IAnimationPlayback> ActiveAnimations => _animations;

        public IReferenceTime Reference => _host.Scene!.App!;
    }
}