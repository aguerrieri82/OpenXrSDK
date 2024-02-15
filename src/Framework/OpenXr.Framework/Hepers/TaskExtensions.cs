using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Framework
{
    public static class TaskExtensions
    {
        public static void ScheduleCancel<T>(this TaskCompletionSource<T> completionSource, TimeSpan time)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(time);
                if (!completionSource.Task.IsCompleted)
                    completionSource.SetCanceled();
            });
        }
    }
}
