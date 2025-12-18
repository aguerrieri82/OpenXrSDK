using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XrEngine.UI.Web
{
    public class WebBrowserBridge
    {
        class CallRequest
        {
            public Type? ResultType { get; set; }

            public TaskCompletionSource<object?>? CompletionSource { get; set; }
        }

        class MappedMethod
        {
            public MethodInfo? Info { get; set; }

            public object? Instance { get; set; }
        }

        class SendMessage
        {
            public string? Type { get; set; }

            public string? ReqId { get; set; }

            public object? Result { get; set; }

            public string? Method { get; set; }

            public object?[]? Args { get; set; }
        }

        readonly IWebBrowser _webBrowser;
        readonly Dictionary<string, MappedMethod> _methods;
        readonly Dictionary<string, CallRequest> _callRequests = [];

        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public WebBrowserBridge(IWebBrowser webBrowser)
        {
            _webBrowser = webBrowser;
            _webBrowser.MessageReceived += OnMessageReceived;
            _methods = [];
        }

        public async Task<T> CallAsync<T>(string method, params object?[] args)
        {
            SendMessage msg = new SendMessage
            {
                Type = "call",
                Args = args,
                Method = method,
                ReqId = Guid.NewGuid().ToString(),
            };

            TaskCompletionSource<object?> cs = new TaskCompletionSource<object?>();

            _callRequests[msg.ReqId] = new CallRequest
            {
                CompletionSource = cs,
                ResultType = typeof(T)
            };

            await _webBrowser.PostMessageAsync(JsonSerializer.Serialize(msg, _jsonOptions));

            object? result = await cs.Task;

            return (T)result!;
        }

        private async void OnMessageReceived(object? sender, MessageReceivedArgs e)
        {
            if (e.Message == "#ref")
                return;

            JsonNode jObj = JsonNode.Parse(e.Message)!;

            string? type = (string?)jObj["type"];

            if (type == "call")
            {
                SendMessage msg = new SendMessage();

                try
                {
                    string? method = (string?)jObj["method"];
                    msg.ReqId = (string?)jObj["reqId"];

                    if (method == null)
                        throw new InvalidOperationException();

                    JsonObject? args = (JsonObject?)jObj["args"];

                    MappedMethod mapped = _methods[method];
                    List<object?> parsedArgs = new List<object?>();

                    if (args != null)
                    {
                        foreach (ParameterInfo param in mapped.Info!.GetParameters())
                        {
                            object? value;

                            if (args.ContainsKey(param.Name!))
                                value = args[param.Name!].Deserialize(param.ParameterType, _jsonOptions);
                            else
                                value = param.DefaultValue;

                            if (value is DBNull)
                                value = null;

                            parsedArgs.Add(value);
                        }
                    }

                    object? result = mapped.Info!.Invoke(mapped.Instance, [.. parsedArgs]);

                    if (result is Task task)
                    {
                        string pName = task.GetType().GetGenericArguments()[0].Name;

                        if (pName == "VoidTaskResult" || pName == "Task")
                        {
                            await task;
                            result = null;
                        }
                        else
                            result = await (dynamic)result;
                    }


                    msg.Result = result;
                    msg.Type = "response";

                }
                catch (Exception ex)
                {
                    msg.Result = ex.Message;
                    msg.Type = "error";
                }

                await _webBrowser.PostMessageAsync(JsonSerializer.Serialize(msg, _jsonOptions));
            }
            else if (type == "response")
            {
                string reqId = (string)jObj["reqId"]!;

                CallRequest callRequest = _callRequests[reqId];

                _callRequests.Remove(reqId);

                object? result = jObj["result"].Deserialize(callRequest.ResultType!, _jsonOptions);

                callRequest.CompletionSource!.SetResult(result);
            }

            else if (type == "error")
            {
                string reqId = (string)jObj["reqId"]!;

                CallRequest callRequest = _callRequests[reqId];

                _callRequests.Remove(reqId);

                string? msg = (string?)jObj["result"];

                callRequest.CompletionSource!.SetException(new InvalidOperationException(msg));

            }
        }

        public void Register<T>() where T : new()
        {
            Register(new T());
        }

        public void Register(object obj)
        {
            foreach (MethodInfo method in obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                _methods[method.Name] = new MappedMethod
                {
                    Info = method,
                    Instance = obj
                };
            }
        }
    }
}
