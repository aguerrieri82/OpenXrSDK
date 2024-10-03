namespace XrEngine.UI.Web
{
    public static class MimeMapper
    {
        public static string GetMimeType(string ext)
        {
            return ext switch
            {
                ".js" => "application/javascript",
                ".html" => "text/html",
                ".css" => "text/css",
                _ => "application/octect-stream"
            };
        }
    }
}
