namespace OpenXr.Framework
{
    public struct SyncEvent : IDisposable
    {
        readonly SemaphoreSlim _semaphore;

        public SyncEvent()
        {
            _semaphore = new SemaphoreSlim(0, 1);
        }


        public void Signal()
        {
            _semaphore.Release();
        }

        public bool Wait()
        {
            return Wait(CancellationToken.None);
        }

        public bool Wait(CancellationToken cancellationToken)
        {
            try
            {
                _semaphore.Wait(cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<bool> WaitAsync()
        {
            return WaitAsync(CancellationToken.None);
        }

        public async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public void Dispose()
        {
            _semaphore.Dispose();
        }

    }
}
