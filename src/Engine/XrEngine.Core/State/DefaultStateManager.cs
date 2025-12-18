namespace XrEngine
{
    public class DefaultStateManager : ITypeStateManager<IStateManager>
    {
        DefaultStateManager() { }

        public IStateManager Read(string key, IStateManager? destObj, Type objType, IStateContainer container)
        {
            var obj = destObj ?? (IStateManager)Activator.CreateInstance(objType)!;
            obj.SetState(container.Enter(key));
            return obj;
        }

        public void Write(string key, IStateManager obj, IStateContainer container)
        {
            obj.GetState(container.Enter(key));
        }

        public static readonly DefaultStateManager Instance = new();
    }
}
