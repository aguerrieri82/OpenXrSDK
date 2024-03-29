namespace XrEngine
{
    public abstract class Behavior<T> : BaseComponent<T>, IBehavior, IComponent<T> where T : EngineObject
    {
        private double _startTime;
        private double _lastUpdateTime;
        private double _deltaTime;


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

        void IRenderUpdate.Update(RenderContext ctx)
        {
            if (!_isEnabled)
                return;

            if (_startTime == -1)
            {
                Start(ctx);
                _startTime = ctx.Time;
                Started?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _deltaTime = _lastUpdateTime == 0 ? 0 : ctx.Time - _lastUpdateTime;
                Update(ctx);
                _lastUpdateTime = ctx.Time;
            }
        }

        public event EventHandler? Started;

        protected double DeltaTime => _deltaTime;
    }

    public class LambdaBehavior<T> : Behavior<T> where T : EngineObject
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
