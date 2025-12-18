namespace XrEngine
{
    public class StateObjectManager<T> : ITypeStateManager<T?> where T : IStateObject
    {
        public StateObjectManager() { }

        public virtual T? Read(string key, T? destObj, Type objType, IStateContainer container)
        {
            if (!container.Contains(key))
                return default;

            var id = container.Read<ObjectId>(key);

            var refTable = container.Context.RefTable;

            if (!refTable.Resolved.TryGetValue(id, out var result))
            {
                if (destObj == null)
                    result = refTable.Container!.CreateTypedObject<IStateObject>(id.ToString())!;
                else
                {
                    result = destObj;
                    destObj.SetState(refTable.Container!.Enter(id.ToString()));
                }

                refTable.Resolved[id] = result;
            }

            return (T)result;
        }

        public virtual void Write(string key, T? value, IStateContainer container)
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
    }
}
