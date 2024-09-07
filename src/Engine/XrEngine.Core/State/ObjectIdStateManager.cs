namespace XrEngine
{
    public class ObjectIdStateManager : ITypeStateManager<ObjectId>
    {
        ObjectIdStateManager() { }

        public ObjectId Read(string key, ObjectId destObj, Type objType, IStateContainer container)
        {
            return new ObjectId() { Value = container.Read<Guid>(key) };
        }

        public void Write(string key, ObjectId obj, IStateContainer container)
        {
            if (obj.Value == Guid.Empty)
                return;
            container.Write(key, obj.Value);
        }

        public static readonly ObjectIdStateManager Instance = new();
    }
}
