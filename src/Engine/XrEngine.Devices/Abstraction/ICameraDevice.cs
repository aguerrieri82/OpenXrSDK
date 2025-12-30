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

        public float Fx => Intrinsic?[0] ?? 0;

        public float Fy => Intrinsic?[1] ?? 0;

        public float Cx => Intrinsic?[2] ?? 0;

        public float Cy => Intrinsic?[3] ?? 0;

        public Vector2 Center => new(Cx, Cy);

        public Vector2 Fov => new(Fx, Fy);

        public Pose3 GetLensPose()
        {
            var worldRot = Quaternion.Inverse(Rotation!.Value);
            var sensorFix = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI);

            return new Pose3()
            {
                Position = Position!.Value,
                Orientation = worldRot * sensorFix
            };
        }

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

        NativeSurface FrameSurface { get; }

        long LastFrame { get; }

        long LastTimestamp { get; }
    }

}

