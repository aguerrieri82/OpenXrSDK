using Common.Interop;
using System.Numerics;
using XrEngine.Media;
using XrMath;

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

    [Flags]
    public enum CameraDeviceCaps
    {
        RenderOnTexture = 1
    }

    public class CameraParams
    {
        public Vector3? Position { get; set; }

        public Quaternion? Rotation { get; set; }

        public float[]? Intrinsic { get; set; }

        public Size2I? SensorSize { get; set; }
    }

    public interface ICameraDevice
    {
        IList<VideoFormat> GetSupportedFormats();

        Task StartCaptureAsync(VideoFormat format, Texture2D? outTexture = null, NativeSurface? outSurface = null);

        Task OpenAsync();

        CameraParams GetParams();

        void Close();

        void StopCapture();

        void UpdateTexture();


        event Action<CaptureImage>? NewImage;

        CameraDeviceCaps Caps { get; }

        NativeSurface Surface { get; }
    }

}

