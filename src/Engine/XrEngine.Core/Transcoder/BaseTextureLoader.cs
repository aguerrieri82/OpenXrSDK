using XrEngine.Transcoder;

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

        public IList<TextureData> Read(string fileName, TextureLoadOptions? options = null)
        {
            using (var stream = File.OpenRead(fileName))
                return LoadTexture(stream, options);
        }

        public abstract IList<TextureData> LoadTexture(Stream stream, TextureLoadOptions? options = null);

        protected static AlignSize GetFormatAlign(TextureCompressionFormat comp, TextureFormat format)
        {
            var result = new AlignSize();

            if (comp == TextureCompressionFormat.Etc2)
            {
                result.AlignX = 4;
                result.AlignY = 4;

                if (format == TextureFormat.Rgba32 || format == TextureFormat.SRgba32)
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
                else if (format == TextureFormat.Rgb24)
                {
                    result.BitPerPixel = 24;
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

            static uint Align(uint value, uint align)
            {
                return (uint)MathF.Ceiling(value / (float)align) * align;
            }

            var results = new List<TextureData>();

            for (var mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                for (var face = 0; face < faceCount; face++)
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

                    stream.ReadExactly(item.Data.Span);

                    results.Add(item);
                }

            }

            return results;
        }

        public override EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            Log.Info(this, "Begin load texture '{0}'", uri);

            var fsPath = GetFilePath(uri);
            using var file = File.OpenRead(fsPath);
            var data = LoadTexture(file, (TextureLoadOptions?)options);

            var result = (Texture?)destObj;

            result ??= (Texture)Activator.CreateInstance(resType)!;

            result.LoadData(data);

            result.AddComponent(new AssetSource { Asset = new TextureAsset(this, uri, (TextureLoadOptions?)options) });

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
