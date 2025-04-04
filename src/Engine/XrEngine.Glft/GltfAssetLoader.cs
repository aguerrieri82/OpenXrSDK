﻿using System.Web;

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

        public bool CanHandle(Uri uri, out Type assetType)
        {
            if (uri.Scheme == "res" && uri.Host == "gltf")
            {
                var seg = uri.Segments.FirstOrDefault();
                if (seg == "/tex")
                {
                    assetType = typeof(Texture2D);
                    return true;
                }
                if (seg == "/geo")
                {
                    assetType = typeof(Geometry3D);
                    return true;
                }
                if (seg == "/mat")
                {
                    assetType = MaterialFactory.DefaultPbr;
                    return true;
                }
                if (seg == "/mesh")
                {
                    assetType = typeof(Object3D);
                    return true;
                }
            }

            var ext = Path.GetExtension(uri.ToString());
            if (ext == ".glb" || ext == ".gltf")
            {
                assetType = typeof(Object3D);
                return true;
            }

            assetType = typeof(object);

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
                    Loader = new GltfLoader(a => Context.Require<IAssetStore>().GetPath(a))
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
                        //TODO pass extensions
                        result = cache.Loader!.ProcessTextureTask(texId, null, (Texture2D?)destObj).Result;
                        break;
                    case "geo":
                        meshId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        var pIndex = int.Parse(uri.Segments[3].TrimEnd('/'));
                        var mesh = cache.Loader!.Model!.Meshes[meshId];
                        result = cache.Loader!.ProcessPrimitive(mesh.Primitives[pIndex], (Geometry3D?)destObj);
                        break;
                    case "mat":
                        var matId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        result = cache.Loader!.ProcessMaterialV2(matId, (PbrV2Material?)destObj);
                        break;
                    case "mesh":
                        meshId = int.Parse(uri.Segments[2].TrimEnd('/'));
                        result = cache.Loader!.ProcessMesh(meshId, (TriangleMesh?)destObj);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                result = cache.Loader!.LoadScene();

                result.AddComponent(new AssetSource
                {
                    Asset = new BaseAsset<GltfLoaderOptions, GltfAssetLoader>(
                            GltfAssetLoader.Instance,
                            Path.GetFileName(uri.ToString())!,
                            typeof(Group3D),
                            uri,
                            (GltfLoaderOptions?)options)
                });
            }

            cache.Loader!.ExecuteLoadTasks();

            if (!UseCache)
                cache.Loader.Dispose();

            return result;
        }

        public bool UseCache { get; set; }


        public static readonly GltfAssetLoader Instance = new GltfAssetLoader();
    }
}
