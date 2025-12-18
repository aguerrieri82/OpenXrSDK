namespace XrEngine
{
    public class StateObjectManager<T> : ITypeStateManager<T?> where T : IStateObject
    {
        public StateObjectManager() { }

        public virtual T? Read(string key, T? destObj, Type objType, IStateContainer container)
        {
            if (!container.Contains(key))
                return default;

            ObjectId id = container.Read<ObjectId>(key);

            RefTable refTable = container.Context.RefTable;

            if (!refTable.Resolved.TryGetValue(id, out object? result))
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

            RefTable refTable = container.Context.RefTable;
            string idKey = value.Id.ToString();

            if (!refTable.Container!.Contains(idKey))
                refTable.Container!.WriteTypedObject(idKey, value);

            container.Write(key, value.Id);
        }
    }
}
