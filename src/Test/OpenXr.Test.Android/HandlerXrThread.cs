﻿using Android.OS;
using OpenXr.Framework;


namespace OpenXr.Test.Android
{
    public class HandlerXrThread : IXrThread
    {
        readonly Handler _handler;

        public HandlerXrThread(Handler handler)
        {
            _handler = handler;

        }

        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            var source = new TaskCompletionSource<T>();

            _handler.Post(() =>
            {
                try
                {
                    source.SetResult(action());
                }
                catch (Exception ex)
                {
                    source.SetException(ex);
                }
            });

            return source.Task;
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            var source = new TaskCompletionSource<T>();

            _handler.Post(() =>
            {
                action().ContinueWith(t =>
                {
                    if (t.Exception != null)
                        source.SetException(t.Exception);
                    else
                        _handler.Post(() => source.SetResult(t.Result));
                });
            });

            return source.Task;
        }
    }
}