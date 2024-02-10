namespace OpenXr.Engine
{
    public enum TextureFormat
    {
        Depth32Float,
        Depth24Float
    }

    public enum WrapMode
    {
        ClampToEdge = 33071
    }

    public enum ScaleFilter
    {
        Nearest = 9728
    }


    public class Texture2D : Texture
    {
        public uint Width { get; set; }

        public uint Height { get; set; }

        public WrapMode WrapS { get; set; }

        public WrapMode WrapT { get; set; }

        public ScaleFilter MagFilter { get; set; }

        public ScaleFilter MinFilter { get; set; }

        public TextureFormat Format { get; set; }
    }
}
