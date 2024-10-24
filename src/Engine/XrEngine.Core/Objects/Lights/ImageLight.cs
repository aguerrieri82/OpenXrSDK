﻿
using System.Diagnostics;
using XrEngine.Services;

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
            UseCache = true;
            Textures = new IBLTextures();
        }

        protected bool LoadCacheTexture<T>(string fileName, Action<T> onLoad) where T : Texture
        {
            Debug.Assert(_cacheBasePath != null);

            var fullPath = Path.Combine(_cacheBasePath, fileName);
            if (!File.Exists(fullPath))
                return false;
            var texture = AssetLoader.Instance.Load<T>(fullPath);
            onLoad(texture);
            return true;
        }

        protected bool SaveCacheTexture<T>(string fileName, Texture? texture) where T : Texture
        {
            if (texture == null || _cacheBasePath == null)
                return false;

            Directory.CreateDirectory(_cacheBasePath!);

            var fullPath = Path.Combine(_cacheBasePath, fileName);

            var data = EngineApp.Current!.Renderer!.ReadTexture(texture, texture.Format, 0, null);
            if (data == null)
                return false;

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

                var baseName = $"{info.Name}_{info.Length}_{info.LastWriteTime:yyyyMMddhhmmss}";

                _cacheBasePath = Path.Combine(Context.Require<IPlatform>().CachePath, "IBL", baseName);

                var cacheValid = LoadCacheTexture<TextureCube>("lamb.pvr", a => Textures.LambertianEnv = a) &&
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
            Panorama.Version = DateTime.Now.Ticks;

            NotifyChanged(ObjectChangeType.Render);
        }

        public void NotifyIBLCreated()
        {
            if (!string.IsNullOrWhiteSpace(_cacheBasePath))
            {
                SaveCacheTexture<TextureCube>("lamb.pvr", Textures!.LambertianEnv);
                SaveCacheTexture<TextureCube>("ggx.pvr", Textures!.GGXEnv);
                SaveCacheTexture<TextureCube>("ggx_lut.pvr", Textures!.GGXLUT);
                SaveCacheTexture<TextureCube>("env.pvr", Textures!.Env);
            }
        }

        public override void GetState(IStateContainer container)
        {
            base.GetState(container);
            container.Write(nameof(Rotation), Rotation);
            container.Write("Panorama", Panorama);
        }

        protected override void SetStateWork(IStateContainer container)
        {
            base.SetStateWork(container);
            Rotation = container.Read<float>(nameof(Rotation));
            Panorama = container.Read("Panorama", Panorama);
            if (Panorama != null)
                Panorama.Version = DateTime.Now.Ticks;
        }

        public IBLTextures Textures { get; set; }

        public Texture2D? Panorama { get; set; }

        [ValueType(ValueType.Radiant)]
        public float Rotation { get; set; }

        public bool UseCache { get; set; }
    }
}
