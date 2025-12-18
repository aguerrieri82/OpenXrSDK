namespace XrEngine
{
    public class ObjectStateManager : ITypeStateManager<object?>
    {
        ObjectStateManager() { }

        public bool CanHandle(Type type)
        {
            return type.IsClass && type.HasEmptyConstructor();
        }

        public object? Read(string key, object? destObj, Type objType, IStateContainer container)
        {
            IStateContainer keyContainer = container.Enter(key);
            if (keyContainer == null)
                return null;
            object obj = destObj ?? Activator.CreateInstance(objType)!;
            keyContainer?.ReadObject(obj, objType);
            return obj;
        }

        public void Write(string key, object? obj, IStateContainer container)
        {
            if (obj != null)
                container.Enter(key).WriteObject(obj, obj.GetType());
        }

        public static readonly ObjectStateManager Instance = new();
    }
}
