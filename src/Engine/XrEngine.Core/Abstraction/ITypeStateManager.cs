namespace XrEngine
{
    public interface ITypeStateManager
    {
        void Write(string key, object? obj, IStateContainer container, StateContext ctx);

        object? Read(string key, Type objType, IStateContainer container, StateContext ctx);

        bool CanHandle(Type type);
    }


    public interface ITypeStateManager<T> : ITypeStateManager
    {
        void Write(string key, T obj, IStateContainer container, StateContext ctx);

        new T Read(string key, Type objType, IStateContainer container, StateContext ctx);

        bool ITypeStateManager.CanHandle(Type type) =>
            typeof(T).IsAssignableFrom(type);

        object? ITypeStateManager.Read(string key, Type objType, IStateContainer container, StateContext ctx) =>
            Read(key, objType, container, ctx);

        void ITypeStateManager.Write(string key, object? obj, IStateContainer container, StateContext ctx) =>
            Write(key, (T)obj!, container, ctx);
    }
}
