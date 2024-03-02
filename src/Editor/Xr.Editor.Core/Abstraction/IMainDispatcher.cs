namespace Xr.Editor
{
    public interface IMainDispatcher
    {
        Task ExecuteAsync(Action action);

    }
}
