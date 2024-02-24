namespace Xr.Engine
{
    public abstract class Behavior<T> : IBehavior, IComponent<T> where T : IComponentHost
    {
        protected float _startTime;
        protected T? _host;
        private bool _isStarted;

        public Behavior()
        {
            IsEnabled = true;
        }

        public virtual void Start(RenderContext ctx)
        {

        }

        protected virtual void Update(RenderContext ctx)
        {

        }

        void IRenderUpdate.Update(RenderContext ctx)
        {
            if (!IsEnabled)
                return;

            if (!_isStarted)
            {
                Start(ctx);
                _isStarted = true;
            }
            else
                Update(ctx);
        }

        void IComponent<T>.Attach(T host)
        {
            _host = host;
        }

        void IComponent.Detach()
        {
            _host = default;
        }

        public bool IsEnabled { get; set; }

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
