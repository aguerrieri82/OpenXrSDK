using System.Web;

namespace XrEngine.Gltf
{
    public class GltfAssetLoader : IAssetHandler
    {
        readonly Dictionary<string, GltfAssetCache> _cache = [];

        class GltfAssetCache
        {
            public GltfLoader? Loader { get; set; }

            public DateTime LastEditTime { get; set; }
        }

        public bool CanHandle(Uri uri, out Type resType)
        {
            if (uri.Scheme == "res" && uri.Host == "gltf")
            {
                var seg = uri.Segments.FirstOrDefault();
                if (seg == "/texture")
                {
                    resType = typeof(Texture2D);
                    return true;
                }
            }

            var ext = Path.GetExtension(uri.ToString());
            if (ext == ".glb" || ext == ".gltf")
            {
                resType = typeof(Object3D);
                return true;
            }

            resType = typeof(object);

            return false;
        }

        public EngineObject LoadAsset(Uri uri, Type resType, IAssetManager assetManager, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            string src;

            if (uri.Scheme == "res")
                src = query["src"]!;
            else
                src = uri.LocalPath;

            var fsSrc = assetManager.GetFsPath(src);
            var lastEditTime = File.GetLastWriteTime(fsSrc);

            if (!_cache.TryGetValue(fsSrc, out var cache) || lastEditTime > cache.LastEditTime)
            {
                cache = new GltfAssetCache
                {
                    LastEditTime = lastEditTime,
                    Loader = new GltfLoader()
                };
                cache.Loader.LoadModel(src, assetManager, (GltfLoaderOptions?)options);
                _cache[fsSrc] = cache;
            }

            if (uri.Scheme == "res")
            {
                var seg = uri.Segments[1].TrimEnd('/');

                switch (seg)
                {
                    case "texture":
                        var texId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        return cache.Loader!.ProcessTexture(cache.Loader.Model!.Textures[texId], texId, (Texture2D?)destObj ?? new());
                    case "geo":
                        var meshId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        var pIndex = int.Parse(uri.Segments[3].TrimEnd('/'));
                        var mesh = cache.Loader!.Model!.Meshes[meshId];
                        return cache.Loader!.ProcessPrimitive(mesh.Primitives[pIndex], (Geometry3D?)destObj ?? new());
                    default:
                        throw new NotSupportedException();
                }
            }

            return cache.Loader!.LoadScene();
        }
    }
}
