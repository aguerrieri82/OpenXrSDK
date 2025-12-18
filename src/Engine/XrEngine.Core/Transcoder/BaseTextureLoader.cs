using Common.Interop;

namespace XrEngine
{
    public abstract class BaseTextureLoader : BaseAssetLoader, ITextureLoader
    {
        protected struct AlignSize
        {
            public uint AlignX;

            public uint AlignY;

            public uint BitPerPixel;
        }



        public abstract IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null);

        protected static AlignSize GetFormatAlign(TextureCompressionFormat comp, TextureFormat format)
        {
            AlignSize result = new AlignSize();

            if (comp == TextureCompressionFormat.Etc2)
            {
                result.AlignX = 4;
                result.AlignY = 4;

                if (format == TextureFormat.Rgba32 || format == TextureFormat.SRgba32)
                    result.BitPerPixel = 8;
                else
                    result.BitPerPixel = 4; ;
            }
            else if (comp == TextureCompressionFormat.Etc1 || comp == TextureCompressionFormat.Bc1)
            {
                result.AlignX = 4;
                result.AlignY = 4;
                result.BitPerPixel = 4;
            }
            else if (comp == TextureCompressionFormat.Bc3 || comp == TextureCompressionFormat.Bc7)
            {
                result.AlignX = 4;
                result.AlignY = 4;
                result.BitPerPixel = 8;
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
                else if (format == TextureFormat.Rgb24)
                {
                    result.BitPerPixel = 24;
                }
                else if (format == TextureFormat.SRgba32 || format == TextureFormat.Rgba32)
                {
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
            AlignSize padding = GetFormatAlign(comp, format);

            static uint Align(uint value, uint align)
            {
                return (uint)MathF.Ceiling(value / (float)align) * align;
            }

            List<TextureData> results = new List<TextureData>();

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                for (int face = 0; face < faceCount; face++)
                {
                    TextureData item = new TextureData
                    {
                        Width = (uint)MathF.Max(1, width >> mipLevel),
                        Height = (uint)MathF.Max(1, height >> mipLevel),
                        MipLevel = (uint)mipLevel,
                        Format = format,
                        Face = (uint)face,
                        Compression = comp,
                    };

                    uint size = (Align(item.Width, padding.AlignX) * Align(item.Height, padding.AlignY) * padding.BitPerPixel) / 8;

                    item.Data = MemoryBuffer.Create<byte>(size);

                    stream.ReadExactly(item.Data.AsSpan());

                    results.Add(item);
                }

            }

            return results;
        }

        public override EngineObject LoadAsset(Uri uri, Type resType, EngineObject? dstObj, IAssetLoaderOptions? options = null)
        {
            Log.Info(this, "Begin load texture '{0}'", uri);

            string fsPath = GetFilePath(uri);
            using FileStream file = File.OpenRead(fsPath);
            IList<TextureData> data = LoadTexture(file, (TextureLoadOptions?)options);

            Texture? result = (Texture?)dstObj;

            result ??= (Texture)Activator.CreateInstance(resType)!;

            result.LoadData(data);

            result.AddComponent(new AssetSource(new TextureAsset(this, uri, (TextureLoadOptions?)options)));

            Log.Debug(this, "Texture loaded");

            return result;
        }

        protected override bool CanHandleExtension(string extension, out Type resType)
        {
            resType = typeof(Texture2D);
            return CanHandleExtension(extension);
        }

        protected abstract bool CanHandleExtension(string extension);

    }
}
