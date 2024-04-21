using System.Web;

namespace XrEngine.Gltf
{
    public class GltfAssetLoader : IAssetLoader
    {
        readonly Dictionary<string, GltfAssetCache> _cache = [];

        class GltfAssetCache
        {
            public GltfLoader? Loader { get; set; }

            public DateTime LastEditTime { get; set; }
        }


        GltfAssetLoader() { }

        protected string GetFilePath(Uri uri)
        {
            if (uri.Scheme == "res" && uri.Host == "asset")
                return Context.Require<IAssetStore>().GetPath(uri.LocalPath);
            return uri.IsAbsoluteUri ? uri.LocalPath : uri.ToString();
        }

        public bool CanHandle(Uri uri, out Type resType)
        {
            if (uri.Scheme == "res" && uri.Host == "gltf")
            {
                var seg = uri.Segments.FirstOrDefault();
                if (seg == "/tex")
                {
                    resType = typeof(Texture2D);
                    return true;
                }
                if (seg == "/geo")
                {
                    resType = typeof(Geometry3D);
                    return true;
                }
                if (seg == "/mat")
                {
                    resType = typeof(PbrMaterial);
                    return true;
                }
                if (seg == "/mesh")
                {
                    resType = typeof(Object3D);
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

        public EngineObject LoadAsset(Uri uri, Type resType, EngineObject? destObj, IAssetLoaderOptions? options = null)
        {
            var query = HttpUtility.ParseQueryString(uri.Query);
            string fsSrc;

            if (uri.Host == "gltf")
                fsSrc = query["src"]!;
            else
                fsSrc = GetFilePath(uri);

            var lastEditTime = File.GetLastWriteTime(fsSrc);

            if (!_cache.TryGetValue(fsSrc, out var cache) || lastEditTime > cache.LastEditTime)
            {
                cache = new GltfAssetCache
                {
                    LastEditTime = lastEditTime,
                    Loader = new GltfLoader()
                };
                cache.Loader.LoadModel(fsSrc, (GltfLoaderOptions?)options);

                if (UseCache)
                    _cache[fsSrc] = cache;
            }

            EngineObject result;

            if (uri.Host == "gltf")
            {
                var seg = uri.Segments[1].TrimEnd('/');

                int meshId;

                switch (seg)
                {
                    case "tex":
                        var texId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        result = cache.Loader!.ProcessTexture(cache.Loader.Model!.Textures[texId], texId, (Texture2D?)destObj);
                        break;
                    case "geo":
                        meshId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        var pIndex = int.Parse(uri.Segments[3].TrimEnd('/'));
                        var mesh = cache.Loader!.Model!.Meshes[meshId];
                        result = cache.Loader!.ProcessPrimitive(mesh.Primitives[pIndex], (Geometry3D?)destObj);
                        break;
                    case "mat":
                        var matId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        result = cache.Loader!.ProcessMaterial(cache.Loader!.Model!.Materials[matId], matId, (PbrMaterial?)destObj);
                        break;
                    case "mesh":
                        meshId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        result = cache.Loader!.ProcessMesh(cache.Loader!.Model!.Meshes[meshId], meshId, (TriangleMesh?)destObj);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
                result = cache.Loader!.LoadScene();

            cache.Loader!.ExecuteLoadTasks();

            return result;
        }

        public bool UseCache { get; set; }  


        public static readonly GltfAssetLoader Instance = new GltfAssetLoader();
    }
}
