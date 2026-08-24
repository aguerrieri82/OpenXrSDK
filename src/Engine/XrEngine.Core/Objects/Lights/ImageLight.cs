
using System.Diagnostics;
using XrMath;

namespace XrEngine
{

    [StateManager(StateManagerMode.Explicit)]
    public class ImageLight : Light
    {
        private string? _cacheBasePath;

        private static readonly TextureLoadOptions _loaderOptions = new()
        {
            Format = TextureFormat.RgbaFloat16
        };

        public ImageLight()
        {
            Intensity = 3;
            Textures = new IBLTextures();
            LightTransform = Matrix3x3.Identity;
        }

        protected bool LoadCacheTexture<T>(string fileName, Action<T> onLoad) where T : Texture
        {
            Debug.Assert(_cacheBasePath != null);

            var fullPath = Path.GetFullPath(Path.Combine(_cacheBasePath, fileName));
            if (!File.Exists(fullPath))
                return false;
            var texture = AssetLoader.Instance.Load<T>(fullPath);
            onLoad(texture);
            return true;
        }

        protected bool SaveCacheTexture<T>(string fileName, T? texture) where T : Texture
        {
            if (texture == null || _cacheBasePath == null)
                return false;

            Directory.CreateDirectory(_cacheBasePath!);

            var fullPath = Path.Combine(_cacheBasePath, fileName);

            var data = EngineApp.Current.Renderer.ReadTexture(texture, texture.Format, 0, null);
            if (data == null)
                return false;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            using var file = File.OpenWrite(fullPath);

            PvrTranscoder.Instance.SaveTexture(file, data!);

            return true;
        }

        public void LoadPanorama(string hdrUri)
        {
            var uri = new Uri(hdrUri);
            var loader = (BaseTextureLoader)AssetLoader.Instance.GetLoader(uri);

            if (UseCache)
            {
                if (hdrUri.StartsWith("res://asset/"))
                {
                    var localPath = new Uri(hdrUri).LocalPath;
                    hdrUri = Context.Require<IAssetStore>().GetPath(localPath);
                }

                var info = new FileInfo(hdrUri);

                var baseName = $"{info.Name}_{info.Length}"; // _{info.LastWriteTime:yyyyMMddhhmmss}";

                _cacheBasePath = Path.Combine(Context.Require<IPlatform>().CachePath, "IBL", baseName);

                var cacheValid = LoadCacheTexture<TextureCube>("lamb.pvr", a => Textures.LambertianEnv = a) &&
                                 LoadCacheTexture<TextureCube>("ggx.pvr", a => Textures.GGXEnv = a) &&
                                 LoadCacheTexture<Texture2D>("ggx_lut.pvr", a => Textures.GGXLUT = a) &&
                                 LoadCacheTexture<TextureCube>("charlie.pvr", a => Textures.CharlieEnv = a) &&
                                 LoadCacheTexture<Texture2D>("charlie_lut.pvr", a => Textures.CharlieLUT = a) &&
                                 LoadCacheTexture<TextureCube>("env.pvr", a => Textures.Env = a);
                if (cacheValid)
                {
                    Textures.GGXLUT!.NeverCompress = true;
                    Textures.CharlieLUT!.NeverCompress = true;
                    Textures.MipCount = Textures.GGXEnv!.MipLevelCount;

                    Panorama = new Texture2D();
                    Panorama.AddComponent(new AssetSource(new TextureAsset(loader, uri, _loaderOptions)));
                    Panorama.Name = "Ibl Panorama";

                    Textures.Env?.Name = "Ibl Env";
                    Textures.GGXEnv?.Name = "Ibl GGXEnv";
                    Textures.CharlieEnv?.Name = "Ibl CharlieEnv";
                    Textures.LambertianEnv?.Name = "Ibl LambertianEnv";
                    Textures.GGXLUT?.Name = "Ibl GGXLut";
                    Textures.CharlieLUT?.Name = "Ibl CharlieLut";
                    return;
                }
            }

            Panorama = (Texture2D)loader.LoadAsset(uri, typeof(Texture2D), null, _loaderOptions);
            Panorama.NotifyChanged(ChangeType.Render);

            NotifyChanged(ChangeType.Render);
        }

        public void NotifyIBLCreated()
        {
            if (!string.IsNullOrWhiteSpace(_cacheBasePath))
            {
                SaveCacheTexture("lamb.pvr", Textures!.LambertianEnv);
                SaveCacheTexture("ggx.pvr", Textures!.GGXEnv);
                SaveCacheTexture("ggx_lut.pvr", Textures!.GGXLUT);
                SaveCacheTexture("charlie.pvr", Textures!.CharlieEnv);
                SaveCacheTexture("charlie_lut.pvr", Textures!.CharlieLUT);
                SaveCacheTexture("env.pvr", Textures!.Env);
            }
            Textures.GGXLUT?.NeverCompress = true;
            Textures.CharlieLUT?.NeverCompress = true;
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

        [ValueType(ValueType.Radiant), SaveState]
        public float RotationY { get; set; }

        [SaveState]
        public float ShadowStrength { get; set; }

        public Matrix3x3 LightTransform { get; set; }

        public static bool UseCache { get; set; } = false;
    }
}
