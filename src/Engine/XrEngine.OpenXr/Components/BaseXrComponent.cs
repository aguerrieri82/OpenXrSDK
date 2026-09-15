using OpenXr.Framework;

namespace XrEngine.OpenXr
{
    public abstract class BaseXrComponent<T> : Behavior<T> where T : Object3D
    {
        protected XrApp? _xrApp;
        protected bool _isAsync;
        protected Task? _updateTask;


        protected sealed override void Update(RenderContext ctx)
        {
            var curApp = XrApp.Current;

            if (_xrApp == null && curApp != null && curApp.IsStarted)
            {
                _xrApp = curApp;
                AttachXr();
            }

            if (_xrApp != null)
            {
                if (!_xrApp.IsStarted)
                {
                    DetachXr();
                    _xrApp = null;
                }
                else
                    UpdateWork(ctx);
            }
        }

        protected virtual void UpdateWork(RenderContext ctx)
        {
            if (_isAsync)
            {
                if (_updateTask != null)
                {
                    if (!_updateTask.IsCompleted)
                        return;

                    if (_updateTask.IsFaulted)
                        _updateTask.GetAwaiter().GetResult();

                    _updateTask = null;
                }

                _updateTask = UpdateAsync(ctx);
            }
        }

        protected virtual Task UpdateWorkAsync(RenderContext ctx)
        {
            return Task.CompletedTask;
        }

        protected async Task UpdateAsync(RenderContext ctx)
        {
            if (!_isEnabled || _suspendCount > 0 || _host == null)
                return;

            if (_startTime == -1)
                _startTime = ctx.Time;
            else
            {
                _deltaTime = _lastUpdateTime == 0 ? 0 : ctx.Time - _lastUpdateTime;

                try
                {
                    await UpdateWorkAsync(ctx);
                }
                catch (Exception ex)
                {
                    Log.Error(this, ex, "Update error: {0}");
                }

                await EngineApp.MainThread;

                _lastUpdateTime = ctx.Time;
            }
        }

        protected virtual void AttachXr()
        {

        }

        protected virtual void DetachXr()
        {

        }
    }
}
