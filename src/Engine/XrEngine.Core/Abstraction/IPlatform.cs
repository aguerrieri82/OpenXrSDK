namespace XrEngine
{
    public interface IPlatform
    {
        public string PersistentPath { get; }

        public string CachePath { get; }

        public string Name { get; }
    }

}
