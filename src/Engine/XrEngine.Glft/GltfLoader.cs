using glTFLoader.Schema;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using TurboJpeg;
using XrEngine.Services;
using XrMath;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace XrEngine.Gltf
{

    public class GltfLoader
    {
        GltfLoaderOptions? _options;
        glTFLoader.Schema.Gltf? _model;
        readonly Dictionary<glTFLoader.Schema.Material, PbrMaterial> _mats = [];
        readonly ConcurrentDictionary<glTFLoader.Schema.Image, TextureData> _images = [];
        readonly Dictionary<glTFLoader.Schema.Mesh, Object3D> _meshes = [];
        readonly List<Action> _tasks = [];
        readonly ConcurrentDictionary<int, byte[]> _buffers = [];
        readonly StringBuilder _log = new();
        readonly Func<string, string> _resourceResolver;
        string? _basePath;
        string? _filePath;
        static readonly string[] supportedExt = { "KHR_draco_mesh_compression", "KHR_materials_pbrSpecularGlossiness" };

        struct KHR_draco_mesh_compression
        {
            public int BufferView;

            public Dictionary<string, int> Attributes;
        }

        struct KHR_materials_pbrSpecularGlossiness
        {
            public float[]? diffuseFactor;

            public float[]? specularFactor;

            public float glossinessFactor;

            public TextureInfo? diffuseTexture;

            public TextureInfo? specularGlossinessTexture;
        }

        public GltfLoader()
            : this(a => a)
        {

        }

        public GltfLoader(Func<string, string> resourceResolver)
        {
            _resourceResolver = resourceResolver;
        }


        protected byte[] LoadBuffer(int index)
        {
            return _buffers.GetOrAdd(index,
                index => glTFLoader.Interface.LoadBinaryBuffer(_model, index, _filePath));

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

        protected TextureData ProcessImage(glTFLoader.Schema.Image img, bool useSrgb = false)
        {
            return _images.GetOrAdd(img, img =>
            {
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
                    var imgPath = _resourceResolver(Path.Join(_basePath!, img.Uri));
                    data = File.OpenRead(imgPath)
                        .ToMemory()
                        .ToArray();
                }
                else
                    throw new NotSupportedException();

                //Debug.Assert(image.ColorSpace.IsSrgb && useSrgb);

                Log.Info(this, "Loading texture {0} ({1} bytes)", img.Name, data.Length);

                if (img.MimeType == glTFLoader.Schema.Image.MimeTypeEnum.image_jpeg)
                {
                    var outImg = TurboJpegLib.Decompress(data);

                    return new TextureData
                    {
                        Data = outImg.Data,
                        Width = (uint)outImg.Width,
                        Height = (uint)outImg.Height,
                        Format = useSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32,
                    };
                }
       
                else
                {
                    using var image = ImageUtils.ChangeColorSpace(SKBitmap.Decode(data), SKColorType.Rgba8888);
                    return new TextureData
                    {
                        Data = image.GetPixelSpan().ToArray(),
                        Width = (uint)image.Width,
                        Height = (uint)image.Height,
                        Format = image.ColorType switch
                        {
                            SKColorType.Srgba8888 => TextureFormat.SRgba32,
                            SKColorType.Bgra8888 => useSrgb ? TextureFormat.SBgra32 : TextureFormat.Bgra32,
                            SKColorType.Rgba8888 => useSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32,
                            SKColorType.Gray8 => TextureFormat.Gray8,
                            _ => throw new NotSupportedException()
                        }
                    };
                }

         
            });
        }

        public Texture2D ProcessTexture(glTFLoader.Schema.Texture texture, int id, Texture2D? result = null, bool useSrgb = false)
        {
            CheckExtensions(texture.Extensions);

            var imageInfo = _model!.Images[texture.Source!.Value];

            result ??= new Texture2D();

            _tasks.Add(() =>
            {
                Log.Info(this, "Loading image {0}", imageInfo.Uri);

                var data = ProcessImage(imageInfo, useSrgb);

                Log.Info(this, "Loading image {0} end", imageInfo.Uri);

                result.LoadData([data]);

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
                else
                {
                    result.WrapS = WrapMode.Repeat;
                    result.WrapT = WrapMode.Repeat;
                }

                if (!hasMinFilter)
                {
                    result.MinFilter = ScaleFilter.LinearMipmapLinear;
                    result.MagFilter = ScaleFilter.Linear;
                }
            });

            result.Flags |= EngineObjectFlags.Readonly;

            result.Name = texture.Name ?? (imageInfo.Name ?? imageInfo.Uri ?? "");

            result.AddComponent(new AssetSource
            {
                Asset = CreateAsset<Texture2D>(result.Name!, "tex", id)
            });

            return result;
        }

        protected Texture2D DecodeTextureOcclusion(glTFLoader.Schema.MaterialOcclusionTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return ProcessTexture(_model!.Textures[info.Index], info.Index);
        }

        protected Texture2D DecodeTextureNormal(glTFLoader.Schema.MaterialNormalTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return ProcessTexture(_model!.Textures[info.Index], info.Index);
        }

        protected Texture2D DecodeTextureBase(glTFLoader.Schema.TextureInfo info, bool useSRgb = false)
        {
            CheckExtensions(info.Extensions);

            return ProcessTexture(_model!.Textures[info.Index], info.Index, null, true);
        }

        public PbrMaterial ProcessMaterial(glTFLoader.Schema.Material gltMat, int id, PbrMaterial? result = null)
        {
            if (result == null && _mats.TryGetValue(gltMat, out result))
                return result;

            result ??= new PbrMaterial();

            result.Name = gltMat.Name;
            result.AlphaCutoff = gltMat.AlphaCutoff;
            result.EmissiveFactor = MathUtils.ToVector3(gltMat.EmissiveFactor);
            result.Alpha = gltMat.AlphaMode switch
            {
                glTFLoader.Schema.Material.AlphaModeEnum.OPAQUE => AlphaMode.Opaque,
                glTFLoader.Schema.Material.AlphaModeEnum.MASK => AlphaMode.Mask,
                glTFLoader.Schema.Material.AlphaModeEnum.BLEND => AlphaMode.Blend,
                _ => throw new NotSupportedException()
            };
            result.DoubleSided = gltMat.DoubleSided;

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

            var specGloss = TryLoadExtension<KHR_materials_pbrSpecularGlossiness>(gltMat.Extensions);
            if (specGloss != null)
            {
                result.SpecularGlossiness = new PbrMaterial.SpecularGlossinessData
                {
                    DiffuseFactor = new Color(specGloss.Value.diffuseFactor!),
                    SpecularFactor = new Color(specGloss.Value.specularFactor!),
                    GlossinessFactor = specGloss.Value.glossinessFactor
                };

                if (specGloss.Value.diffuseTexture != null)
                {
                    result.SpecularGlossiness.DiffuseTexture = DecodeTextureBase(specGloss.Value.diffuseTexture);
                    result.SpecularGlossiness.DiffuseUVSet = specGloss.Value.diffuseTexture.TexCoord;
                }

                if (specGloss.Value.specularGlossinessTexture != null)
                {
                    result.SpecularGlossiness.SpecularGlossinessTexture = DecodeTextureBase(specGloss.Value.specularGlossinessTexture);
                    result.SpecularGlossiness.SpecularGlossinessUVSet = specGloss.Value.specularGlossinessTexture.TexCoord;
                }

                result.Type = PbrMaterial.MaterialType.Specular;
            }

            result.AddComponent(new AssetSource
            {
                Asset = CreateAsset<PbrMaterial>(gltMat.Name!, "mat", id)
            });


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

        public Geometry3D ProcessPrimitive(glTFLoader.Schema.MeshPrimitive primitive, Geometry3D? result = null)
        {
            result ??= new Geometry3D();

            result.Flags |= EngineObjectFlags.Readonly;

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
                        result.Indices = DracoDecoder.ReadIndices(mesh);
                        result.Vertices = new VertexData[mesh.VerticesSize];

                        foreach (var attr in draco.Value.Attributes)
                        {
                            var acc = _model!.Accessors[attr.Value];

                            switch (attr.Key)
                            {
                                case "POSITION":
                                    var vValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                    result.ActiveComponents |= VertexComponent.Position;
                                    vertexCount = vValues.Length;
                                    break;
                                case "NORMAL":
                                    var nValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                    result.ActiveComponents |= VertexComponent.Normal;
                                    break;
                                case "TANGENT":
                                    if (_options != null && _options.DisableTangents)
                                        break;
                                    var tValues = DracoDecoder.ReadAttribute<Quaternion>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, Quaternion b) => a.Tangent = b, tValues);
                                    result.ActiveComponents |= VertexComponent.Tangent;
                                    break;
                                case "TEXCOORD_0":
                                    var uValues = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                    result.ActiveComponents |= VertexComponent.UV0;
                                    break;
                                case "TEXCOORD_1":
                                    var uValues1 = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, Vector2 b) => a.UV1 = b, uValues1);
                                    result.ActiveComponents |= VertexComponent.UV1;
                                    break;
                                default:
                                    _log.AppendLine($"{attr.Key} data not supported");
                                    break;
                            }

                        }
                    }
                    finally
                    {
                        DracoDecoder.DisposeMesh(mesh.Mesh);
                    }
                }
                else
                {
                    foreach (var attr in primitive.Attributes)
                    {
                        var acc = _model!.Accessors[attr.Value];

                        var view = _model.BufferViews[acc.BufferView!.Value];

                        var buffer = LoadBuffer(view.Buffer);

                        switch (attr.Key)
                        {
                            case "POSITION":
                                var vValues = ConvertBuffer<Vector3>(buffer, view, acc);
                                result.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                result.ActiveComponents |= VertexComponent.Position;
                                vertexCount = vValues.Length;
                                Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                                Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "NORMAL":
                                var nValues = ConvertBuffer<Vector3>(buffer, view, acc);
                                result.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                result.ActiveComponents |= VertexComponent.Normal;
                                Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC3);
                                Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TANGENT":
                                if (_options != null && _options.DisableTangents)
                                    break;
                                var tValues = ConvertBuffer<Quaternion>(buffer, view, acc);
                                result.SetVertexData((ref VertexData a, Quaternion b) => a.Tangent = b, tValues);
                                result.ActiveComponents |= VertexComponent.Tangent;
                                Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC4);
                                Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TEXCOORD_0":
                                var uValues = ConvertBuffer<Vector2>(buffer, view, acc);
                                result.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                result.ActiveComponents |= VertexComponent.UV0;
                                Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.VEC2);
                                Debug.Assert(acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TEXCOORD_1":
                                var uValues1 = ConvertBuffer<Vector2>(buffer, view, acc);
                                result.SetVertexData((ref VertexData a, Vector2 b) => a.UV1 = b, uValues1);
                                result.ActiveComponents |= VertexComponent.UV1;
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
                            result.Indices = ConvertBuffer<ushort>(buffer, view, acc)
                                .Select(a => (uint)a)
                                .ToArray();
                        else if (acc.ComponentType == glTFLoader.Schema.Accessor.ComponentTypeEnum.UNSIGNED_INT)
                            result.Indices = ConvertBuffer<uint>(buffer, view, acc);
                        else
                            throw new NotSupportedException();
                    }
                }


            }
            else
                throw new NotSupportedException();

            if (((result.ActiveComponents & VertexComponent.Normal) != 0) &&
                ((result.ActiveComponents & VertexComponent.UV0) != 0) &&
                ((result.ActiveComponents & VertexComponent.Tangent) == 0))
            {
                //geo.ComputeTangents();
            }

            return result;
        }

        public Object3D ProcessMesh(glTFLoader.Schema.Mesh gltMesh, int id, Object3D? result = null)
        {
            if (result == null && _meshes.TryGetValue(gltMesh, out result))
                return new Object3DInstance() { Reference = result };



            CheckExtensions(gltMesh.Extensions);

            var group = gltMesh.Primitives.Length > 1 ? new Group3D() : null;

            int pIndex = 0;
            foreach (var primitive in gltMesh.Primitives)
            {
                var curMesh = new TriangleMesh();

                Debug.Assert(primitive.Targets == null);
                CheckExtensions(primitive.Extensions);

                _tasks.Add(() =>
                {
                    curMesh.Geometry = ProcessPrimitive(primitive);
                    curMesh.Geometry.AddComponent(new AssetSource
                    {
                        Asset = CreateAsset<Geometry3D>(gltMesh.Name, "geo", id, pIndex)
                    });

                    if (gltMesh.Name == "Floor")
                    {
                        //Debugger.Break();
                    }

                    Log.Info(this, "Loaded geometry {0} ({1} bytes)", gltMesh.Name, curMesh.Geometry.Vertices.Length * Marshal.SizeOf<VertexData>());
                });


                if (primitive.Material != null)
                {
                    var gltfMat = _model!.Materials[primitive.Material.Value];
                    curMesh.Materials.Add(ProcessMaterial(gltfMat, primitive.Material.Value));

                    if (gltMesh.Name == "Floor")
                    {
                        //Debugger.Break();
                    }

                }

                if (group == null)
                {
                    _meshes[gltMesh] = curMesh;
                    return curMesh;
                }

                group.AddChild(curMesh);
            }

            pIndex++;

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
                var nodeMesh = ProcessMesh(_model!.Meshes[node.Mesh.Value], node.Mesh.Value);
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

            bool transformSet = false;
            if (node.Matrix != null)
            {
                var matrix = MathUtils.CreateMatrix(node.Matrix);
                if (!matrix.IsIdentity)
                {
                    nodeObj.Transform.Matrix = matrix;
                    transformSet = true;
                }
            }

            if (!transformSet)
            {
                if (node.Rotation != null)
                    nodeObj.Transform.Orientation = new Quaternion(node.Rotation[0], node.Rotation[1], node.Rotation[2], node.Rotation[3]);

                if (node.Scale != null)
                    nodeObj.Transform.Scale = MathUtils.ToVector3(node.Scale);

                if (node.Translation != null)
                    nodeObj.Transform.Position = MathUtils.ToVector3(node.Translation);
            }

            nodeObj.Transform.Update();

            if (nodeGrp != null && nodeGrp.Children.Count == 1 && nodeGrp.WorldMatrix.IsIdentity)
                nodeObj = nodeGrp.Children[0];

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

        public void Unload()
        {
            _buffers.Clear();
            _log.Clear();
            _images.Clear();
            _mats.Clear();
            _meshes.Clear();
        }

        public void LoadModel(string filePath, GltfLoaderOptions? options)
        {
            _options = options;
            _basePath = Path.GetDirectoryName(filePath)!;
            _filePath = filePath;
            _model = glTFLoader.Interface.LoadModel(filePath);
        }


        public Object3D Load(string filePath, GltfLoaderOptions options)
        {
            LoadModel(filePath, options);
            var result = LoadScene();
            ExecuteLoadTasks();
            return result;
        }

        public Object3D LoadScene()
        {
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

            Log.Info(this, "GLFT scene loaded '{0}'", _filePath!);


            return curRoot;
        }

        public void ExecuteLoadTasks()
        {
            Parallel.ForEach(_tasks, new ParallelOptions { MaxDegreeOfParallelism = 5 }, a => a());
            _tasks.Clear();
        }

        protected IAsset CreateAsset<T>(string name, params object[] parts)
        {
            return new BaseAsset<GltfLoaderOptions, GltfAssetLoader>(
                GltfAssetLoader.Instance,
                name,
                typeof(T),
                new Uri("res://gltf/" + string.Join('/', parts) + "?src=" + _filePath),
                _options);
        }

        public static Object3D LoadFile(string filePath)
        {
            return LoadFile(filePath, GltfLoaderOptions.Default);
        }

        public static Object3D LoadFile(string filePath, GltfLoaderOptions options)
        {
            return LoadFile(filePath, options, a => a);
        }

        public static Object3D LoadFile(string filePath, GltfLoaderOptions options, Func<string, string> resourceResolver)
        {
            var loader = new GltfLoader(resourceResolver);
            return loader.Load(filePath, options);
        }


        public glTFLoader.Schema.Gltf? Model => _model;

        public string? FilePath => _filePath;
    }
}
