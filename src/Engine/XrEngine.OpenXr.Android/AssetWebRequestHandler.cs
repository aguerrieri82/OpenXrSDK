using XrEngine.UI.Web;
using Context2 = global::Android.Content.Context;

namespace XrEngine.OpenXr.Android
{
    public class AssetWebRequestHandler : IWebRequestHandler
    {
        protected string? _host;
        protected string? _basePath;
        protected WeakReference<Context2> _context;

        public AssetWebRequestHandler(Context2 context, string host, string basePath)
        {
            _host = host;
            _basePath = basePath;
            _context = new WeakReference<Context2>(context);
        }

        public bool CanHandle(WebRequest request)
        {
            return request.Uri?.Host == _host && request.Method == "GET";
        }


        public WebResponse? HandleRequest(WebRequest request)
        {
            Log.Info(this, "Browser Handle Request: {0}", request.Uri);

            if (!_context.TryGetTarget(out var context))
                return null;

            var path = request.Uri!.LocalPath;
            if (path == "/")
                path = "index.html";

            var fullPath = Path.Join(_basePath, path);

            try
            {
                using var srcStream = context.Assets!.Open(fullPath);
                using var memStream = new MemoryStream();
                srcStream.CopyTo(memStream);

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
            catch (Exception ex)
            {
                Log.Warn(this, "Browser Handle Request exception: {0}", request.Uri);

                Log.Error(this, ex);

                return new WebResponse
                {
                    Code = 404
                };
            }

        }

        public string Scheme => "ui";

    }
}
