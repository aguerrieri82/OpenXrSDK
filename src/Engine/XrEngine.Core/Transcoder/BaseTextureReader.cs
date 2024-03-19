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

        public abstract IList<TextureData> Read(Stream stream);

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
                if (format == TextureFormat.RgbFloat)
                {
                    result.AlignX = 1;
                    result.AlignY = 1;
                    result.BitPerPixel = 32;
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

            for (var m = 0; m < mipCount; m++)
            {
                for (var f = 0 ; f < faceCount; f++)
                {
                    var item = new TextureData
                    {
                        Width = (uint)MathF.Max(1, width >> m),
                        Height = (uint)MathF.Max(1, height >> m),
                        MipLevel = (uint)m,
                        Format = format,
                        Face = (uint)f,
                        Compression = comp,
                    };

                    var size = (Align(item.Width, padding.AlignX) * Align(item.Height, padding.AlignY) * padding.BitPerPixel) / 8;

                    item.Data = new byte[size];

                    var totRead = stream.Read(item.Data);

                    results.Add(item);

                    if (totRead != item.Data.Length)
                        break;
                }
             
            }

            return results;
        }
    }
}
