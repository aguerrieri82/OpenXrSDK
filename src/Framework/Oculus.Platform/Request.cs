// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Oculus.Platform
{
    public class Request<T> : Request
    {
        public readonly Type requestType;
        public bool HasCallback => _genericCallback != null;

        private Message<T>.Callback _genericCallback = null;
        private TaskCompletionSource<Message<T>> _tcs = null;

        public Request(ulong requestID) : base(requestID)
        {
            requestType = typeof(T);
        }

        public Request<T> OnComplete(Message<T>.Callback callback)
        {
            if (_genericCallback != null || _tcs != null)
            {
                throw new InvalidOperationException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
            }

            _genericCallback = callback;
            Callback.AddRequest(this);
            return this;
        }

        #region Async await functions
        // Legacy await handler: await Leaderboards.GetEntries().Gen();
        public new async Task<Message<T>> Gen()
        {
            if (_genericCallback != null || _tcs != null)
            {
                throw new InvalidOperationException("Attempted to attach multiple handlers to a Request.  This is not allowed.");
            }

            _tcs = new TaskCompletionSource<Message<T>>();
            Callback.AddRequest(this);
            return await _tcs.Task;
        }

        // New await handler: await Leaderboards.GetEntries();
        public new TaskAwaiter<Message<T>> GetAwaiter()
        {
            return Gen().GetAwaiter();
        }
        #endregion

        override public void HandleMessage(Message msg)
        {
            Message<T> typedMessage;
            try
            {
                typedMessage = new Message<T>(msg.requestID, msg.sessionID, msg.cookie, msg.data, msg.status);
            }
            catch (Exception error) when (_tcs != null)
            {
                _tcs.TrySetException(error);
                return;
            }

            // handles the case with non typed message, where dev choose to convert themselves
            //
            // Leaderboards.GetEntries().OnComplete((Message message) => {
            //      Message<LeaderboardEntryList> msg = message as Message<LeaderboardEntryList>;
            //      LeaderboardEntryList entryList = msg.Data;
            // }
            if (_callback != null)
            {
                _callback(typedMessage);
                return;
            }

            // handles the case with typed message, the proper approach dev should use
            //
            // Leaderboards.GetEntries().OnComplete((Message<LeaderboardEntryList> message) => {
            //      LeaderboardEntryList entryList = msg.Data;
            // }
            if (_genericCallback != null)
            {
                _genericCallback(typedMessage);
                return;
            }

            // handles the case with Async Await, the modern async approach without callback
            //
            // Message<LeaderboardEntryList> listMessage = await Leaderboards.GetEntries().Gen();
            // LeaderboardEntryList entryList = msg.Data;
            if (_tcs != null)
            {
                _tcs.SetResult(typedMessage);
                return;
            }

            throw new InvalidOperationException("Request<T> with no handler. This should never happen.");
        }
    }
    public class Request
    {
        public ulong RequestID { get; set; }
        protected Message.Callback _callback;
        private TaskCompletionSource<Message> _tcs;


        public Request(ulong requestID)
        {
            this.RequestID = requestID;
        }

        public Request OnComplete(Message.Callback callback)
        {
            _callback = callback;
            Callback.AddRequest(this);
            return this;
        }

        #region Async await functions
        public async Task<Message> Gen()
        {
            _tcs = new TaskCompletionSource<Message>();
            Callback.AddRequest(this);
            return await _tcs.Task;
        }

        public TaskAwaiter<Message> GetAwaiter()
        {
            return Gen().GetAwaiter();
        }
        #endregion

        virtual public void HandleMessage(Message msg)
        {
            if (_callback != null)
            {
                _callback(msg);
                return;
            }

            if (_tcs != null)
            {
                _tcs.SetResult(msg);
                return;
            }

            throw new InvalidOperationException("Request with no handler. This should never happen.");
        }

        public static void RunCallbacks(uint limit = 0)
        {
            // limit parameter is deprecated, but we ignore it to avoid breaking existing code
            Callback.RunCallbacks();
        }
    }
}
