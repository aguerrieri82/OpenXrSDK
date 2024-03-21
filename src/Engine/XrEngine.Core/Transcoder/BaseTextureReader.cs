namespace XrEngine
{
    public abstract class BaseTextureReader : ITextureReader
    {
        protected struct AlignSize
        {
            public uint AlignX;

            public uint AlignY;

            public uint BitPerPixel;
        }

        public IList<TextureData> Read(string fileName, TextureReadOptions? options = null)
        {
            using (var stream = File.OpenRead(fileName))
                return Read(stream, options);
        }
        
        public abstract IList<TextureData> Read(Stream stream, TextureReadOptions? options = null);

        protected static AlignSize GetFormatAlign(TextureCompressionFormat comp, TextureFormat format)
        {
            var result = new AlignSize();

            if (comp == TextureCompressionFormat.Etc2)
            {
                result.AlignX = 4;
                result.AlignY = 4;

                if (format == TextureFormat.Rgba32)
                    result.BitPerPixel = 8;
                else
                    result.BitPerPixel = 4; ;
            }
            else if (comp == TextureCompressionFormat.Etc1)
            {
                result.AlignX = 4;
                result.AlignY = 4;
                result.BitPerPixel = 4;
            }
            else if (comp == TextureCompressionFormat.Uncompressed)
            {
                result.AlignX = 1;
                result.AlignY = 1;

                if (format == TextureFormat.RgbFloat32)
                {
                    result.BitPerPixel = 96;
                }
                else if (format == TextureFormat.RgbaFloat32)
                {
                    result.BitPerPixel = 128;
                }
                else if (format == TextureFormat.RgbaFloat16)
                {
                    result.BitPerPixel = 64;
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();

            return result;
        }

        protected static IList<TextureData> ReadData(Stream stream, uint width, uint height, uint mipCount, uint faceCount, TextureCompressionFormat comp, TextureFormat format)
        {
            var padding = GetFormatAlign(comp, format);

            uint Align(uint value, uint align)
            {
                return (uint)MathF.Ceiling(value / (float)align) * align;
            }

            var results = new List<TextureData>();

            for (var mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                for (var face = 0 ; face < faceCount; face++)
                {
                    var item = new TextureData
                    {
                        Width = (uint)MathF.Max(1, width >> mipLevel),
                        Height = (uint)MathF.Max(1, height >> mipLevel),
                        MipLevel = (uint)mipLevel,
                        Format = format,
                        Face = (uint)face,
                        Compression = comp,
                    };

                    var size = (Align(item.Width, padding.AlignX) * Align(item.Height, padding.AlignY) * padding.BitPerPixel) / 8;

                    item.Data = new byte[size];

                    var totRead = stream.Read(item.Data.Span);

                    results.Add(item);

                    if (totRead != item.Data.Length)
                        break;
                }
             
            }

            return results;
        }
    }
}
