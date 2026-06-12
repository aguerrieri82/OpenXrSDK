namespace XrEngine
{
    public abstract class AsyncBehavior<T> : Behavior<T> where T : EngineObject
    {

        Task? _updateTask;

        protected virtual Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual Task StartAsync()
        {
            return Task.CompletedTask;
        }

        protected override void Start(RenderContext ctx)
        {
            base.Start(ctx);
        }

        protected sealed override void Update(RenderContext ctx)
        {
            if (_updateTask != null)
            {
                if (!_updateTask.IsCompleted)
                    return;

                if (_updateTask.Exception != null)
                    throw _updateTask.Exception;

                _updateTask = null;
            }

            _updateTask = UpdateAsync();
        }
    }


}
