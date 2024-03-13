namespace XrEngine
{
    public interface IBuffer
    {
        void Update(object value);

        long Version { get; set; }
    }

}
