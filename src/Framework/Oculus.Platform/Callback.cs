// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    public static class Callback
    {
        private static Dictionary<ulong, Request> requestIDsToRequests = new Dictionary<ulong, Request>();

        internal static void AddRequest(Request request)
        {
            requestIDsToRequests[request.RequestID] = request;
        }

        public static void RunCallbacks()
        {
            // Handle requests popping
            while (PlatformClient.GetResponseCount() > 0)
            {
                var msg = PlatformClient.PopResponse(true);
                if (msg == null)
                {
                    break;
                }

                HandleMessage(msg);
            }

            // Handle notification callback intent popping
            foreach (ulong sessionId in notificationCallbacks.Keys.ToArray())
            {
                while (PlatformClient.GetMessageCount(sessionId) > 0)
                {
                    var msg = PlatformClient.PopMessage(sessionId, true);
                    if (msg == null) break;
                    RequestCallback callbackHolder;
                    if (notificationCallbacks.TryGetValue(sessionId, out callbackHolder))
                    {
                        callbackHolder.HandleMessage(msg);
                    }
                }
            }
        }

        internal static void HandleMessage(Message msg)
        {
            Request request;
            if (requestIDsToRequests.TryGetValue(msg.requestID, out request))
            {
                try
                {
                    request.HandleMessage(msg);
                }
                finally
                {
                    requestIDsToRequests.Remove(msg.requestID);
                }

                return;
            }
        }

        #region Notification callback handlers

        private class RequestCallback
        {
            private Message.Callback messageCallback;

            /// This method initializes a new instance of the RequestCallback class.
            /// It is used to handle asynchronous requests made to the platform.
            public RequestCallback()
            {
            }

            /// This method initializes a new instance of the RequestCallback class with a specified callback function.
            /// It sets the message callback to the provided callback function, which will be executed when a response is received for the associated request.
            public RequestCallback(Message.Callback callback)
            {
                this.messageCallback = callback;
            }

            /// This method handles a message by executing the associated callback function, if one is provided.
            /// It checks if a message callback has been set, and if so, it calls the callback function: messageCallback(msg) with the provided message as an argument.
            public virtual void HandleMessage(Message msg)
            {
                if (messageCallback != null)
                {
                    messageCallback(msg);
                }
            }
        }

        private sealed class RequestCallback<T> : RequestCallback
        {
            private Message<T>.Callback callback;

            /// This method initializes a new instance of the RequestCallback class with a specified callback function.
            /// It sets the message callback to the provided callback function, which will be executed when a response is received for the associated request.
            public RequestCallback(Message<T>.Callback callback)
            {
                this.callback = callback;
            }

            /// This method handles a message by executing the associated callback function, if one is provided.
            /// It checks if a message callback has been set, and if so, it calls the callback function: callback(msg) with the provided message as an argument.
            public override void HandleMessage(Message msg)
            {
                if (callback != null)
                {
                    Message<T> typedMessage = new Message<T>(msg.requestID, msg.sessionID, msg.cookie, msg.data, msg.status);
                    callback(typedMessage);
                }
            }
        }

        private static Dictionary<ulong, RequestCallback> notificationCallbacks = new Dictionary<ulong, RequestCallback>();
        internal static void SetNotificationCallback(ulong sessionId, Message.Callback callback)
        {
            if (callback == null)
            {
                throw new Exception("Cannot provide a null notification callback.");
            }

            notificationCallbacks[sessionId] = new RequestCallback(callback);
        }

        internal static void SetNotificationCallback<T>(ulong sessionId, Message<T>.Callback callback)
        {
            if (callback == null)
            {
                throw new Exception("Cannot provide a null notification callback.");
            }

            notificationCallbacks[sessionId] = new RequestCallback<T>(callback);
        }

        #endregion

        internal static void OnApplicationQuit()
        {
            requestIDsToRequests.Clear();
            notificationCallbacks.Clear();
        }
    }
}
