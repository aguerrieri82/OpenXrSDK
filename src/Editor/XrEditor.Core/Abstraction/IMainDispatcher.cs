using XrEngine;

namespace XrEditor
{
    public interface IMainDispatcher : IDispatcher
    {
        void Execute(Action action, bool force = false);
    }
}
