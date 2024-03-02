namespace Xr.Editor.Helpers
{
    public static class UIUtils
    {
        static Timer? _timer;

        public static Task DelayAsync(TimeSpan delay)
        {
            var source = new TaskCompletionSource();

            void OnTimer(object? state)
            {
                Context.Require<IMainDispatcher>().ExecuteAsync(() => source.SetResult());
                _timer?.Dispose();
                _timer = null;
            }

            if (_timer != null)
                _timer.Dispose();

            _timer = new Timer(OnTimer, null, (int)delay.TotalMilliseconds, Timeout.Infinite);

            return source.Task;
        }
    }
}
