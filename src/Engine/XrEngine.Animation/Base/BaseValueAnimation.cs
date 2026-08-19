namespace XrEngine.Animation
{
    public abstract class BaseValueAnimation<TValue> : IAnimation
    {
        static ValueHandlerRegistry? _registry;

        protected Action<AnimationTarget<TValue>>? _setTarget;
        protected float _delay;
        protected int _iterationCount;
        protected AnimationDirection _direction;
        protected string? _name;

        protected readonly IAnimationValueHandler<TValue> _valueHandler;

        public BaseValueAnimation(IAnimationValueHandler<TValue>? valueHandler = null)
        {
            _iterationCount = 1;
            _direction = AnimationDirection.Forward;
            _valueHandler = (valueHandler ?? CreateValueHandler());
        }

        public virtual IAnimationValueHandler<TValue> CreateValueHandler()
        {
            _registry ??= Context.Require<ValueHandlerRegistry>();
            return _registry.GetHandler<TValue>();
        }

        public abstract float Duration { get; }

        public abstract IAnimationControl CreateControl(IAnimationManager manager, IAnimable? host = null);

        public Action<AnimationTarget<TValue>>? SetTarget
        {
            get => _setTarget;
            set => _setTarget = value;
        }

        public float Delay
        {
            get => _delay;
            set => _delay = value;
        }

        public AnimationDirection Direction
        {
            get => _direction;
            set => _direction = value;
        }

        public int IterationCount
        {
            get => _iterationCount;
            set => _iterationCount = value;
        }

        public string? Name
        {
            get => _name;
            set => _name = value;
        }
    }
}
