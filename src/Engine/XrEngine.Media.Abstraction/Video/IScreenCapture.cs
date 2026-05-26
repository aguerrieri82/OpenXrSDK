namespace XrEngine.Media
{
    public class ScreenCaptureOptions
    {
        public uint Width { get; set; }

        public uint Height { get; set; }

        public NativeSurface OutSurface { get; set; }
    }

    public interface IScreenCapture
    {
        Task<bool> StartCaptureAsync(ScreenCaptureOptions options);

        void StopCapture();

    }
}
