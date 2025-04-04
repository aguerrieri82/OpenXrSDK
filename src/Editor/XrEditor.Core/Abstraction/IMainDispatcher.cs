using XrEngine;

namespace XrEditor
{
    public interface IMainDispatcher
    {
        Task ExecuteAsync(Action action);

        void Execute(Action action);
    }
}
