using XrEngine;

namespace XrEditor.Abstraction
{
    public class NativeImage
    {
        public object? Native { get; set; }

        public uint Width { get; set; }

        public uint Height { get; set; }
    }

    public interface IImageFactory
    {
        NativeImage CreateImage(Span<byte> data, uint width, uint height, TextureFormat format);
    }
}
