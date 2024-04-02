
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Web;
using XrMath;

namespace XrEngine.Gltf
{
    public class GltfLoaderOptions
    {
        public GltfLoaderOptions()
        {
            ConvertColorTextureSRgb = true;
        }


        public bool ConvertColorTextureSRgb { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public static readonly GltfLoaderOptions Default = new();
    }

    public class GltfAssetLoader : IAssetLoader
    {
        Dictionary<string, GltfAssetCache> _cache = [];

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

        public EngineObject LoadAsset(Uri uri, Type resType, IAssetManager assetManager, object? options = null)
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

            var seg = uri.Segments[1].TrimEnd('/');

            switch (seg)
            {
                case "texture":
                    var id = int.Parse(uri.Segments[2].TrimEnd('/'));
                    return cache.Loader!.CreateTexture(cache.Loader.Model!.Textures[id], id);
            }

            throw new NotSupportedException();
        }
    }

    public class GltfLoader 
    {
        GltfLoaderOptions? _options;
        IAssetManager? _assetManager;
        glTFLoader.Schema.Gltf? _model;
        Dictionary<glTFLoader.Schema.Material, PbrMaterial> _mats =  [];
        Dictionary<glTFLoader.Schema.Image, TextureData> _images = [];
        Dictionary<glTFLoader.Schema.Mesh, Object3D> _meshes = [];
        Dictionary<int, byte[]> _buffers = [];
        StringBuilder _log = new();
        string? _basePath;
        string? _filePath;

        static string[] supportedExt = { "KHR_draco_mesh_compression", "KHR_materials_pbrSpecularGlossiness" };

        struct KHR_draco_mesh_compression
        {
            public int BufferView;

            public Dictionary<string, int> Attributes;
        }

        struct KHR_materials_pbrSpecularGlossiness
        {
            public float[] diffuseFactor;

            public float[] specularFactor;

            public float glossinessFactor;

            public glTFLoader.Schema.TextureInfo? diffuseTexture;

            public glTFLoader.Schema.TextureInfo? specularGlossinessTexture;

        }

        public GltfLoader()
        {

        }

        protected byte[] LoadBuffer(int index)
        {
            if (!_buffers.TryGetValue(index, out var buffer))
            {
                buffer = glTFLoader.Interface.LoadBinaryBuffer(_model, index, _filePath);
                _buffers[index] = buffer;
            }
            return buffer;
        }

        protected void CheckExtensions(Dictionary<string, object>? ext)
        {
            if (ext == null)
                return;
            foreach (var key in ext.Keys)
            {
                if (!supportedExt.Contains(key))
                    _log.AppendLine($"Extensions '{key}' not supported");
            }
        }

        protected static T? TryLoadExtension<T>(Dictionary<string, object>? ext) where T : struct
        {
            if (ext != null && ext.TryGetValue(typeof(T).Name, out var extension))
                return ((JObject)extension).ToObject<T>();
            return null;
        }

        protected TextureData LoadImage(glTFLoader.Schema.Image img, bool useSrgb = false)
        {
            if (_images.TryGetValue(img, out var result))
                return result;

            CheckExtensions(img.Extensions);

            byte[] data;

            if (img.BufferView != null)
            {
                var view = _model!.BufferViews[img.BufferView.Value];
                var buffer = LoadBuffer(view.Buffer);
                data = new Span<byte>(buffer, view.ByteOffset, view.ByteLength).ToArray();
            }
            else if (img.Uri != null)
            {
                data = _assetManager!.Open(Path.Join(_basePath!, img.Uri))
                    .ToMemory()
                    .ToArray();
            }
            else
                throw new NotSupportedException();

            using var image = ImageUtils.ChangeColorSpace(SKBitmap.Decode(data), SKColorType.Rgba8888);

            //Debug.Assert(image.ColorSpace.IsSrgb && useSrgb);

            result = new TextureData
            {
                Width = (uint)image.Width,
                Height = (uint)image.Height,
                Data = image.GetPixelSpan().ToArray(),
                Format = image.ColorType switch
                {
                    SKColorType.Srgba8888 => TextureFormat.SRgba32,
                    SKColorType.Bgra8888 => useSrgb ? TextureFormat.SBgra32 : TextureFormat.Bgra32,
                    SKColorType.Rgba8888 => useSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32,
                    SKColorType.Gray8 => TextureFormat.Gray8,
                    _ => throw new NotSupportedException()
                }
            };

            _images[img] = result;

            return result;
        }

