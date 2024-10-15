using System.Collections.Concurrent;
using XrEngine;

namespace XrEditor.Services
{
    public record struct LogMessage(string Text, LogLevel Level, DateTime Date);

    public abstract class BaseUIRetainLogger : IProgressLogger, IAsyncDisposable
    {
        private double _progressCurrent;
        private double _progressTotal;
        private string? _progressMessage;

        protected ConcurrentQueue<LogMessage> _messages;
        protected DateTime _lastUpdate;
        protected bool _isDirty;
        protected bool _isActive;
        protected IMainDispatcher _main;
        protected Task _updateLoop;
        protected bool _isDisposed;
        private bool _updating;

        public BaseUIRetainLogger()
        {
            _isActive = true;
            _main = Context.Require<IMainDispatcher>();
            _messages = [];
            _updateLoop = UpdateLoopTask();
        }

        protected async Task UpdateLoopTask()
        {
            while (!_isDisposed)
            {
                if (_isDirty && _isActive)
                    await UpdateAsync();
                await Task.Delay(100);
            }
        }

        protected async Task UpdateAsync()
        {
            if (_updating)
                return;

            _updating = true;

            var progressMgs = _progressMessage;
            var progressCurrent = _progressCurrent;
            var progressTotal = _progressTotal;

            var invokeTime = DateTime.Now;

            _lastUpdate = invokeTime;
            _isDirty = false;

            await _main.ExecuteAsync(() =>
            {
                if (_messages.Count > 0)
                    UpdateMessages();

                UpdateProgress(progressCurrent, progressTotal, progressMgs);

                _updating = false;
            });
        }

        protected abstract void UpdateMessages();

        protected abstract void UpdateProgress(double current, double total, string? message);

        public virtual void BeginTask(object source, string? message = null)
        {
            LogProgress(source, 1, 0);
            if (message != null)
                LogMessage(source, message, LogLevel.Info);
        }

        public virtual void EndTask()
        {
            LogProgress(this, 0, 0);
            LogMessage(this, "Done", LogLevel.Info);
        }

        public virtual void LogMessage(object source, string text, LogLevel level = LogLevel.Info, bool retain = false)
        {
            if (string.IsNullOrWhiteSpace(text) || !IsActive)
                return;

            _messages.Enqueue(new LogMessage(text, level, DateTime.Now));

            if (!retain)
                _ = UpdateAsync();
            else
                _isDirty = true;
        }

        public virtual void LogProgress(object source, double current, double total, string? message = null, bool retain = false)
        {
            if (!IsActive)
                return;

            _progressTotal = total;
            _progressCurrent = current;
            _progressMessage = message;

            if (!retain)
                _ = UpdateAsync();
            else
                _isDirty = true;
        }


        public async ValueTask DisposeAsync()
        {
            _isDisposed = true;
            await _updateLoop;
            GC.SuppressFinalize(this);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                    return;
                _isActive = value;
            }
        }
    }
}
