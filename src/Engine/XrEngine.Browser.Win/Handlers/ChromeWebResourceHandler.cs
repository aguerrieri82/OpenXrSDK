using CefSharp;
using CefSharp.Callback;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Web;
using XrInteraction;

namespace XrEngine.Browser.Win
{
    internal class ChromeWebResourceHandler : IResourceHandler
    {
        readonly IWebRequestHandler _handler;
        WebResponse? _response;
        MemoryStream? _bodyStream;


        public ChromeWebResourceHandler(IWebRequestHandler webRequestHandler)
        {
            _handler = webRequestHandler;
        }

        public void Cancel()
        {

        }

        public void Dispose()
        {
            _bodyStream?.Dispose();
        }

        public void GetResponseHeaders(IResponse response, out long responseLength, out string? redirectUrl)
        {
            redirectUrl = null;
            responseLength = _response!.Body?.LongLength ?? 0;

            response.StatusCode = _response.Code;
            response.Charset = "utf-8";

            if (_response.Headers != null)
            {
                response.MimeType = _response.Headers["Content-Type"];
                foreach (var header in _response.Headers)
                    response.SetHeaderByName(header.Key, header.Value, true);
            }

            if (_response.Body != null)
                _bodyStream = new MemoryStream(_response.Body);
        }

        public bool Open(IRequest request, out bool handleRequest, ICallback callback)
        {
            _response = _handler.HandleRequest(request.ToWebRequest());
            handleRequest = true;
            return _response != null;
        }

        public bool ProcessRequest(IRequest request, ICallback callback)
        {
            throw new NotImplementedException();
        }

        public bool Read(Stream dataOut, out int bytesRead, IResourceReadCallback callback)
        {
            if (_bodyStream == null || _bodyStream.Position == _bodyStream.Length)
            {
                bytesRead = 0;
                return false;
            }

            var buffer = new byte[dataOut.Length];
            bytesRead = _bodyStream.Read(buffer);
            dataOut.Write(buffer, 0, bytesRead);

            return true;
        }

        public bool ReadResponse(Stream dataOut, out int bytesRead, ICallback callback)
        {
            throw new NotImplementedException();
        }

        public bool Skip(long bytesToSkip, out long bytesSkipped, IResourceSkipCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
