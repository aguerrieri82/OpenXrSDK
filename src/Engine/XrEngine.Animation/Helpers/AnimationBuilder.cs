using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace XrEngine.Animation
{
    public class AnimationGroupBuilder<THost> where THost : Object3D
    {
        readonly AnimationBuilder<THost> _parent;
        readonly AnimationGroup _group;

        public AnimationGroupBuilder(AnimationBuilder<THost> parent)
        {
            _parent = parent;
            _group = new AnimationGroup();
        }

        public AnimationGroupBuilder<THost> Add(Action<AnimationBuilder<THost>> build)
        {
            var builder = new AnimationBuilder<THost>(_parent._host);
            build(builder);
            _group.Add(builder.Build());
            return this;
        }

        public AnimationBuilder<THost> End()
        {
            _parent._animation = _group;
            return _parent;
        }
    }

    public class AnimationBuilder<THost, TValue> where THost : Object3D
    {
        float _time;
        bool _isRelative;
        readonly AnimationBuilder<THost> _parent;
        List<AnimationStep<TValue>>? _steps;

        readonly Func<THost, TValue> _getTargetDelegate;
        readonly Action<AnimationTarget<TValue>> _setTargetDelegate;

        public AnimationBuilder(AnimationBuilder<THost> parent, Expression<Func<THost, TValue>> target)
        {
            _parent = parent;

            _getTargetDelegate = target.Compile();
            _setTargetDelegate = CreateSetter(target);
        }

        static Action<AnimationTarget<TValue>> CreateSetter(Expression<Func<THost, TValue>> target)
        {
            var animationTarget = Expression.Parameter(typeof(AnimationTarget<TValue>), "target");

            var host = Expression.Convert(
                Expression.Field(animationTarget, nameof(AnimationTarget<>.Host)),
                typeof(THost));

            var targetMember = new ReplaceExpressionVisitor(target.Parameters[0], host)
                .Visit(target.Body)!;

            var value = Expression.Field(animationTarget, nameof(AnimationTarget<>.Value));

            var assign = Expression.Assign(targetMember, value);

            return Expression.Lambda<Action<AnimationTarget<TValue>>>(assign, animationTarget)
                  .Compile();
        }

        sealed class ReplaceExpressionVisitor : ExpressionVisitor
        {
            readonly Expression _source;
            readonly Expression _target;

            public ReplaceExpressionVisitor(Expression source, Expression target)
            {
                _source = source;
                _target = target;
            }

            public override Expression? Visit(Expression? node)
            {
                return node == _source ? _target : base.Visit(node);
            }
        }

        public AnimationBuilder<THost, TValue> Relative()
        {
            if (_steps != null)
                throw new InvalidOperationException("Relative mode must be specified before defining any keyframes.");

            _isRelative = true;
            return this;
        }

        public AnimationBuilder<THost> FromFunction(IComputeFunction<TValue> compute)
        {
            if (_steps != null)
                throw new InvalidOperationException("A computed animation cannot be defined after keyframes have been added.");

            var getDelegate = _getTargetDelegate;

            _parent._animation = new ComputedAnimation<TValue>(compute)
            {
                IsRelative = _isRelative,

                GetTarget = _isRelative
                    ? host => getDelegate((THost)host!)
                    : null,

                SetTarget = _setTargetDelegate
            };

            return _parent;
        }

        [MemberNotNull(nameof(_steps))]
        public AnimationBuilder<THost, TValue> From(TValue value)
        {
            if (_steps != null)
                throw new InvalidOperationException("The initial keyframe has already been defined. From() can only be called once.");

            _steps = [];

            _steps.Add(new AnimationStep<TValue>
            {
                Value = value,
                Time = 0,
                TimeFunction = TimeFunctions.Step
            });

            _time = 0;

            return this;
        }

        void CreateStepAnimation()
        {
            _parent._animation = new StepAnimation<TValue>
            {
                Steps = _steps?.ToArray() ?? [],
                SetTarget = _setTargetDelegate
            };
        }

        public AnimationBuilder<THost, TValue> To(TValue value, float duration, TimeFunctionDelegate timeFunction)
        {
            if (_steps == null)
            {
                TValue fromValue;

                if (_isRelative || _parent._host == null)
                {
                    var handler = Context.Require<IAnimationValueHandler<TValue>>();
                    fromValue = handler.Identity;
                }
                else
                    fromValue = _getTargetDelegate(_parent._host);

                From(fromValue);
            }

            _time += duration;

            _steps.Add(new AnimationStep<TValue>
            {
                Value = value,
                Time = _time,
                TimeFunction = timeFunction
            });

            return this;
        }

        public AnimationBuilder<THost, TValue> Delay(float duration)
        {
            return ToStep(_steps == null ? default! : _steps[^1].Value, duration);
        }

        public AnimationBuilder<THost, TValue> ToLinear(TValue value, float duration)
        {
            return To(value, duration, TimeFunctions.Linear);
        }

        public AnimationBuilder<THost, TValue> ToStep(TValue value, float duration)
        {
            return To(value, duration, TimeFunctions.Step);
        }

        public IAnimation Build()
        {
            CreateStepAnimation();
            return _parent.Build();
        }

        public IAnimation Add()
        {
            CreateStepAnimation();
            return _parent.Add();
        }

        public IAnimationControl Create()
        {
            CreateStepAnimation();
            return _parent.Create();
        }
    }

    public class AnimationBuilder<THost> where THost : Object3D
    {
        internal readonly THost? _host;
        int _iterationCount;
        string? _name;
        AnimationDirection _direction;
        internal IAnimation? _animation;
        private float _delay;

        public AnimationBuilder(THost? host = default)
        {
            _host = host;
            _iterationCount = 1;
        }

        public AnimationGroupBuilder<THost> BeginGroup()
        {
            if (_animation != null)
                throw new InvalidOperationException("An animation has already been defined for this builder. BeginGroup() must be called before defining another animation.");

            return new AnimationGroupBuilder<THost>(this);
        }

        public AnimationBuilder<THost, TValue> Target<TValue>(
            Expression<Func<THost, TValue>> target)
        {
            if (_animation != null)
                throw new InvalidOperationException("An animation has already been defined for this builder. Target() cannot be called again.");

            if (target.Body is not MemberExpression)
                throw new ArgumentException("Target must be a member access expression, for example 'x => x.Transform.Position'.", nameof(target));

            return new AnimationBuilder<THost, TValue>(this, target);
        }

        public AnimationBuilder<THost> Direction(AnimationDirection direction)
        {
            _direction = direction;
            return this;
        }

        public AnimationBuilder<THost> Name(string name)
        {
            _name = name;
            return this;
        }

        public AnimationBuilder<THost> Delay(float duration)
        {
            _delay = duration;
            return this;
        }

        public AnimationBuilder<THost> Loop(int iterationCount = 0)
        {
            _iterationCount = iterationCount;
            return this;
        }

        public IAnimation Build()
        {
            if (_animation == null)
                throw new InvalidOperationException("No animation has been defined. Define a target animation or a group before calling Build().");

            _animation.IterationCount = _iterationCount;
            _animation.Direction = _direction;
            _animation.Name = _name;
            _animation.Delay = _delay;

            return _animation;
        }

        public IAnimation Add()
        {
            if (_host == null)
                throw new InvalidOperationException("Add() requires a host object. Use Build() to create a host-independent animation.");

            var animHost = _host.EnsureComponent<AnimationsHost>();

            var animation = Build();

            animHost.AddAnimation(animation);

            return animation;
        }

        public IAnimationControl Create()
        {
            if (_host == null)
                throw new InvalidOperationException("Create() requires a host object. Use Build() to create a host-independent animation.");

            if (_host.Scene == null)
                throw new InvalidOperationException("Create() requires the host object to be attached to a scene.");

            var manager = _host.Scene.EnsureComponent<AnimationManager>();

            return manager.Create(Build(), _host);
        }
    }
}