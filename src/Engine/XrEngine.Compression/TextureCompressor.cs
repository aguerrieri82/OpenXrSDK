using System.Diagnostics;
using System.Security.Cryptography;
using XrEngine.Helpers;

namespace XrEngine.Compression
{
    public struct TextureCompressionInfo
    {
        public Func<TextureData, TextureData> Encode;
        public int Align;
        public TextureCompressionFormat Format;
        public uint BlockSize;
        public bool RequireRgba;
    }

    public class TextureCompressor
    {
        readonly object _cacheLock = new();
        readonly AsyncTaskDispatcher _dispatcher = new AsyncTaskDispatcher(3, ThreadPriority.Lowest);
        bool _cacheCleared;

        public static TextureCompressionInfo EncodeAstc(bool isNormalMap, float quality, uint blockSize, int threadPriority)
        {
            return new TextureCompressionInfo
            {
                Format = TextureCompressionFormat.Astc,
                Align = 1,
                BlockSize = blockSize,
                RequireRgba = true,
                Encode = data => AstcCompressor.Encode(data, isNormalMap, quality, blockSize, threadPriority)
            };
        }

        public static TextureCompressionInfo EncodeEtc2()
        {
            return new TextureCompressionInfo
            {
                Format = TextureCompressionFormat.Etc2,
                Align = 4,
                BlockSize = 0,
                Encode = EtcCompressor.Encode
            };
        }

        public Task<IList<TextureData>> EncodeAsync(TextureData data, int mipsLevels, TextureCompressionInfo compressor, uint handle)
        {
            var hash = TextureHash(data, compressor, mipsLevels);

            return _dispatcher.ExecuteAsync(() => Encode(data, mipsLevels, hash, compressor, handle), hash);
        }

        public static string TextureHash(TextureData data, TextureCompressionInfo compressor, int mipsLevels)
        {
            var dataHash = HashBuilder.Instance.Compute(data.Data!.AsSpan());

            return $"{dataHash:X16}_{compressor.Format}_{compressor.BlockSize}_{mipsLevels}_v7";
        }

        public void ClearCache()
        {
            if (_cacheCleared)
                return;

            if (!Directory.Exists(CachePath))
                return;

            foreach (var file in Directory.GetFiles(CachePath))
                File.Delete(file);
            _cacheCleared = true;
        }

        public IList<TextureData> Encode(TextureData data, int mipsLevels, string? hash, TextureCompressionInfo compressor, uint handle)
        {
            IList<TextureData>? result = null;

            var isCached = false;

            string? cacheFile = null;

            string? validFile = null;

            hash ??= TextureHash(data, compressor, mipsLevels);

            if (CachePath != null)
            {
                //ClearCache();

                cacheFile = Path.Combine(CachePath, hash + ".pvr");
                validFile = cacheFile + ".valid";

                lock (_cacheLock)
                {
                    if (File.Exists(cacheFile) && File.Exists(validFile))
                    {
                        try
                        {
                            using var readStream = File.OpenRead(cacheFile);
                            result = PvrTranscoder.Instance.LoadTexture(readStream);
                            isCached = true;
                            Log.Debug(this, "Loaded from cache: {0} - {1}", handle, Path.GetFileName(cacheFile));
                        }
                        catch (Exception ex)
                        {
                            Log.Warn(this, "Invalid compression cache '{0}':\n{1}", cacheFile, ex);
                            File.Delete(validFile);
                        }
                    }
                }
            }

            if (!isCached)
            {
                result = new List<TextureData>();

                var level = 0;

                var lastData = data;

                while (true)
                {
                    var width = (int)MathF.Max(1, data.Width >> level);
                    var height = (int)MathF.Max(1, data.Height >> level);

                    var resizeData = ImageUtils.Resize(lastData, width, height);

                    Log.Info(this, "Compressing mip {0} mipsLevels width {1} height {2}", level, width, height);

                    TextureData packData;

                    if (compressor.RequireRgba)
                    {
                        if (resizeData.Format == TextureFormat.Rgba16)
                        {
                            Debug.Assert(compressor.Align == 1);
                            packData = ImageUtils.ConvertRgba16ToRgba32F(resizeData);
                        }
                        else if (resizeData.Format == TextureFormat.RgbFloat32)
                        {
                            Debug.Assert(compressor.Align == 1);
                            packData = ImageUtils.ConvertRgb32FToRgba16F(resizeData);
                        }
                        else
                            packData = ImageUtils.PackToRgba8(resizeData, compressor.Align);
                    }
                    else
                        packData = ImageUtils.Pack(resizeData, compressor.Align);

                    var newData = compressor.Encode(packData);

                    newData.MipLevel = (uint)level;
                    newData.Width = resizeData.Width;
                    newData.Height = resizeData.Height;

                    lastData = resizeData;

                    result.Add(newData);

                    if (level >= mipsLevels || newData.Width <= 4 || newData.Height <= 4)
                        break;

                    level++;
                }

                if (cacheFile != null)
                {
                    lock (_cacheLock)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);

                        if (File.Exists(cacheFile))
                            File.Delete(cacheFile);

                        if (File.Exists(validFile))
                            File.Delete(validFile);

                        using var writeStream = File.OpenWrite(cacheFile);

                        PvrTranscoder.Instance.SaveTexture(writeStream, result);

                        writeStream.Close();

                        File.WriteAllText(validFile!, "");
                    }
                }
            }

            Debug.Assert(result != null);

            foreach (var item in result)
            {
                if (mipsLevels == 0)
                    item.MipLevel = data.MipLevel;
                item.Layer = data.Layer;
                item.Depth = data.Depth;
            }

            return result;
        }

        public string? CachePath { get; set; }

        public static readonly TextureCompressor Instance = new();

    }
}
