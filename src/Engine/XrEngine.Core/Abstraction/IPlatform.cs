namespace XrEngine
{
    public class DeviceInfo
    {
        public string? Id { get; set; }

        public string? Name { get; set; }
    }

    public interface IPlatform
    {
        public DeviceInfo Device { get; }

        public string PersistentPath { get; }

        public string CachePath { get; }

        public string Name { get; }
    }

}
