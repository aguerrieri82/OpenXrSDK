namespace XrEngine
{
    public interface IObjectId
    {
        ObjectId Id { get; }

        void EnsureId();
    }
}
