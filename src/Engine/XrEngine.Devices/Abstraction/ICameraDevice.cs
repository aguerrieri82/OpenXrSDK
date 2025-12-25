using Common.Interop;
using XrEngine.Media;

namespace XrEngine.Devices
{
    public struct CaptureImage
    {
        public object? Native;

        public long TimeStamp;

        public Action<IMemoryBuffer<byte>>? GetData;

        public int Width;

        public int Height;

        public ImageFormat Format;
    }

    public interface ICameraDevice
    {
        IList<VideoFormat> GetSupportedFormats();

        Task StartCaptureAsync(VideoFormat format, Texture2D? outTexture = null);

        Task OpenAsync();

        void Close();

        void StopCapture();

        void UpdateTexture();


        event Action<CaptureImage>? NewImage;

        bool CanRenderOnTexture { get; }
    }

}

