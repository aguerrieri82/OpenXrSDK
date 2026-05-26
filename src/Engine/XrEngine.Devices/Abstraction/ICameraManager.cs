namespace XrEngine.Devices
{
    public class CameraDeviceInfo
    {
        public string? Id { get; internal set; }

        public int? Position { get; set; }

        public int? Facing { get; set; }

        public int? Source { get; set; }

        public string? Name { get; set; }
    }

    public interface ICameraManager
    {
        IList<CameraDeviceInfo> GetCameras();

        Task<ICameraDevice> OpenCameraAsync(string id);


    }
}

