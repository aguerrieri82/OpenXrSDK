namespace Xr.Engine
{
    public abstract class Behavior<T> : IBehavior, IComponent<T> where T : IComponentHost
    {
        protected T? _host;
        private double _startTime;
        private double _lastUpdateTime;
        private double _deltaTime;
        private bool _isEnabled;

        public Behavior()
        {
            _isEnabled = true;
            _startTime = -1;
        }

        protected virtual void Start(RenderContext ctx)
        {

        }

        protected virtual void Update(RenderContext ctx)
        {

        }

        protected virtual void OnDisabled()
        {

        }

        protected virtual void OnEnabled()
        {

        }

        void IRenderUpdate.Update(RenderContext ctx)
        {
            if (!_isEnabled)
                return;

            if (_startTime == -1)
            {
                Start(ctx);
                _startTime = ctx.Time;
            }
            else
            {
                _deltaTime = _lastUpdateTime == 0 ? 0 : ctx.Time - _lastUpdateTime;
                Update(ctx);
                _lastUpdateTime = ctx.Time;
            }
        }

        void IComponent<T>.Attach(T host)
        {
            _host = host;
        }

        void IComponent.Detach()
        {
            _host = default;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                if (!_isEnabled)
                    OnDisabled();
                else
                    OnEnabled();
            }
        }

        protected double DeltaTime => _deltaTime;

        T? IComponent<T>.Host => _host;
    }

    public class LambdaBehavior<T> : Behavior<T> where T : IComponentHost
    {
        readonly Action<T, RenderContext> _update;

        public LambdaBehavior(Action<T, RenderContext> update)
        {
            _update = update;
        }
        protected override void Update(RenderContext ctx)
        {
            _update(_host!, ctx);
        }
    }
}
