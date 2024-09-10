using CefSharp;
using XrEngine.UI.Web;


namespace XrEngine.Browser.Win
{
    internal static class ChromeExtensions
    {
        public static WebRequest ToWebRequest(this IRequest request)
        {
            var headers = new Dictionary<string, string>();
            foreach (var key in request.Headers.AllKeys)
                headers[key] = request.Headers[key].ToString();

            return new WebRequest
            {
                Method = request.Method,
                Uri = new Uri(request.Url),
                Headers = headers
            };
        }
    }
}
