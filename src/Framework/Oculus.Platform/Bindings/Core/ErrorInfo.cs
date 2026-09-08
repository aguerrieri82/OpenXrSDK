// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
namespace Oculus.Platform
{
    /// <summary>
    /// Model for deserializing error information from the statusMessage JSON.
    /// The JSON structure from the server looks like:
    /// {
    ///   "error": {
    ///     "is_graph_api_error": true,
    ///     "http_code": 400,
    ///     "message": "Error message here",
    ///     "type": "OAuthException",
    ///     "code": 1005,
    ///     "error_subcode": 1891065,
    ///     "is_transient": false,
    ///     "error_user_title": "Error Title",
    ///     "error_user_msg": "User-friendly error message.",
    ///     "fbtrace_id": "trace_id_here"
    ///   }
    /// }
    /// </summary>
    internal class ErrorInfoWrapper
    {
        [JsonProperty("error")]
        public ErrorInfo Error { get; set; }
    }

    /// <summary>
    /// Represents the inner error object containing detailed error information.
    /// </summary>
    internal class ErrorInfo
    {
        [JsonProperty("is_graph_api_error")]
        public bool IsGraphApiError { get; set; }

        [JsonProperty("http_code")]
        public int HttpCode { get; set; } = -1;

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("error_subcode")]
        public int ErrorSubcode { get; set; }

        [JsonProperty("is_transient")]
        public bool IsTransient { get; set; }

        [JsonProperty("error_user_title")]
        public string ErrorUserTitle { get; set; }

        [JsonProperty("error_user_msg")]
        public string ErrorUserMsg { get; set; }

        [JsonProperty("fbtrace_id")]
        public string FbtraceId { get; set; }
    }
}
