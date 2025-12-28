namespace XrEngine.Media
{
    public enum VideoRecordFormat
    {
        Mp4,
    }

    public class VideoRecordOptions
    {
        public int BitRate { get; set; }

        public int FrameRate { get; set; }

        public int IFrameInterval { get; set; }

        public string? MimeType { get; set; }

        public VideoRecordFormat Format { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
