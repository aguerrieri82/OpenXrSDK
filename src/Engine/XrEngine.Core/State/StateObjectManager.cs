namespace XrEngine
{
    public class StateObjectManager : ITypeStateManager<IStateObject?>
    {
        StateObjectManager() { }

        public IStateObject? Read(string key, IStateObject? destObj, Type objType, IStateContainer container)
        {
            if (!container.Contains(key))
                return null;

            var id = container.Read<ObjectId>(key);

            var refTable = container.Context.RefTable;

            if (!refTable.Resolved.TryGetValue(id, out var result))
            {
                result = refTable.Container!.ReadTypedObject<IStateObject>(id.ToString());
                refTable.Resolved[id] = result;
            }

            return (IStateObject)result;
        }

        public void Write(string key, IStateObject? value, IStateContainer container)
        {
            if (value == null)
                return;

            value.EnsureId();

            var refTable = container.Context.RefTable;
            var idKey = value.Id.ToString();

            if (!refTable.Container!.Contains(idKey))
                refTable.Container!.WriteTypedObject(idKey, value);

            container.Write(key, value.Id);
        }

        public static readonly StateObjectManager Instance = new();
    }
}
