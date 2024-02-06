using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public struct SyncEvent : IDisposable
    {
        SemaphoreSlim _semaphore;

        public SyncEvent()
        {
            _semaphore = new SemaphoreSlim(0, 1);
        }

      
        public void Signal()
        {
            _semaphore.Release();
        }

        public bool Wait(CancellationToken? token)
        {
            try
            {
                _semaphore.Wait(token ?? CancellationToken.None);
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

        public void Dispose()
        {
            _semaphore.Dispose();
        }

    }
}
