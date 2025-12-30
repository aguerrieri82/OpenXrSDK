using System.Windows.Media;
using System.Windows.Media.Imaging;
using XrEditor.Abstraction;
using XrEngine;

namespace XrEditor.Services
{
    public class WpfImageFactory : IImageFactory
    {
        public NativeImage CreateImage(Span<byte> data, uint width, uint height, TextureFormat format)
        {
            PixelFormat pFormat;
            int stride;

            if (format == TextureFormat.GrayInt8)
            {
                pFormat = PixelFormats.Gray8;
                stride = (int)width;

            }
            else if (format == TextureFormat.Rgba32)
            {
                pFormat = PixelFormats.Bgra32;
                stride = (int)width * 4;
            }
            else if (format == TextureFormat.Rgb24)
            {
                pFormat = PixelFormats.Rgb24;
                stride = (int)width * 3;
            }
            else
                throw new NotSupportedException();

            var bitmap = BitmapSource.Create(
                (int)width,
                (int)height,
                96.0,
                96.0,
                pFormat,
                null,
                data.ToArray(),
                stride
            );

            bitmap.Freeze();

            return new NativeImage
            {
                Native = bitmap,
                Width = width,
                Height = height
            };
        }
    }
}
