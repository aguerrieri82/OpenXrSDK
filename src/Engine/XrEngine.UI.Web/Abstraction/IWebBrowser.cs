namespace XrEngine.UI.Web
{
    public class MessageReceivedArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }

    public interface IWebBrowser
    {
        Task PostMessageAsync(string message);

        event EventHandler<MessageReceivedArgs> MessageReceived;
    }
}
