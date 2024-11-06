using XrEngine;


namespace XrEditor
{
    public interface IViewManager
    {
        void AddView<T>(string path) where T : IModule;
    }
}
