namespace XrEngine
{
    public interface ITypeStateManager
    {
        void Write(string key, object? obj, IStateContainer container);

        object? Read(string key, object? destObj, Type objType, IStateContainer container);

        bool CanHandle(Type type);
    }


    public interface ITypeStateManager<T> : ITypeStateManager
    {
        void Write(string key, T obj, IStateContainer container);

        T Read(string key, T? curObject, Type objType, IStateContainer container);

        bool ITypeStateManager.CanHandle(Type type) =>
            typeof(T).IsAssignableFrom(type);

        object? ITypeStateManager.Read(string key, object? destObj, Type objType, IStateContainer container) =>
            Read(key, destObj == null ? default : (T)destObj, objType, container);

        void ITypeStateManager.Write(string key, object? obj, IStateContainer container) =>
            Write(key, (T)obj!, container);
    }

}
