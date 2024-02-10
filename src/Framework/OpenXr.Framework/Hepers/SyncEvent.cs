namespace OpenXr.Framework
{
    public readonly struct SyncEvent : IDisposable
    {
        readonly SemaphoreSlim _semaphore;

        public SyncEvent()
        {
            _semaphore = new SemaphoreSlim(0, 1);
        }

        public readonly void Signal()
        {
            _semaphore.Release();
        }

        public readonly bool Wait()
        {
            return Wait(CancellationToken.None);
        }

        public readonly bool Wait(CancellationToken cancellationToken)
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

        public async readonly Task<bool> WaitAsync(CancellationToken cancellationToken)
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


        public readonly void Dispose()
        {
            _semaphore.Dispose();

            GC.SuppressFinalize(this);
        }

    }
}
