
using System.Diagnostics;
using XrMath;

namespace XrEngine
{
    public class ImageLight : Light
    {
        private string? _cacheBasePath;

        private static readonly TextureLoadOptions _loaderOptions = new()
        {
            Format = TextureFormat.RgbaFloat32
        };

        public ImageLight()
        {
            Intensity = 3;
            Textures = new IBLTextures();
            LightTransform = Matrix3x3.Identity;
            UseCache = true;
        }

        protected bool LoadCacheTexture<T>(string fileName, Action<T> onLoad) where T : Texture
        {
            Debug.Assert(_cacheBasePath != null);

            string fullPath = Path.GetFullPath(Path.Combine(_cacheBasePath, fileName));
            if (!File.Exists(fullPath))
                return false;
            T texture = AssetLoader.Instance.Load<T>(fullPath);
            onLoad(texture);
            return true;
        }

        protected bool SaveCacheTexture<T>(string fileName, T? texture) where T : Texture
        {
            if (texture == null || _cacheBasePath == null)
                return false;

            Directory.CreateDirectory(_cacheBasePath!);

            string fullPath = Path.Combine(_cacheBasePath, fileName);

            IList<TextureData>? data = EngineApp.Current!.Renderer!.ReadTexture(texture, texture.Format, 0, null);
            if (data == null)
                return false;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            using FileStream file = File.OpenWrite(fullPath);

            PvrTranscoder.Instance.SaveTexture(file, data!);

            return true;
        }

        public void LoadPanorama(string hdrUri)
        {
            Uri uri = new Uri(hdrUri);
            BaseTextureLoader loader = (BaseTextureLoader)AssetLoader.Instance.GetLoader(uri);

            if (UseCache)
            {
                if (hdrUri.StartsWith("res://asset/"))
                {
                    string localPath = new Uri(hdrUri).LocalPath;
                    hdrUri = Context.Require<IAssetStore>().GetPath(localPath);
                }

                FileInfo info = new FileInfo(hdrUri);

                string baseName = $"{info.Name}_{info.Length}"; // _{info.LastWriteTime:yyyyMMddhhmmss}";

                _cacheBasePath = Path.Combine(Context.Require<IPlatform>().CachePath, "IBL", baseName);

                bool cacheValid = LoadCacheTexture<TextureCube>("lamb.pvr", a => Textures.LambertianEnv = a) &&
                                 LoadCacheTexture<TextureCube>("ggx.pvr", a => Textures.GGXEnv = a) &&
                                 LoadCacheTexture<TextureCube>("env.pvr", a => Textures.Env = a) &&
                                 LoadCacheTexture<Texture2D>("ggx_lut.pvr", a => Textures.GGXLUT = a);
                if (cacheValid)
                {
                    Textures.MipCount = Textures.GGXEnv!.MipLevelCount;
                    Panorama = new Texture2D();
                    Panorama.AddComponent(new AssetSource(new TextureAsset(loader, uri, _loaderOptions)));
                    return;
                }
            }

            Panorama = (Texture2D)loader.LoadAsset(uri, typeof(Texture2D), null, _loaderOptions);
            Panorama.NotifyChanged(ObjectChangeType.Render);

            NotifyChanged(ObjectChangeType.Render);
        }

        public void NotifyIBLCreated()
        {
            if (!string.IsNullOrWhiteSpace(_cacheBasePath))
            {
                SaveCacheTexture<TextureCube>("lamb.pvr", Textures!.LambertianEnv);
                SaveCacheTexture<TextureCube>("ggx.pvr", Textures!.GGXEnv);
                SaveCacheTexture<Texture2D>("ggx_lut.pvr", Textures!.GGXLUT);
                SaveCacheTexture<TextureCube>("env.pvr", Textures!.Env);
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(RotationY), RotationY);
            container.Write("Panorama", Panorama);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            RotationY = container.Read<float>(nameof(RotationY));
            Panorama = container.Read("Panorama", Panorama);
            if (Panorama != null)
                Panorama.NotifyChanged(ObjectChangeType.Render);
        }

        public override void Dispose()
        {
            Textures.Dispose();
            Panorama?.Dispose();
            Panorama = null;
            base.Dispose();
        }

        public IBLTextures Textures { get; set; }

        public Texture2D? Panorama { get; set; }

        [ValueType(ValueType.Radiant)]
        public float RotationY { get; set; }

        public Matrix3x3 LightTransform { get; set; }

        public static bool UseCache { get; set; } = true;
    }
}
