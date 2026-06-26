using Microsoft.Extensions.Hosting;

namespace XrEngine
{


    public abstract class AsyncBehavior<T> : BaseComponent<T>, IBehavior  where T : EngineObject
    {

        Task? _updateTask;
        protected double _startTime;
        protected double _lastUpdateTime;
        protected double _deltaTime;
        
        public AsyncBehavior()
        {
            _startTime = -1;
        }

        public virtual void Reset(bool onlySelf = false)
        {
            _startTime = -1;
            _lastUpdateTime = 0;
        }

        public void ForceUpdate(RenderContext ctx)
        {
            ((IRenderUpdate)this).Update(ctx);
        }

        protected override void OnAttach()
        {
            Dispatcher = EngineApp.Current!.Dispatcher;
        }

        protected async Task UpdateInternalAsync(RenderContext ctx)
        {
            if (!_isEnabled || _suspendCount > 0 || _host == null)
                return;

            if (_startTime == -1)
            {
                await StartAsync(ctx);
                
                Log.Debug(this, "Started component {0}", GetType().Name);

                _startTime = ctx.Time;
                
                Started?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _deltaTime = _lastUpdateTime == 0 ? 0 : ctx.Time - _lastUpdateTime;

                try
                {
                    await UpdateAsync(ctx);
                    await EngineApp.MainThread;
                }
                catch (Exception ex)
                {
                    Log.Error(this, ex, "Update error: {0}");
                }

                _lastUpdateTime = ctx.Time;
            }
        }


        protected virtual Task StartAsync(RenderContext ctx)
        {
            return Task.CompletedTask;
        }


        protected virtual void UpdateSync(RenderContext ctx)
        {

        }

        protected virtual Task UpdateAsync(RenderContext ctx)
        {
            return Task.CompletedTask;
        }


        void IRenderUpdate.Update(RenderContext ctx)
        {
            UpdateSync(ctx);

            if (_updateTask != null)
            {
                if (!_updateTask.IsCompleted)
                    return;

                if (_updateTask.IsFaulted)
                    _updateTask.GetAwaiter().GetResult();

                _updateTask = null;
            }

            _updateTask = UpdateInternalAsync(ctx);
        }

        protected IDispatcher? Dispatcher { get; set; }

        protected bool IsStarted => _startTime != -1;

        protected double DeltaTime => _deltaTime;

        public IUpdateGroup? UpdateGroup { get; set; }

        public int UpdatePriority { get; protected set; }

        public event EventHandler? Started;

    }
}
