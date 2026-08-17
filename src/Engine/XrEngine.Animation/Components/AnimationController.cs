using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace XrEngine.Animation
{
    public class AnimationController : Behavior<Scene3D>, IAnimationController
    {
        protected class AnimationState
        {
            public float StartTime;
            public float ReferenceTime;

            public float AnimationTime;
            public float StepTime;
            public float StepDuration;
            public float InvStepDuration;

            public int Iteration;
            public int Step;
            public int NextStep;
            public int Direction;
        }


        static Dictionary<Type, MethodInfo> _processMethods = [];

        protected Dictionary<IAnimation, AnimationState> _animations = [];
        private MethodInfo? _processMethod;


        protected override void Update(RenderContext ctx)
        {
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

            foreach (var entry in _animations)
            {
                if (!_processMethods.TryGetValue(entry.Key.ValueType, out var method))
                {
                    method = _processMethod!.MakeGenericMethod(entry.Key.ValueType);
                    _processMethods[entry.Key.ValueType] = method;
                }
                method.Invoke(this, [entry.Key, entry.Value]);
            }
        }
        protected void Process<TValue>(IAnimation<TValue> anim, AnimationState state)
        {
            var referenceTime = (float)Reference.Time;

            if (referenceTime < state.StartTime)
                return;

            var steps = anim.Steps;

            if (steps.Count < 2)
                return;

            void EnterStep()
            {
                state.NextStep = state.Step + state.Direction;
                state.StepDuration = MathF.Abs(steps[state.NextStep].Time - steps[state.Step].Time) ;
                state.InvStepDuration = 1f / state.StepDuration;
            }

            void Evaluate(float time, float evaluationTime)
            {

                var start = steps[state.Step];
                var end = steps[state.NextStep];

                Log.Debug(this, "[{3}] {0} - {1} - {2}", (int)(time * 1000), state.Step, (int)(state.AnimationTime * 1000), anim.GetHashCode());

                time = start.TimeFunction?.Invoke(time, state.StepDuration) ?? time;

                anim.Step(new AnimationContext<TValue>
                {
                    StartValue = start.Value,
                    EndValue = end.Value,
                    ReferenceTime = evaluationTime,
                    Time = time
                });
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

                state.Iteration++;

                if (anim.IterationCount > 0 && state.Iteration >= anim.IterationCount)
                {
                    Stop(anim);
                    return false;
                }

                state.AnimationTime -= anim.Duration;

                if (anim.Direction is AnimationDirection.Alternate or AnimationDirection.AlternateReverse)
                    state.Direction = -state.Direction;

                state.Step = state.Direction > 0 ? 0 : steps.Count - 1;

                EnterStep();

                return true;
            }

            if (state.Step < 0)
            {
                state.Step = state.Direction > 0 ? 0 : steps.Count - 1;
                EnterStep();
            }

            var deltaTime = referenceTime - state.ReferenceTime;

            state.ReferenceTime = referenceTime;
            state.AnimationTime += deltaTime;
            state.StepTime += deltaTime;

            while (state.StepTime > state.StepDuration)
            {
                var overflow = state.StepTime - state.StepDuration;
                var stepEndReferenceTime = referenceTime - overflow;

                Evaluate(1f, stepEndReferenceTime);

                state.StepTime = overflow;

                if (!AdvanceStep())
                    return;
            }

            var normalizedTime = state.StepTime * state.InvStepDuration;

            Evaluate(normalizedTime, referenceTime);

            if (state.StepTime == state.StepDuration)
            {
                state.StepTime = 0;

                AdvanceStep();
            }
        }

        public void Start(IAnimation animation)
        {
            if (!_animations.TryGetValue(animation, out var state))
            {
                state = new AnimationState();
                _animations[animation] = state;
            }

            state.StartTime = (float)Reference.Time + animation.Delay;
            state.ReferenceTime = state.StartTime;

            state.AnimationTime = 0;
            state.StepTime = 0;

            state.Iteration = 0;
            state.Step = -1;

            state.Direction = animation.Direction is
                AnimationDirection.Forward or
                AnimationDirection.Alternate ? 1 : -1;
        }


        public void Stop(IAnimation animation)
        {
            _animations.Remove(animation);
        }

        public void StopAll()
        {
            _animations.Clear();
        }

        public IReadOnlyCollection<IAnimation> Animations => _animations.Keys;

        public IReferenceTime Reference => _host.Scene!.App!;
    }
}
