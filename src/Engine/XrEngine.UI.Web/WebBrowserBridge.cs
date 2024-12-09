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
            var msg = new SendMessage
            {
                Type = "call",
                Args = args,
                Method = method,
                ReqId = Guid.NewGuid().ToString(),
            };

            var cs = new TaskCompletionSource<object?>();

            _callRequests[msg.ReqId] = new CallRequest
            {
                CompletionSource = cs,
                ResultType = typeof(T)
            };

            await _webBrowser.PostMessageAsync(JsonSerializer.Serialize(msg, _jsonOptions));

            var result = await cs.Task;

            return (T)result!;
        }

        private async void OnMessageReceived(object? sender, MessageReceivedArgs e)
        {
            if (e.Message == "#ref")
                return;

            var jObj = JsonNode.Parse(e.Message)!;

            var type = (string?)jObj["type"];

            if (type == "call")
            {
                var msg = new SendMessage();

                try
                {
                    var method = (string?)jObj["method"];
                    msg.ReqId = (string?)jObj["reqId"];

                    if (method == null)
                        throw new InvalidOperationException();

                    var args = (JsonObject?)jObj["args"];

                    var mapped = _methods[method];
                    var parsedArgs = new List<object?>();

                    if (args != null)
                    {
                        foreach (var param in mapped.Info!.GetParameters())
                        {
                            if (args.ContainsKey(param.Name!))
                                parsedArgs.Add(args[param.Name!].Deserialize(param.ParameterType, _jsonOptions));
                            else
                                parsedArgs.Add(param.DefaultValue);
                        }
                    }

                    object? result = mapped.Info!.Invoke(mapped.Instance, [.. parsedArgs]);

                    if (result is Task task)
                    {
                        var pName = task.GetType().GetGenericArguments()[0].Name;

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
                var reqId = (string)jObj["reqId"]!;

                var callRequest = _callRequests[reqId];

                _callRequests.Remove(reqId);

                var result = jObj["result"].Deserialize(callRequest.ResultType!, _jsonOptions);

                callRequest.CompletionSource!.SetResult(result);
            }

            else if (type == "error")
            {
                var reqId = (string)jObj["reqId"]!;

                var callRequest = _callRequests[reqId];

                _callRequests.Remove(reqId);

                var msg = (string?)jObj["result"];

                callRequest.CompletionSource!.SetException(new InvalidOperationException(msg));

            }
        }

        public void Register<T>() where T : new()
        {
            Register(new T());
        }

        public void Register(object obj)
        {
            foreach (var method in obj.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
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
