namespace XrEngine
{
    public interface ILayer3DItem
    {
        ObjectId Id { get; }

        void EnsureId();
    }
}
