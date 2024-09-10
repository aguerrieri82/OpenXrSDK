using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace XrEngine.UI.Web
{
    public class WebBrowserBridge
    {
        class MappedMethod
        {
            public MethodInfo? Info { get; set; }

            public object? Instance { get; set; }
        }

        class ResponseMessage
        {
            public string? Type { get; set; }

            public string? ReqId { get; set; }

            public object? Result { get; set; }
        }

        private readonly IWebBrowser _webBrowser;
        private readonly Dictionary<string, MappedMethod> _methods;

        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public WebBrowserBridge(IWebBrowser webBrowser)
        {
            _webBrowser = webBrowser;
            _webBrowser.MessageReceived += OnMessageReceived;
            _methods = [];
        }

        private async void OnMessageReceived(object? sender, MessageReceivedArgs e)
        {
            var jObj = JsonNode.Parse(e.Message)!;

            var type = (string?)jObj["type"];

            if (type == "call")
            {
                var msg = new ResponseMessage();

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
                                parsedArgs.Add(args[param.Name!].Deserialize(param.ParameterType));
                            else
                                parsedArgs.Add(param.DefaultValue);
                        }
                    }

                    dynamic? result = mapped.Info!.Invoke(mapped.Instance, [.. parsedArgs]);
                    
                    if (result is Task task)
                        result = await result;

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
