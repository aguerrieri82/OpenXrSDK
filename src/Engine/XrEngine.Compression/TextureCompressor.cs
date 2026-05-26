using System.Diagnostics;
using System.Security.Cryptography;

namespace XrEngine.Compression
{
    public struct TextureCompressionInfo
    {
        public Func<TextureData, TextureData> Encode;
        public int Align;
        public TextureCompressionFormat Format;
        public uint BlockSize;
    }

    public class TextureCompressor
    {
        readonly object _cacheLock = new object();
        readonly Dictionary<string, Task<IList<TextureData>>> _encodeTasks = [];
        readonly SemaphoreSlim _dictMutex = new(1, 1);
        readonly SemaphoreSlim _encodeLimit = new(3, 3);
        bool _cacheCleared;

        public static TextureCompressionInfo EncodeAstc(bool isNormalMap, float quality, uint blockSize)
        {
            return new TextureCompressionInfo
            {
                Format = TextureCompressionFormat.Astc,
                Align = 1,
                BlockSize = blockSize,
                Encode = data => AstcCompressor.Encode(data, isNormalMap, quality, blockSize)
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


        public async Task<IList<TextureData>> EncodeAsync(TextureData data, int mipsLevels, TextureCompressionInfo compressor)
        {
            var hash = TextureHash(data, compressor);

            Task<IList<TextureData>> task;

            await _dictMutex.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_encodeTasks.TryGetValue(hash, out task!))
                {
                    task = Task.Run(async () =>
                    {
                        await _encodeLimit.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            return Encode(data, mipsLevels, hash, compressor);
                        }
                        finally
                        {
                            _encodeLimit.Release();
                        }
                    });

                    _encodeTasks[hash] = task;
                }
            }
            finally
            {
                _dictMutex.Release();
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            finally
            {
                await _dictMutex.WaitAsync().ConfigureAwait(false);
                try
                {
                    _encodeTasks.Remove(hash);
                }
                finally
                {
                    _dictMutex.Release();
                }
            }
        }


        public static string TextureHash(TextureData data, TextureCompressionInfo compressor)
        {
            return Convert.ToHexString(MD5.HashData(data.Data!.AsSpan())) + "_" + compressor.Format + "_" + compressor.BlockSize + "_v6";
        }

        public void ClearCache()
        {
            if (_cacheCleared)
                return;
            foreach (var file in Directory.GetFiles(CachePath!))
                File.Delete(file);
            _cacheCleared = true;
        }

        public IList<TextureData> Encode(TextureData data, int mipsLevels, string? hash, TextureCompressionInfo compressor)
        {
            IList<TextureData>? result = null;

            var isCached = false;

            string? cacheFile = null;

            hash ??= TextureHash(data, compressor);

            if (CachePath != null)
            {
                cacheFile = Path.Combine(CachePath, hash + ".pvr");

                lock (_cacheLock)
                {
                    if (File.Exists(cacheFile))
                    {
                        using var readStream = File.OpenRead(cacheFile);
                        result = PvrTranscoder.Instance.LoadTexture(readStream);
                        isCached = true;
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

                    var resizeData = ImageUtils.ResizeV2(lastData, width, height);

                    Log.Info(this, "Compressing mip {0} mipsLevels width {1} height {2}", level, width, height);

                    var packData = ImageUtils.Pack(resizeData, compressor.Align);

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
                        using var writeStream = File.OpenWrite(cacheFile);
                        PvrTranscoder.Instance.SaveTexture(writeStream, result);
                    }
                }
            }

            Debug.Assert(result != null);

            foreach (var item in result)
            {
                if (mipsLevels == 0)
                    item.MipLevel = data.MipLevel;
                item.Face = data.Face;
                item.Depth = data.Depth;
            }

            return result;
        }


        public string? CachePath { get; set; }


        public static readonly TextureCompressor Instance = new();

    }
}
