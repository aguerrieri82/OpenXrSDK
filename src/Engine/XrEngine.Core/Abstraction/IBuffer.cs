namespace XrEngine
{
    public interface IBuffer
    {
        void Update(object value);

        string Hash { get; set; }
    }

}
