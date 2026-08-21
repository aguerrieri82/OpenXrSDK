using XrEngine;

namespace XrEditor
{
    public interface IMainDispatcher : IDispatcher
    {
        bool IsActive { get; }

        void Execute(Action action, bool force = false);
    }
}
