using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static OpenXr.Engine.PvrReader;

namespace OpenXr.Engine
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
            if (comp == TextureCompressionFormat.Etc2)
            {
                var result = new   AlignSize();
                result.AlignX = 4;
                result.AlignY = 4;

                if (format == TextureFormat.Rgba32)
                    result.BitPerPixel = 8;
                else
                    result.BitPerPixel = 4;

                return result;
            }

            throw new NotSupportedException();
        }

        protected static IList<TextureData> ReadMips(Stream stream, uint width, uint height, uint mipCount, TextureCompressionFormat comp, TextureFormat format)
        {
            var padding = GetFormatAlign(comp, format);

            uint Align(uint value, uint align)
            {
                return (uint)Math.Ceiling(value / (float)align) * align;
            }

            var results = new List<TextureData>();

            for (var i = 0; i < mipCount; i++)
            {
                var item = new TextureData
                {
                    Width = Math.Max(1, width >> i),
                    Height = Math.Max(1, height >> i),
                    MipLevel = (uint)i,
                    Format = format,
                    Compression = comp,
                };

                var size = (Align(item.Width, padding.AlignX) * Align(item.Height, padding.AlignY) * padding.BitPerPixel) / 8;

                item.Data = new byte[size];

                var totRead = stream.Read(item.Data);
                if (totRead != item.Data.Length)
                    break;

                results.Add(item);
            }

            return results;
        }
    }
}
