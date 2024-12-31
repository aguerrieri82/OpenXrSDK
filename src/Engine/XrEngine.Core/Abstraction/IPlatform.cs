namespace XrEngine
{
    public class DeviceInfo
    {
        public string? Id { get; set; }

        public string? Name { get; set; }
    }

    public interface IPlatform
    {
        DeviceInfo Device { get; }

        string PersistentPath { get; }

        string CachePath { get; }

        string SharedPath => PersistentPath;

        public string Name { get; }
    }

}
