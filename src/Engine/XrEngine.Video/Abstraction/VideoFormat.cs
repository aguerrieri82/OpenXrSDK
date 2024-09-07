namespace XrEngine.Video.Abstraction
{
    public enum ImageFormat
    {
        Unknown,
        Rgb24,
        Rgb32,
        YUY2,
        NV12,
        I420,
        YV12,
        IMC1,
        H264,
        MJPG
    }

    public struct VideoFormat
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public double FrameRate { get; set; }

        public short IsFlipV { get; set; }

        public ImageFormat ImageFormat { get; set; }

        public int ImageSize { get; set; }

        public int RowStride { get; set; }
    }
}
