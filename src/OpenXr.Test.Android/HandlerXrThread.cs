using Android.OS;
using Android.Util;
using OpenXr.WebLink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Graphics.ImageDecoder;

namespace OpenXr.Test.Android
{
    public class HandlerXrThread : IXrThread
    {
        Handler _handler;

        public HandlerXrThread(Handler handler)
        {
            _handler = handler;

        }

        public Task<T> ExecuteAsync<T>(Func<T> action)
        {
            var source = new TaskCompletionSource<T>();

            _handler.Post(() =>
            {
                Log.Debug("HandlerXrThread", Process.MyTid().ToString());
                source.SetResult(action());
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
                    _handler.Post(() => source.SetResult(t.Result));
                });
            });

            return source.Task;
        }
    }
}