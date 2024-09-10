using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.UI.Web
{
    public class WebResponse
    {
        public int Code { get; set; }

        public Dictionary<string, string>? Headers { get; set; }

        public byte[]? Body { get; set; }
    }

    public class WebRequest
    {
        public Uri? Uri { get; set; }

        public string? Method { get; set; }

        public Dictionary<string, string>? Headers { get; set; }

        public byte[]? Body { get; set; }
    }

    public interface IWebRequestHandler
    {
        bool CanHandle(WebRequest request);

        WebResponse? HandleRequest(WebRequest request);

        string Scheme { get; }
    }
}
