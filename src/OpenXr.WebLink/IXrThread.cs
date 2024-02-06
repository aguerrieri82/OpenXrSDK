﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink
{
    public interface IXrThread
    {
        Task<T> ExecuteAsync<T>(Func<T> action);

        Task<T> ExecuteAsync<T>(Func<Task<T>> action);
    }

    public static class XrThreadExtensions
    {
        public static async Task ExecuteAsync(this IXrThread thread, Action action)
        {
            await thread.ExecuteAsync(() =>
            {
                action();
                return true;
            });
        }

    }
}
