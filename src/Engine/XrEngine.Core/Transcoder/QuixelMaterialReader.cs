
using Common.Interop;
using System.Globalization;
using System.Text.Json;
using XrMath;

#pragma warning disable CS8618

namespace XrEngine
{
    public class QuixelMaterialReader : BaseAssetLoader
    {
        private readonly Dictionary<string, IPbrMaterial> _materialCache = [];


        #region QuixelMaterialInfo

        protected class QuixelMaterialInfo
        {
            public class PackInfo
            {
                public string _id { get; set; }
                public string name { get; set; }
            }

            public class SemanticTagsInfo
            {
                public string name { get; set; }
                public string asset_type { get; set; }
                public string[] contains { get; set; }
                public string[] theme { get; set; }
                public string[] descriptive { get; set; }
                public string[] environment { get; set; }
                public string[] orientation { get; set; }
                public object architectural_style { get; set; }
                public string[] state { get; set; }
                public string country { get; set; }

                public CategoryInfo states { get; set; }

                public string region { get; set; }
                public string[] color { get; set; }
                public float maxSize { get; set; }
                public float minSize { get; set; }
                public string subject_matter { get; set; }
                public string[] interior_exterior { get; set; }
                public string[] industry { get; set; }
            }

            public class ImageInfo
            {
                public int contentLength { get; set; }
                public string resolution { get; set; }
                public string uri { get; set; }
                public string[] tags { get; set; }
            }

            public class PreviewInfo
            {
                public ImageInfo[] images { get; set; }
                public object[] scaleReferences { get; set; }
                public string relativeSize { get; set; }
            }

            public class MapInfo
            {
                public string mimeType { get; set; }
                public float minIntensity { get; set; }
                public int bitDepth { get; set; }
                public string name { get; set; }
                public string resolution { get; set; }
                public int contentLength { get; set; }
                public string colorSpace { get; set; }
                public string uri { get; set; }
                public string physicalSize { get; set; }
                public int maxIntensity { get; set; }
                public string type { get; set; }
                public string averageColor { get; set; }
            }

            public class MetaInfo
            {
                public string key { get; set; }
                public string name { get; set; }
                public object value { get; set; }
            }

            public class CategoryInfo : Dictionary<string, CategoryInfo>
            {

            }


            public class ReferencePreviewInfo
            {
                public MapInfo[] maps { get; set; }
            }

            public PackInfo pack { get; set; }
            public SemanticTagsInfo semanticTags { get; set; }
            public string[] tags { get; set; }
            public PreviewInfo previews { get; set; }
            public MapInfo[] maps { get; set; }
            public int points { get; set; }
            public MetaInfo[] meta { get; set; }
            public string[] categories { get; set; }
            public int version { get; set; }
            public object[] references { get; set; }
            public ReferencePreviewInfo referencePreviews { get; set; }
            public string averageColor { get; set; }
            public string name { get; set; }
            public int comp_version { get; set; }
            public CategoryInfo assetCategories { get; set; }
            public int revision { get; set; }
            public bool revised { get; set; }
            public string id { get; set; }
            public string physicalSize { get; set; }
        }

        #endregion

        #region MateriaInfo
        public enum TextureType
        {
            None,
            Albedo,
            Roughness,
            AmbientOcclusion,
            Normal,
            Metallic
        }

        public class TextureInfo
        {
            public TextureType Type { get; set; }

            public Size2 PhysicalSize { get; set; }

            public bool CanTile { get; set; }

            public string? FileName { get; set; }

            public bool IsSrgb { get; set; }

            public string? MimeType { get; set; }
        }

        public class MaterialInfo
        {
            public string? Id { get; set; }

            public string? DisplayName { get; set; }

            public IList<TextureInfo>? Textures { get; set; }

        }

        #endregion

        protected override bool CanHandleExtension(string extension, out Type resType)
        {
            resType = typeof(Material);
            return extension == ".json";
        }

