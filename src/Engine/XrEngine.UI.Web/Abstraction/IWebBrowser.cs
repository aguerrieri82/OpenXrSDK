namespace XrEngine.UI.Web
{
    public class MessageReceivedArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }

    public interface IWebBrowser
    {
        Task NavigateAsync(string uri);

        Task PostMessageAsync(string message);

        event EventHandler<MessageReceivedArgs> MessageReceived;

        IWebRequestHandler? RequestHandler { get; set;  }
    }
}
