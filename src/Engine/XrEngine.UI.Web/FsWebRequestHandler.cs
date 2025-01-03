﻿namespace XrEngine.UI.Web
{
    public class FsWebRequestHandler : IWebRequestHandler
    {
        protected string? _host;
        protected string? _basePath;

        public FsWebRequestHandler(string host, string basePath)
        {
            _host = host;
            _basePath = basePath;
        }

        public bool CanHandle(WebRequest request)
        {
            return request.Uri?.Host == _host && request.Method == "GET";
        }

        public WebResponse? HandleRequest(WebRequest request)
        {
            var fullPath = Path.Join(_basePath, request.Uri!.LocalPath);

            if (!File.Exists(fullPath))
                fullPath = Path.Join(_basePath, "index.html");

            var ext = Path.GetExtension(fullPath).ToLower();
            var mimeType = MimeMapper.GetMimeType(ext);

            var data = File.ReadAllBytes(fullPath);

            return new WebResponse
            {
                Body = data,
                Code = 200,
                Headers = new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Content-Type"] = mimeType,
                    ["Content-Length"] = data.Length.ToString(),
                }
            };
        }

        public string Scheme => "ui";
    }
}