        protected MaterialInfo Parse(string fileName)
        {

            var matInfo = JsonSerializer.Deserialize<QuixelMaterialInfo>(File.ReadAllText(fileName))!;

            var prevImage = matInfo.previews.images.First(a => a.tags.Contains("sidepanel")).uri;

            var info = new MaterialInfo()
            {
                DisplayName = matInfo.semanticTags.name,
                Id = matInfo.id,
                Textures = []
            };

            foreach (var map in matInfo.maps)
            {
                if (map.mimeType != "image/jpeg")
                    continue;

                if (map.resolution != "2048x2048")
                    continue;

                var type = map.type switch
                {
                    "ao" => TextureType.AmbientOcclusion,
                    "albedo" => TextureType.Albedo,
                    "normal" => TextureType.Normal,
                    "roughness" => TextureType.Roughness,
                    "metalness" => TextureType.Metallic,
                    _ => TextureType.None
                };

                if (type == TextureType.None)
                    continue;

                var tile = matInfo.meta.FirstOrDefault(a => a.key == "tileable")?.value?.ToString();

                var texInfo = new TextureInfo()
                {
                    Type = type,
                    MimeType = map.mimeType,
                    CanTile = tile == "True",
                    IsSrgb = map.colorSpace == "sRGB",
                    FileName = map.uri
                };

                if (!string.IsNullOrWhiteSpace(map.physicalSize))
                {
                    var parts = map.physicalSize.Split('x');
                    texInfo.PhysicalSize = new Size2(
                        float.Parse(parts[0], CultureInfo.InvariantCulture),
                        float.Parse(parts[1], CultureInfo.InvariantCulture));
                }

                info.Textures.Add(texInfo);
            }

            return info;
        }

        public override EngineObject LoadAsset(Uri uri, Type assetType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            var fileName = GetFilePath(uri);

            if (!_materialCache.TryGetValue(fileName, out var result))
            {
                result = MaterialFactory.CreatePbr(Color.White);
                result.Roughness = 0.7f;

                IMemoryBuffer<byte>? mrImage = null;

                var mat = Parse(fileName);

                foreach (var texture in mat.Textures!)
                {
                    var localPath = uri.LocalPath;
                    var texPath = Path.Join(Path.GetDirectoryName(localPath), texture.FileName!).Replace('\\', '/');
                    var texUri = $"{uri.Scheme}://{uri.Host}{texPath}";

                    var tex2D = AssetLoader.Instance.Load<Texture2D>(texUri, new TextureLoadOptions
                    {
                        Format = texture.IsSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32,
                        MimeType = texture.MimeType
                    });

                    tex2D.MinFilter = ScaleFilter.LinearMipmapLinear;
                    tex2D.MagFilter = ScaleFilter.Linear;

                    if (texture.CanTile)
                    {
                        tex2D.WrapT = WrapMode.Repeat;
                        tex2D.WrapS = WrapMode.Repeat;
                    }

                    if (texture.PhysicalSize.Width != 0)
                        tex2D.Transform = Matrix3x3.CreateScale(1f / texture.PhysicalSize.Width, 1f / texture.PhysicalSize.Height);

                    if (texture.Type == TextureType.Albedo)
                        result.ColorMap = tex2D;

                    else if (texture.Type == TextureType.AmbientOcclusion)
                    {
                        result.OcclusionMap = tex2D;
                        //tex2D.Format = TextureFormat.Gray8;
                    }


                    else if (texture.Type == TextureType.Normal)
                    {
                        result.NormalMap = tex2D;
                        result.NormalScale = 1;
                        //tex2D.Format = TextureFormat.Rgb24;
                    }


                    else if (texture.Type == TextureType.Roughness ||
                             texture.Type == TextureType.Metallic)
                    {
                        mrImage ??= MemoryBuffer.Create<byte>(tex2D.Width * tex2D.Height * 4);

                        uint ofs = texture.Type == TextureType.Roughness ? 1u : 2u;

                        using var pSrc = tex2D.Data![0].Data!.MemoryLock();
                        using var pDst = mrImage.MemoryLock();

                        EngineNativeLib.ImageCopyChannel(pSrc, pDst, tex2D.Width, tex2D.Height, tex2D.Width * 4, tex2D.Width * 4, 0, ofs, 1);

                        tex2D.Data[0].Data = mrImage;
                        //tex2D.Format = TextureFormat.Rgb24;
                        result.MetallicRoughnessMap = tex2D;
                    }
                }

                _materialCache[fileName] = result;
            }

            return (Material)result;
        }
    }
}