        public Texture2D CreateTexture(glTFLoader.Schema.Texture texture, int id)
        {
            var imageInfo = _model!.Images[texture.Source!.Value];

            var data = LoadImage(imageInfo);

            CheckExtensions(texture.Extensions);

            var result = Texture2D.FromData([data]);
            result.Name = texture.Name ?? (imageInfo.Name ?? "");

            bool hasMinFilter = false;

            if (texture.Sampler != null)
            {
                var sampler = _model!.Samplers[texture.Sampler.Value];
                CheckExtensions(sampler.Extensions);

                result.WrapS = (WrapMode)sampler.WrapS;
                result.WrapT = (WrapMode)sampler.WrapT;

                if (sampler.MagFilter != null)
                    result.MagFilter = (ScaleFilter)sampler.MagFilter;

                if (sampler.MinFilter != null)
                {
                    hasMinFilter = true;
                    result.MinFilter = (ScaleFilter)sampler.MinFilter;
                }
            }

            if (!hasMinFilter)
            {
                result.MinFilter = ScaleFilter.LinearMipmapLinear;
                result.MagFilter = ScaleFilter.Linear;
            }

            result.AddComponent(new AssetSource 
            { 
                AssetUri = new Uri($"res://gltf/texture/{id}?src={_filePath}")
            });

            return result;
        }

        protected Texture2D DecodeTextureOcclusion(glTFLoader.Schema.MaterialOcclusionTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return CreateTexture(_model!.Textures[info.Index], info.Index);
        }

        protected Texture2D DecodeTextureNormal(glTFLoader.Schema.MaterialNormalTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return CreateTexture(_model!.Textures[info.Index], info.Index);
        }

        protected Texture2D DecodeTextureBase(glTFLoader.Schema.TextureInfo info, bool useSRgb = false)
        {
            CheckExtensions(info.Extensions);

            return CreateTexture(_model!.Textures[info.Index], info.Index);
        }

