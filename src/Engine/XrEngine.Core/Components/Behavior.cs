using System.Diagnostics.CodeAnalysis;

namespace XrEngine
{
    public abstract class Behavior<T> : BaseComponent<T>, IBehavior, IComponent<T> where T : EngineObject
    {
        protected double _startTime;
        protected double _lastUpdateTime;
        protected double _deltaTime;

        public Behavior()
        {
            _isEnabled = true;
            _startTime = -1;
        }

        public virtual void Reset(bool onlySelf = false)
        {
            _startTime = -1;
            _lastUpdateTime = 0;
        }


        protected virtual void Start(RenderContext ctx)
        {

        }

        protected virtual void Update(RenderContext ctx)
        {

        }

        void IRenderUpdate.Update(RenderContext ctx)
        {
            if (!_isEnabled || _suspendCount > 0)
                return;

            if (_startTime == -1)
            {
                Start(ctx);
                Log.Debug(this, "Started component {0}", GetType().Name);
                _startTime = ctx.Time;
                Started?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _deltaTime = _lastUpdateTime == 0 ? 0 : ctx.Time - _lastUpdateTime;

                EngineApp.Current!.Stats.Update(this, () => Update(ctx));

                _lastUpdateTime = ctx.Time;
            }
        }

        protected bool IsStarted => _startTime != -1;

        protected double DeltaTime => _deltaTime;


        public event EventHandler? Started;

        public IUpdateGroup? UpdateGroup { get; set; }

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
