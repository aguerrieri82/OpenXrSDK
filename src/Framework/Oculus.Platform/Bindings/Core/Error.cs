// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using Oculus.Platform;

namespace Oculus.Platform.Models
{
    /// It represents an error response from the server.
    /// It contains error information such as the error code, http status code, and technical message.
    /// It's used whenever the server needs to communicate an error or failure to the client.
    public class Error
    {
        private static readonly JsonSerializerSettings ErrorDeserializationSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        public Error(int code, string message, int httpCode = -1)
        {
            Message = message;
            Code = code;
            HttpCode = httpCode;
        }

        public static Error FromStatus(HorizonStatus status)
        {
            string context = status.GetContext();
            int httpCode = -1;

            if (!string.IsNullOrEmpty(context))
            {
                try
                {
                    var errorWrapper = JsonConvert.DeserializeObject<ErrorInfoWrapper>(context, ErrorDeserializationSettings);
                    if (errorWrapper?.Error != null)
                    {
                        httpCode = errorWrapper.Error.HttpCode;
                    }
                }
                catch (JsonException)
                {
                    // Not a valid JSON or parsing failed, keep httpCode as -1
                }
            }

            return new Error(status.Code(), context, httpCode);
        }

        /// Check Status.cs for error codes
        public readonly int Code;

        /// It contains the HTTP status code for the error. More information about the http code can be found [here](https://en.wikipedia.org/wiki/List_of_HTTP_status_codes).
        public readonly int HttpCode;

        /// Technical description of what went wrong intended for developers. For use in logs or developer consoles.
        public readonly string Message;
    }
}