        protected PbrMaterial DecodeMaterial(glTFLoader.Schema.Material gltMat)
        {
            if (_mats.TryGetValue(gltMat, out var result))
                return result;

            result = new PbrMaterial
            {
                Name = gltMat.Name,
                AlphaCutoff = gltMat.AlphaCutoff,
                EmissiveFactor = MathUtils.ToVector3(gltMat.EmissiveFactor),
                Alpha = gltMat.AlphaMode switch
                {
                    glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE => AlphaMode.Opaque,
                    glTFLoader.Schema.Material.AlphaModeEnum.MASK => AlphaMode.Mask,
                    glTFLoader.Schema.Material.AlphaModeEnum.BLEND => AlphaMode.Blend,
                    _ => throw new NotSupportedException()
                },
                DoubleSided = gltMat.DoubleSided
            };

            if (gltMat.PbrMetallicRoughness != null)
            {
                result.MetallicRoughness = new PbrMaterial.MetallicRoughnessData
                {
                    RoughnessFactor = gltMat.PbrMetallicRoughness.RoughnessFactor,
                    MetallicFactor = gltMat.PbrMetallicRoughness.MetallicFactor,
                    BaseColorFactor = new Color(gltMat.PbrMetallicRoughness.BaseColorFactor)
                };

                if (gltMat.PbrMetallicRoughness.BaseColorTexture != null)
                {
                    result.MetallicRoughness.BaseColorTexture = DecodeTextureBase(gltMat.PbrMetallicRoughness.BaseColorTexture, _options!.ConvertColorTextureSRgb);
                    result.MetallicRoughness.BaseColorUVSet = gltMat.PbrMetallicRoughness.BaseColorTexture.TexCoord;
                }

                if (gltMat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                {
                    result.MetallicRoughness.MetallicRoughnessTexture = DecodeTextureBase(gltMat.PbrMetallicRoughness.MetallicRoughnessTexture);
                    result.MetallicRoughness.MetallicRoughnessUVSet = gltMat.PbrMetallicRoughness.MetallicRoughnessTexture.TexCoord;
                }

                result.Type = PbrMaterial.MaterialType.Metallic;
            }

            if (gltMat.EmissiveTexture != null)
                result.EmissiveTexture = DecodeTextureBase(gltMat.EmissiveTexture);

            if (gltMat.NormalTexture != null)
            {
                result.NormalTexture = DecodeTextureNormal(gltMat.NormalTexture);
                result.NormalScale = gltMat.NormalTexture.Scale;
                result.NormalUVSet = gltMat.NormalTexture.TexCoord;
            }

            if (gltMat.OcclusionTexture != null)
            {
                result.OcclusionTexture = DecodeTextureOcclusion(gltMat.OcclusionTexture);
                result.OcclusionStrength = gltMat.OcclusionTexture.Strength;
                result.OcclusionUVSet = gltMat.OcclusionTexture.TexCoord;
            }

            var specGlos = TryLoadExtension<KHR_materials_pbrSpecularGlossiness>(gltMat.Extensions);
            if (specGlos != null)
            {
                result.SpecularGlossiness = new PbrMaterial.SpecularGlossinessData
                {
                    DiffuseFactor = new Color(specGlos.Value.diffuseFactor),
                    SpecularFactor = new Color(specGlos.Value.specularFactor),
                    GlossinessFactor = specGlos.Value.glossinessFactor
                };

                if (specGlos.Value.diffuseTexture != null)
                {
                    result.SpecularGlossiness.DiffuseTexture = DecodeTextureBase(specGlos.Value.diffuseTexture);
                    result.SpecularGlossiness.DiffuseUVSet = specGlos.Value.diffuseTexture.TexCoord;
                }

                if (specGlos.Value.specularGlossinessTexture != null)
                {
                    result.SpecularGlossiness.SpecularGlossinessTexture = DecodeTextureBase(specGlos.Value.specularGlossinessTexture);
                    result.SpecularGlossiness.SpecularGlossinessUVSet = specGlos.Value.specularGlossinessTexture.TexCoord;
                }

                result.Type = PbrMaterial.MaterialType.Specular;
            }

            _mats[gltMat] = result;

            return result;
        }

        unsafe T[] ConvertBuffer<T>(byte[] buffer, glTFLoader.Schema.BufferView view, glTFLoader.Schema.Accessor acc) where T : unmanaged
        {
            Debug.Assert(acc.Sparse == null);

            fixed (byte* pBuffer = buffer)
            {
                if (view.ByteStride == null || view.ByteStride == sizeof(T))
                    return new Span<T>((T*)(pBuffer + view.ByteOffset + acc.ByteOffset), acc.Count).ToArray();
                else
                {
                    byte* curBuffer = pBuffer + view.ByteOffset + acc.ByteOffset;
                    var array = new T[acc.Count];
                    for (var i = 0; i < acc.Count; i++)
                    {
                        array[i] = *(T*)curBuffer;
                        curBuffer += view.ByteStride.Value;
                    }
                    return array;
                }
            }

        }


        protected Object3D ProcessMesh(glTFLoader.Schema.Mesh gltMesh)
        {
            if (_meshes.TryGetValue(gltMesh, out var result))
                return new Object3DInstance() { Reference = result };

            CheckExtensions(gltMesh.Extensions);

            var group = gltMesh.Primitives.Length > 1 ? new Group3D() : null;

            foreach (var primitive in gltMesh.Primitives)
            {
                var curMesh = new TriangleMesh();

                Debug.Assert(primitive.Targets == null);

                CheckExtensions(primitive.Extensions);

                var draco = TryLoadExtension<KHR_draco_mesh_compression>(primitive.Extensions);

                if (primitive.Mode == glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
                {
                    int vertexCount = 0;
                    if (draco != null)
                    {
                        var view = _model!.BufferViews[draco.Value.BufferView];
                        var buffer = LoadBuffer(view.Buffer);
                        var mesh = DracoDecoder.DecodeBuffer(buffer, view.ByteOffset, view.ByteLength);

                        try
                        {
                            var geo = new Geometry3D
                            {
                                Indices = DracoDecoder.ReadIndices(mesh),
                                Vertices = new VertexData[mesh.VerticesSize]
                            };

                            curMesh.Geometry = geo;

                            foreach (var attr in draco.Value.Attributes)
                            {
                                switch (attr.Key)
                                {
                                    case "POSITION":
                                        var vValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                        geo.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                        geo.ActiveComponents |= VertexComponent.Position;
                                        vertexCount = vValues.Length;
                                        break;
                                    case "NORMAL":
                                        var nValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                        geo.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                        geo.ActiveComponents |= VertexComponent.Normal;
                                        break;
                                    case "TANGENT":
                                        var tValues = DracoDecoder.ReadAttribute<Quaternion>(mesh, attr.Value);
                                        geo.SetVertexData((ref VertexData a, Quaternion b) => a.Tangent = b, tValues);
                                        geo.ActiveComponents |= VertexComponent.Tangent;
                                        break;
                                    case "TEXCOORD_0":
                                        var uValues = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                        geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                        geo.ActiveComponents |= VertexComponent.UV0;
                                        break;
                                    default:
                                        _log.AppendLine($"{attr.Key} data not supported");
                                        break;
                                }

                            }
                        }
                        finally
                        {
                            DracoDecoder.DisposeMesh((IntPtr)mesh.Mesh);
                        }
                    }
                    else
                    {
                        var geo = new Geometry3D();

                        foreach (var attr in primitive.Attributes)
                        {
                            var acc = _model!.Accessors[attr.Value];

                            var view = _model.BufferViews[acc.BufferView!.Value];

                            var buffer = LoadBuffer(view.Buffer);

                            switch (attr.Key)
                            {
                                case "POSITION":
                                    var vValues = ConvertBuffer<Vector3>(buffer, view, acc);
                                    geo.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                    geo.ActiveComponents |= VertexComponent.Position;
                                    vertexCount = vValues.Length;
                                    Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                                    Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                    break;
                                case "NORMAL":
                                    var nValues = ConvertBuffer<Vector3>(buffer, view, acc);
                                    geo.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                    geo.ActiveComponents |= VertexComponent.Normal;
                                    Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                                    Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                    break;
                                case "TANGENT":
                                    var tValues = ConvertBuffer<Quaternion>(buffer, view, acc);
                                    geo.SetVertexData((ref VertexData a, Quaternion b) => a.Tangent = b, tValues);
                                    geo.ActiveComponents |= VertexComponent.Tangent;
                                    Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC4);
                                    Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                    break;
                                case "TEXCOORD_0":
                                    var uValues = ConvertBuffer<Vector2>(buffer, view, acc);
                                    geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                    geo.ActiveComponents |= VertexComponent.UV0;
                                    Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC2);
                                    Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                    break;
                                default:
                                    _log.AppendLine($"{attr.Key} data not supported");
                                    break;
                            }

                        }

                        if (primitive.Indices != null)
                        {
                            var acc = _model!.Accessors[primitive.Indices.Value];

                            Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.SCALAR);

                            var view = _model.BufferViews[acc.BufferView!.Value];

                            var buffer = LoadBuffer(view.Buffer);

                            if (acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                                geo.Indices = ConvertBuffer<ushort>(buffer, view, acc)
                                    .Select(a => (uint)a)
                                    .ToArray();
                            else if (acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
                                geo.Indices = ConvertBuffer<uint>(buffer, view, acc);
                            else
                                throw new NotSupportedException();
                        }

                        curMesh.Geometry = geo;

                    }

                    if (primitive.Material != null)
                    {
                        var glftMat = _model!.Materials[primitive.Material.Value];
                        curMesh.Materials.Add(DecodeMaterial(glftMat));
                    }
                }
                else
                    throw new NotSupportedException();

                if (((curMesh.Geometry.ActiveComponents & VertexComponent.Normal) != 0) &&
                    ((curMesh.Geometry.ActiveComponents & VertexComponent.UV0) != 0) &&
                    ((curMesh.Geometry.ActiveComponents & VertexComponent.Tangent) == 0))
                {
                    //curMesh.Geometry.ComputeTangents();
                }

                if (group == null)
                {
                    _meshes[gltMesh] = curMesh;
                    return curMesh;
                }

                group.AddChild(curMesh);
            }

            _meshes[gltMesh] = group!;

            return group!;
        }

        protected Camera ProcessCamera(glTFLoader.Schema.Camera gltCamera)
        {
            CheckExtensions(gltCamera.Extensions);
            throw new NotSupportedException();
        }

        protected Object3D ProcessNode(glTFLoader.Schema.Node node, Group3D curGrp)
        {
            CheckExtensions(node.Extensions);

            Object3D? nodeObj = null;
            Group3D? nodeGrp = null;

            if (node.Children != null && node.Children.Length > 0)
            {
                nodeGrp = new Group3D();
                nodeObj = nodeGrp;
            }

            if (node.Mesh != null)
            {
                var nodeMesh = ProcessMesh(_model!.Meshes[node.Mesh.Value]);
                if (nodeGrp != null)
                    nodeGrp.AddChild(nodeMesh);
                else
                    nodeObj = nodeMesh;
            }
            else if (node.Camera != null)
            {
                nodeObj = ProcessCamera(_model!.Cameras[node.Camera.Value]);

                Debug.Assert(node.Children == null);
            }
            else if (nodeGrp == null)
            {
                nodeObj = new Object3D();
            }

            if (nodeGrp != null)
            {
                foreach (var childNode in node.Children!)
                    ProcessNode(_model!.Nodes[childNode], nodeGrp);
            }

            nodeObj!.Name = node.Name;

            if (node.Rotation != null)
                nodeObj.Transform.Orientation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);

            if (node.Scale != null)
                nodeObj.Transform.Scale = MathUtils.ToVector3(node.Scale);

            if (node.Translation != null)
                nodeObj.Transform.Position = MathUtils.ToVector3(node.Translation);

            nodeObj.Transform.Update();

            //obj.Transform.SetMatrix(MathUtils.CreateMatrix(node.Matrix));

            curGrp.AddChild(nodeObj);

            return nodeObj;
        }

