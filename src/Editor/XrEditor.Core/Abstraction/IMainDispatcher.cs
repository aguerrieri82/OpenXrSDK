namespace XrEditor
{
    public interface IMainDispatcher
    {
        Task ExecuteAsync(Action action);

    }
}
