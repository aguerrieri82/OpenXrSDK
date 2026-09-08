// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;

namespace Oculus.Platform
{
    public partial class Message<T> : Message
    {
        public new delegate void Callback(Message<T> message);

        private static readonly JsonSerializerSettings DeserializationSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            Converters = { new BoolFromObjectConverter() }
        };

        public Message(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status) : base(requestID, sessionID, cookie, data, status)
        {
            if (IsError)
            {
                Data = default;
            }
            else if (typeof(T) == typeof(string))
            {
                Data = (T)(object)data;
            }
            else
            {
                Data = JsonConvert.DeserializeObject<T>(data, DeserializationSettings);
            }
        }

        public T Data;

        // Gets the System.Type of the message data payload.
        // ex. if (msg.DataType == typeof(LeaderboardEntryList))
        public Type DataType => typeof(T);
    }

    public partial class Message
    {
        public delegate void Callback(Message message);

        public Message(ulong requestID, ulong sessionID, int cookie, string data, HorizonStatus status)
        {
            this.requestID = requestID;
            this.sessionID = sessionID;
            this.cookie = cookie;
            this.data = data;
            this.status = status;

            if (!status.IsSuccess())
            {
                error = Error.FromStatus(status);
            }

            if (Core.LogMessages)
            {
                PlatformLog.Log(data);
            }
        }

        public ulong requestID;
        public ulong RequestID => requestID;
        public ulong sessionID;
        public int cookie;
        public string data;
        public HorizonStatus status;
        public bool IsError => !status.IsSuccess();
        private Error error;

        /// Returns the error object associated with this message, or null if there is no error.
        public virtual Error GetError() { return error; }
    }
}
