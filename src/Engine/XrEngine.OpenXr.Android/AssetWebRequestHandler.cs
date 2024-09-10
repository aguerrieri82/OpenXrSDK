using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrEngine.UI.Web;
using Context2 = global::Android.Content.Context;

namespace XrEngine.OpenXr.Android
{
    public class AssetWebRequestHandler : IWebRequestHandler
    {
        protected string? _host;
        protected string? _basePath;
        protected Context2 _context;

        public AssetWebRequestHandler(Context2 context, string host, string basePath)
        {
            _host = host;
            _basePath = basePath;
            _context = context;
        }

        public bool CanHandle(WebRequest request)
        {
            return request.Uri?.Host == _host && request.Method == "GET";
        }


        public WebResponse? HandleRequest(WebRequest request)
        {
  
            var path = request.Uri!.LocalPath;
            if (path == "/")
                path = "index.html";

            var fullPath = Path.Join(_basePath, path);

            try
            {
                using var srcStream = _context.Assets!.Open(fullPath);
                using var memStream = new MemoryStream();

                var buf = new byte[64 * 1024];
                while (true)
                {
                    var read = srcStream.Read(buf);
                    if (read == 0)
                        break;
                    memStream.Write(buf, 0, read);  
                }

                var ext = Path.GetExtension(path).ToLower();
                var mimeType = MimeMapper.GetMimeType(ext);

                return new WebResponse
                {
                    Body = memStream.ToArray(),
                    Code = 200,
                    Headers = new()
                    {
                        ["Access-Control-Allow-Origin"] = "*",
                        ["Content-Type"] = mimeType,
                        ["Content-Length"] = memStream.Length.ToString(),
                    }
                };
            }
            catch
            {
                return new WebResponse
                {
                    Code = 404
                };
            }
 
        }

        public string Scheme => "ui";

    }
}