        protected Group3D ProcessScene(glTFLoader.Schema.Scene glScene)
        {
            var scene = new Group3D();

            foreach (var node in glScene.Nodes)
                ProcessNode(_model!.Nodes[node], scene);

            return scene;
        }

        public void LoadModel(string filePath, IAssetManager assetManager, GltfLoaderOptions? options)
        {
            _buffers.Clear();
            _log.Clear();
            _images.Clear();
            _mats.Clear();
            _meshes.Clear();

            _options = options;
            _assetManager = assetManager;
            _basePath = Path.GetDirectoryName(filePath)!;
            _filePath = filePath;


            _model = glTFLoader.Interface.LoadModel(assetManager.GetFsPath(filePath));
        }


        public Object3D Load(string filePath, IAssetManager assetManager, GltfLoaderOptions options)
        {
            LoadModel(filePath, assetManager, options);

            var root = new Group3D();

            foreach (var scene in _model!.Scenes)
                root.AddChild(ProcessScene(scene));

            Object3D curRoot = root;

            while (true)
            {
                if (curRoot is Group3D grp && grp.Children.Count == 1 && grp.WorldMatrix.IsIdentity)
                    curRoot = grp.Children[0];
                else
                    break;
            }

            return curRoot;
        }


        public static Object3D LoadFile(string filePath, IAssetManager assetManager)
        {
            return LoadFile(filePath, assetManager, GltfLoaderOptions.Default);
        }

        public static Object3D LoadFile(string filePath, IAssetManager assetManager, GltfLoaderOptions options)
        {
            var loader = new GltfLoader();
            return loader.Load(filePath, assetManager, options);
        }


        public glTFLoader.Schema.Gltf? Model => _model;
    }
}
