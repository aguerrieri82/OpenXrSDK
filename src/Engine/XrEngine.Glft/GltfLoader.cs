
using Common.Interop;
using glTFLoader.Schema;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using XrEngine.Animation;
using XrEngine.Components;
using XrMath;
using static glTFLoader.Schema.AnimationSampler;
using static glTFLoader.Schema.Material;

#pragma warning disable CS0649

namespace XrEngine.Gltf
{

    public class GltfLoader : IDisposable
    {
        static readonly JsonSerializerOptions JSON_OPTIONS = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        GltfLoaderOptions _options;
        glTFLoader.Schema.Gltf? _model;

        readonly Dictionary<glTFLoader.Schema.Material, ShaderMaterial> _mats = [];
        readonly ConcurrentDictionary<Image, TextureData> _images = [];
        readonly ConcurrentDictionary<Image, LoadTask<Texture2D>> _textures = [];
        readonly Dictionary<Mesh, Object3D> _meshes = [];
        readonly List<Task> _tasks = [];
        readonly ConcurrentDictionary<int, byte[]> _buffers = [];
        readonly StringBuilder _log = new();
        readonly Func<string, string> _resourceResolver;

        readonly Dictionary<int, Object3D> _nodes = [];

        readonly Dictionary<int, GltfSkin> _skins = [];

        string? _basePath;
        string? _filePath;
        private MethodInfo? _convertBufGen;
        static readonly string[] supportedExt = {
            "KHR_texture_transform",
            "KHR_draco_mesh_compression",
            "EXT_texture_webp",
            "KHR_texture_basisu",
            "KHR_materials_pbrSpecularGlossiness" };


        #region STRUCTS

        struct EXT_texture_webp
        {
            public int? source;
        }

        struct KHR_texture_basisu
        {
            public int? source;
        }

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

        struct KHR_texture_transform
        {
            public float[]? offset;

            public float[]? scale;

            public float rotation;

            public int texCoord;
        }

        struct KHR_materials_sheen
        {
            public float[]? sheenColorFactor;

            public TextureInfo? sheenColorTexture;

            public float sheenRoughnessFactor;

            public TextureInfo? sheenRoughnessTexture;
        }

        public struct LoadTask<T>
        {
            public T Result;

            public Task Task;
        }

        public class GltfSkin
        {
            public IList<Joint3D>? Joints;

            public Guid Id;
        }

        public struct GltfSamplerValue
        {
            public float Time;

            public object Value;
        }

        public class GltfSampler
        {
            public GltfSamplerValue[]? Values;
            public InterpolationEnum Interpolation;

        }


        #endregion

        public GltfLoader()
            : this(a => a)
        {
        }

        public GltfLoader(Func<string, string> resourceResolver)
        {
            _resourceResolver = resourceResolver;
            _options = new();
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
                    LoadLog($"Extensions '{key}' not supported");
            }
        }

        protected static T? TryLoadExtension<T>(Dictionary<string, object>? ext) where T : struct
        {
            if (ext != null && ext.TryGetValue(typeof(T).Name, out var extension))
                return ((JsonElement)extension).Deserialize<T>(JSON_OPTIONS);
            return null;
        }

        protected TextureData ProcessImage(int imgId, bool useSrgb = false)
        {
            var img = _model!.Images[imgId];

            return _images.GetOrAdd(img, img =>
            {
                Log.Info(this, "Loading image {0}", img.Uri);

                try
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

                    Log.Info(this, "Loading texture {0} ({1} bytes)", img.Name, data.Length);

                    Uri uri;
                    var mimeType = img.MimeType.ToString()?.Replace('_', '/');
                    if (string.IsNullOrEmpty(mimeType))
                        uri = new Uri("file://" + img.Uri);
                    else
                        uri = AssetLoader.Instance.GetMimeUri(mimeType);

                    var loader = (ITextureLoader)AssetLoader.Instance.GetLoader(uri);

                    using var stream = new MemoryStream(data);

                    var texData = loader.LoadTexture(stream, new TextureLoadOptions
                    {
                        IsSrgb = useSrgb
                    });

                    if (texData.Count == 0)
                        throw new InvalidOperationException();

                    return texData[0];

                }
                finally
                {
                    Log.Info(this, "Loading image {0} end", img.Uri);
                }
            });
        }

        protected LoadTask<T> Load<T>(T result, Action action)
        {
            var task = new LoadTask<T>
            {
                Result = result,
                Task = Task.Run(action)
            };

            _tasks.Add(task.Task);

            return task;
        }

        public LoadTask<Texture2D> ProcessTextureTask(int texId, Dictionary<string, object>? extensions, Texture2D? result = null, bool useSrgb = false)
        {
            var texture = _model!.Textures[texId];

            CheckExtensions(texture.Extensions);

            var webP = TryLoadExtension<EXT_texture_webp>(texture.Extensions);

            var basisu = TryLoadExtension<KHR_texture_basisu>(texture.Extensions);

            texture.Source ??= webP?.source ?? basisu?.source;

            var imageInfo = _model!.Images[texture.Source!.Value];

            return _textures.GetOrAdd(imageInfo, img =>
            {
                Debug.Assert(result == null);

                var texResult = new Texture2D();

                texResult.Flags |= EngineObjectFlags.Readonly;

                texResult.Name = texture.Name ?? (imageInfo.Name ?? imageInfo.Uri ?? "");

                AssignAsset(texResult, "tex", texId);

                return Load(texResult, () =>
                {
                    var data = ProcessImage(texture.Source!.Value, useSrgb);

                    texResult.LoadData([data]);

                    var hasMinFilter = false;

                    if (texture.Sampler != null)
                    {
                        var sampler = _model!.Samplers[texture.Sampler.Value];
                        CheckExtensions(sampler.Extensions);

                        texResult.WrapS = (WrapMode)sampler.WrapS;
                        texResult.WrapT = (WrapMode)sampler.WrapT;

                        if (sampler.MagFilter != null)
                            texResult.MagFilter = (ScaleFilter)sampler.MagFilter;

                        if (sampler.MinFilter != null)
                        {
                            hasMinFilter = true;
                            texResult.MinFilter = (ScaleFilter)sampler.MinFilter;
                        }
                    }
                    else
                    {
                        texResult.WrapS = WrapMode.Repeat;
                        texResult.WrapT = WrapMode.Repeat;
                    }

                    if (!hasMinFilter)
                    {
                        texResult.MinFilter = ScaleFilter.Linear;
                        texResult.MagFilter = ScaleFilter.Linear;
                    }

                    var transform = TryLoadExtension<KHR_texture_transform>(extensions);
                    if (transform != null)
                    {
                        var mat = Matrix3x3.Identity;

                        if (transform.Value.offset != null)
                            mat *= Matrix3x3.CreateTranslation(transform.Value.offset[0], transform.Value.offset[1]);

                        if (transform.Value.rotation != 0)
                            mat *= Matrix3x3.CreateRotationZ(transform.Value.rotation);

                        if (transform.Value.scale != null)
                            mat *= Matrix3x3.CreateScale(transform.Value.scale[0], transform.Value.scale[1]);

                        texResult.Transform = mat;
                    }
                });
            });
        }

        protected LoadTask<Texture2D> DecodeTextureOcclusionTask(MaterialOcclusionTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return ProcessTextureTask(info.Index, info.Extensions);
        }

        protected LoadTask<Texture2D> DecodeTextureNormalTask(MaterialNormalTextureInfo info)
        {
            CheckExtensions(info.Extensions);

            return ProcessTextureTask(info.Index, info.Extensions);
        }

        protected LoadTask<Texture2D> DecodeTextureBaseTask(TextureInfo info, bool useSRgb = false)
        {
            CheckExtensions(info.Extensions);

            return ProcessTextureTask(info.Index, info.Extensions, null, useSRgb);
        }

        public PbrMaterial ProcessMaterial(int matId, PbrMaterial? result = null)
        {
            var gltMat = _model!.Materials[matId];

            if (result == null && _mats.TryGetValue(gltMat, out var mat))
                return (PbrMaterial)mat;

            result ??= _options.MaterialFactory(matId);

            result.Name = gltMat.Name;

            result.Alpha = gltMat.AlphaMode switch
            {
                var mode when mode == AlphaModeEnum.OPAQUE => AlphaMode.Opaque,
                var mode when mode == AlphaModeEnum.MASK => AlphaMode.Mask,
                var mode when mode == AlphaModeEnum.BLEND => AlphaMode.Blend,
                _ => throw new NotSupportedException()
            };

            result.DoubleSided = gltMat.DoubleSided;

            result.AlphaCutoff = gltMat.AlphaCutoff;

            if (gltMat.PbrMetallicRoughness != null)
            {
                if (gltMat.PbrMetallicRoughness.BaseColorTexture != null)
                {
                    result.ColorMap = DecodeTextureBaseTask(gltMat.PbrMetallicRoughness.BaseColorTexture, _options.ConvertColorTextureSRgb).Result;
                    ApplyMips(result.ColorMap);
                }

                if (gltMat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                {
                    result.MetallicRoughnessMap = DecodeTextureBaseTask(gltMat.PbrMetallicRoughness.MetallicRoughnessTexture).Result;
                    ApplyMips(result.MetallicRoughnessMap);
                }

                result.Color = new Color(gltMat.PbrMetallicRoughness.BaseColorFactor);
                result.Metalness = gltMat.PbrMetallicRoughness.MetallicFactor;
                result.Roughness = gltMat.PbrMetallicRoughness.RoughnessFactor;
            }

            if (gltMat.NormalTexture != null)
            {
                result.NormalMap = DecodeTextureNormalTask(gltMat.NormalTexture).Result;
                result.NormalMap.Type = TextureType.NormalMap;
                result.NormalScale = gltMat.NormalTexture.Scale;
            }

            if (gltMat.OcclusionTexture != null)
            {
                result.OcclusionMap = DecodeTextureOcclusionTask(gltMat.OcclusionTexture).Result;
                result.OcclusionStrength = gltMat.OcclusionTexture.Strength;
                ApplyMips(result.OcclusionMap);
            }

            if (gltMat.EmissiveTexture != null)
            {
                result.EmissiveMap = DecodeTextureBaseTask(gltMat.EmissiveTexture, true).Result;
                ApplyMips(result.EmissiveMap);
            }

            result.EmissiveColor = new Color(gltMat.EmissiveFactor);

            AssignAsset(result, "mat", matId);

            _mats[gltMat] = result;

            return result;
        }

        private void ApplyMips(Texture2D texture)
        {
            if (_options.UseMips)
            {
                texture.MipLevelCount = 10;
                texture.MinFilter = ScaleFilter.LinearMipmapLinear;
            }
            else
            {
                texture.MipLevelCount = 0;
                texture.MinFilter = ScaleFilter.Linear;
            }
        }

        Array ConvertBuffer(int accessorId)
        {
            return ConvertBuffer(_model!.Accessors[accessorId]);
        }

        Array ConvertBuffer(Accessor accessor)
        {
            Type type;

            if (accessor.Type == Accessor.TypeEnum.VEC2)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(Vector2);
                else
                    throw new NotImplementedException();
            }
            else if (accessor.Type == Accessor.TypeEnum.VEC3)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(Vector3);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    type = typeof(Vector3I);
                else
                    throw new NotImplementedException();
            }
            else if (accessor.Type == Accessor.TypeEnum.VEC4)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(Vector4);
                else if(accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                    type = typeof(Vector4US);
                else if(accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                    type = typeof(Vector4UB);
                else if(accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    type = typeof(Vector4I);
                else
                    throw new NotImplementedException();
            }
            else if (accessor.Type == Accessor.TypeEnum.MAT4)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(Matrix4x4);
                else
                    throw new NotImplementedException();
            }
            else if (accessor.Type == Accessor.TypeEnum.MAT3)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(Matrix3x3);
                else
                    throw new NotImplementedException();
            }
            else if (accessor.Type == Accessor.TypeEnum.SCALAR)
            {
                if (accessor.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                    type = typeof(float);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.SHORT)
                    type = typeof(short);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                    type = typeof(ushort);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.BYTE)
                    type = typeof(sbyte);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                    type = typeof(byte);
                else if (accessor.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                    type = typeof(uint);
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();

            _convertBufGen ??= GetType().GetMethod("ConvertBuffer", 1,
                               BindingFlags.NonPublic | BindingFlags.Instance,
                               [typeof(Accessor)])!;

            var genericMethod = _convertBufGen.MakeGenericMethod(type);

            return (Array)genericMethod.Invoke(this, [accessor])!;
        }

        T[] ConvertBuffer<T>(int accessorId) where T : unmanaged
        {
            return ConvertBuffer<T>(_model!.Accessors[accessorId]);
        }

        T[] ConvertBuffer<T>(Accessor accessor) where T : unmanaged
        {
            var view = _model!.BufferViews[accessor.BufferView!.Value];
            var buffer = LoadBuffer(view.Buffer);
            return ConvertBuffer<T>(buffer, view, accessor);
        }

        unsafe T[] ConvertBuffer<T>(byte[] buffer, BufferView view, Accessor acc) where T : unmanaged
        {
            Debug.Assert(acc.Sparse == null);

            fixed (byte* pBuffer = buffer)
            {
                if (view.ByteStride == null || view.ByteStride == sizeof(T))
                    return new Span<T>((T*)(pBuffer + view.ByteOffset + acc.ByteOffset), acc.Count).ToArray();
                else
                {
                    var curBuffer = pBuffer + view.ByteOffset + acc.ByteOffset;
                    var array = new T[acc.Count];

                    fixed (T* pArray = array)
                    {
                        for (var i = 0; i < acc.Count; i++)
                        {
                            pArray[i] = *(T*)curBuffer;
                            curBuffer += view.ByteStride.Value;
                        }
                    }
                    return array;
                }
            }
        }

        public Geometry3D ProcessPrimitive(MeshPrimitive primitive, Geometry3D? result = null)
        {
            result ??= new Geometry3D();

            result.Flags |= EngineObjectFlags.Readonly;

            var draco = TryLoadExtension<KHR_draco_mesh_compression>(primitive.Extensions);

            if (primitive.Mode == MeshPrimitive.ModeEnum.TRIANGLES)
            {
                var vertexCount = 0;
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
                                    result.SetVertexData((ref VertexData a, in Vector3 b) => a.Pos = b, vValues);
                                    result.ActiveComponents |= VertexComponent.Position;
                                    vertexCount = vValues.Length;
                                    break;
                                case "NORMAL":
                                    var nValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, in Vector3 b) => a.Normal = b, nValues);
                                    result.ActiveComponents |= VertexComponent.Normal;
                                    break;
                                case "TANGENT":
                                    if (_options != null && _options.DisableTangents)
                                        break;
                                    var tValues = DracoDecoder.ReadAttribute<Vector4>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, in Vector4 b) => a.Tangent = b, tValues);
                                    result.ActiveComponents |= VertexComponent.Tangent;
                                    break;
                                case "TEXCOORD_0":
                                    var uValues = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, in Vector2 b) => a.UV = b, uValues);
                                    result.ActiveComponents |= VertexComponent.UV0;
                                    break;
                                case "TEXCOORD_1":
                                    var uValues1 = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                    result.SetVertexData((ref VertexData a, in Vector2 b) => a.UV1 = b, uValues1);
                                    result.ActiveComponents |= VertexComponent.UV1;
                                    break;
                                case "JOINTS_0":

                                    result.EnsureComponent<SkinnedGeometry>();

                                    if (acc.Type == Accessor.TypeEnum.VEC4)
                                    {
                                        if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                                        {
                                            var jValues = DracoDecoder.ReadAttribute<Vector4US>(mesh, attr.Value);
                                            result.SetSkinData((ref SkinData a, in Vector4US b) => a.JointIndices = b.ToVector4I(), jValues);
                                            result.ActiveComponents |= VertexComponent.JointIndex;
                                        }
                                        else
                                            throw new NotSupportedException();
                                    }
                                    else
                                        throw new NotSupportedException();

                                    break;
                                default:
                                    LoadLog($"{attr.Key} data not supported");
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

                        switch (attr.Key)
                        {
                            case "POSITION":
                                var vValues = ConvertBuffer<Vector3>(acc);
                                result.SetVertexData((ref VertexData a, in Vector3 b) => a.Pos = b, vValues);
                                result.ActiveComponents |= VertexComponent.Position;
                                vertexCount = vValues.Length;
                                Debug.Assert(acc.Type == Accessor.TypeEnum.VEC3);
                                Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "NORMAL":
                                var nValues = ConvertBuffer<Vector3>(acc);
                                result.SetVertexData((ref VertexData a, in Vector3 b) => a.Normal = b, nValues);
                                result.ActiveComponents |= VertexComponent.Normal;
                                Debug.Assert(acc.Type == Accessor.TypeEnum.VEC3);
                                Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TANGENT":
                                if (_options.DisableTangents)
                                    break;
                                var tValues = ConvertBuffer<Vector4>(acc);
                                result.SetVertexData((ref VertexData a, in Vector4 b) => a.Tangent = b, tValues);
                                result.ActiveComponents |= VertexComponent.Tangent;
                                Debug.Assert(acc.Type == Accessor.TypeEnum.VEC4);
                                Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TEXCOORD_0":
                                var uValues = ConvertBuffer<Vector2>(acc);
                                result.SetVertexData((ref VertexData a, in Vector2 b) => a.UV = b, uValues);
                                result.ActiveComponents |= VertexComponent.UV0;
                                Debug.Assert(acc.Type == Accessor.TypeEnum.VEC2);
                                Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "TEXCOORD_1":
                                var uValues1 = ConvertBuffer<Vector2>(acc);
                                result.SetVertexData((ref VertexData a, in Vector2 b) => a.UV1 = b, uValues1);
                                result.ActiveComponents |= VertexComponent.UV1;
                                Debug.Assert(acc.Type == Accessor.TypeEnum.VEC2);
                                Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);
                                break;
                            case "WEIGHTS_0":

                                result.EnsureComponent<SkinnedGeometry>();

                                if (acc.Type == Accessor.TypeEnum.VEC4)
                                {
                                    if (acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT)
                                    {
                                        var wValues = ConvertBuffer<Vector4>(acc);
                                        result.SetSkinData((ref SkinData a, in Vector4 b) => a.JointWeights = b, wValues);
                                    }
                                    else
                                        throw new NotSupportedException();
                                }
                                else
                                    throw new NotSupportedException();

                                result.ActiveComponents |= VertexComponent.JointWeight;

                                break;
                            case "JOINTS_0":

                                result.EnsureComponent<SkinnedGeometry>();

                                if (acc.Type == Accessor.TypeEnum.SCALAR)
                                {
                                    if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                                    {
                                        var jValues = ConvertBuffer<ushort>(acc);

                                        result.SetSkinData((ref SkinData a, in ushort b) =>
                                        {
                                            a.JointIndices.X = b;
                                            a.JointWeights.X = 1;
                                        }, jValues);

                                        result.ActiveComponents |= VertexComponent.Skin;
                                    }
                                    else
                                        throw new NotSupportedException();
                                }
                                else if (acc.Type == Accessor.TypeEnum.VEC4)
                                {
                                    if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                                    {
                                        var jValues1 = ConvertBuffer<Vector4US>(acc);
                                        result.SetSkinData((ref SkinData a, in Vector4US b) => a.JointIndices = b.ToVector4I(), jValues1);
                                    }
                                    else if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                                    {
                                        var jValues2 = ConvertBuffer<Vector4UB>(acc);
                                        result.SetSkinData((ref SkinData a, in Vector4UB b) => a.JointIndices = b.ToVector4I(), jValues2);
                                    }
                                    else
                                        throw new NotSupportedException();

                                    result.ActiveComponents |= VertexComponent.JointIndex;
                                }
                                else
                                    throw new NotSupportedException();
                                break;
                            default:
                                LoadLog($"{attr.Key} data not supported");
                                break;
                        }

                    }

                    if (primitive.Indices != null)
                    {
                        var acc = _model!.Accessors[primitive.Indices.Value];

                        Debug.Assert(acc.Type == Accessor.TypeEnum.SCALAR);

                        if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_SHORT)
                            result.Indices = ConvertBuffer<ushort>(acc)
                                .Select(a => (uint)a)
                                .ToArray();
                        
                        else if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_INT)
                            result.Indices = ConvertBuffer<uint>(acc);

                        else if (acc.ComponentType == Accessor.ComponentTypeEnum.UNSIGNED_BYTE)
                            result.Indices = ConvertBuffer<byte>(acc)
                                .Select(a => (uint)a)
                                .ToArray();
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
                result.ComputeTangents();
            }

            if (_options.GeometryGpuOnly)
                result.Flags |= EngineObjectFlags.GpuOnly;

            if (primitive.Targets != null && primitive.Targets.Length > 0)
            {
                var geoMorph = result.EnsureComponent<MorphedGeometry>();

                geoMorph.Targets = new MorphTarget[primitive.Targets.Length];

                var iTarget = 0;

                foreach (var target in primitive.Targets)
                {
                    var morphTarget = new MorphTarget()
                    {
                        Components = new MorphComponent[target.Count]
                    };

                    var iComp = 0;

                    foreach (var attr in target)
                    {
                        var acc = _model!.Accessors[attr.Value];

                        Debug.Assert(acc.Type == Accessor.TypeEnum.VEC3);
                        Debug.Assert(acc.ComponentType == Accessor.ComponentTypeEnum.FLOAT);

                        var morphComp = new MorphComponent
                        {
                            Values = ConvertBuffer<Vector3>(acc)
                        };

                        switch (attr.Key)
                        {
                            case "POSITION":
                                morphComp.Component = VertexComponent.MorphPosition;
                                break;
                            case "NORMAL":
                                morphComp.Component = VertexComponent.MorphNormal;
                                break;
                            case "TANGENT":
                                morphComp.Component = VertexComponent.MorphTangent;
                                break;
                            default:
                                throw new NotSupportedException();
                        }

                        morphTarget.Components[iComp] = morphComp;

                        iComp++;
                    }

                    geoMorph.Targets[iTarget] = morphTarget;

                    iTarget++;
                }
            }

            return result;
        }

        protected void LoadLog(string text)
        {
            lock (_log)
                _log.AppendLine(text);
        }

        static Object3D Clone(Object3D obj)
        {
            Object3D result;

            if (obj is TriangleMesh mesh)
            {
                var newMesh = new TriangleMesh
                {
                    Geometry = mesh.Geometry
                };

                foreach (var mat in mesh.Materials)
                    newMesh.Materials.Add(mat);

                result = newMesh;
            }
            else if (obj is Group3D group)
            {
                var newGroup = new Group3D();

                foreach (var child in group.Children)
                    newGroup.AddChild(Clone(child));
                result = newGroup;
            }
            else
                throw new NotSupportedException();

            result.Name = obj.Name;
            result.Transform.Set(obj.Transform.Matrix);

            return result;
        }

        public Object3D ProcessMesh(int meshId, Node? node, Object3D? result = null)
        {
            var gltMesh = _model!.Meshes[meshId];

            if (result == null && _meshes.TryGetValue(gltMesh, out result))
            {
                if (_options.UseInstances)
                    return new Object3DInstance() { Reference = result };

                return Clone(result);
            }

            CheckExtensions(gltMesh.Extensions);

            var group = gltMesh.Primitives.Length > 1 ? new Group3D() : null;

            var pIndex = 0;

            foreach (var primitive in gltMesh.Primitives)
            {
                var curMesh = new TriangleMesh()
                {
                    Geometry = new Geometry3D()
                };

                if (node?.Skin != null)
                {
                    var skin = _skins[node.Skin.Value];

                    curMesh.AddComponent(new MeshSkin()
                    {
                        Joints = skin.Joints?.ToArray() ?? [],
                        SkinId = skin.Id
                    });
                }

                var weights = node?.Weights ?? gltMesh.Weights;

                if (weights != null && weights.Length > 0)
                {
                    curMesh.AddComponent(new MeshMorph()
                    {
                        Weights = weights
                    });
                }

                CheckExtensions(primitive.Extensions);

                Load(curMesh, () =>
                {
                    var geo = ProcessPrimitive(primitive, curMesh.Geometry);

                    AssignAsset(geo, gltMesh.Name, "geo", meshId, pIndex);

                    curMesh.Geometry = geo;

                    Log.Info(this, "Loaded geometry {0} ({1} bytes)", gltMesh.Name,
                        curMesh.Geometry.Vertices.Length * MarshalCache.SizeOf(typeof(VertexData)));
                });

                if (primitive.Material != null)
                {
                    var mat = ProcessMaterial(primitive.Material.Value);
                    mat.Skin = SkinMode.Static;
                    mat.HasSkin = node?.Skin != null;
                    mat.HasMorph = weights != null && weights.Length > 0;
                    curMesh.Materials.Add(mat);
                }


                if (group == null)
                {
                    _meshes[gltMesh] = curMesh;
                    GenerateId(curMesh, "mesh", meshId);
                    return curMesh;
                }

                group.AddChild(curMesh);
            }

            pIndex++;

            _meshes[gltMesh] = group!;

            GenerateId(group!, "mesh", meshId);

            return group!;
        }

        protected Camera ProcessCamera(int cameraId)
        {
            var camera = _model!.Cameras[cameraId];
            var cameraObj = new PerspectiveCamera();

            CheckExtensions(camera.Extensions);
            LoadLog("Camera not supported!");

            return cameraObj;
        }

        protected Object3D ProcessNode(int nodeId, Group3D? curGrp, bool isJoint)
        {
            if (_nodes.TryGetValue(nodeId, out var nodeObj))
            {
                if (nodeObj.Parent == null)
                    curGrp?.AddChild(nodeObj);
                return nodeObj;
            }

            var node = _model!.Nodes[nodeId];

            CheckExtensions(node.Extensions);

            Group3D? nodeGrp = null;

            if (isJoint || (node.Children != null && node.Children.Length > 0))
            {
                nodeGrp = isJoint ? new Joint3D() : new Group3D();
                nodeObj = nodeGrp;
            }

            if (node.Mesh != null)
            {
                Object3D nodeMesh = ProcessMesh(node.Mesh.Value, node);

                if (nodeGrp != null)
                    nodeGrp.AddChild(nodeMesh);
                else
                    nodeObj = nodeMesh;
            }
            else if (node.Camera != null)
            {
                nodeObj = ProcessCamera(node.Camera.Value);

                Debug.Assert(node.Children == null);
            }
            else if (nodeGrp == null)
            {
                nodeObj = new Object3D();
            }

            if (nodeGrp != null && node.Children != null)
            {
                foreach (var childNode in node.Children)
                    ProcessNode(childNode, nodeGrp, isJoint);
            }

            nodeObj!.Name = node.Name;

            var transformSet = false;
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

            curGrp?.AddChild(nodeObj);

            GenerateId(nodeObj, "node", nodeId);

            _nodes[nodeId] = nodeObj;

            return nodeObj;
        }

        protected GltfSkin ProcessSkin(int skinId)
        {
            var skin = _model!.Skins[skinId];

            CheckExtensions(skin.Extensions);

            var skinObj = new GltfSkin
            {
                Joints = [],
                Id = Guid.NewGuid()
            };

            Matrix4x4[]? matrices = null;

            if (skin.InverseBindMatrices != null)
            {
                var matsAcc = _model.Accessors[skin.InverseBindMatrices.Value];
                matrices = ConvertBuffer<Matrix4x4>(matsAcc);
            }

            foreach (var joint in skin.Joints)
            {
                var jointObj = (Joint3D)ProcessNode(joint, null, true);

                skinObj.Joints.Add(jointObj);
            }

            Debug.Assert(matrices != null && matrices.Length == skinObj.Joints.Count);

            for (var i = 0; i < skinObj.Joints.Count;i++)
                skinObj.Joints[i].InverseBindMatrix = matrices[i];

            _skins[skinId] = skinObj;

            return skinObj;
        }

        protected void ProcessAnimation(glTFLoader.Schema.Animation anim, Object3D root)
        {
            Debug.Assert(_model != null);

            CheckExtensions(anim.Extensions);

            var samplers = new List<GltfSampler>();

            foreach (var sampler in anim.Samplers)
            {
                CheckExtensions(sampler.Extensions);

                var times = ConvertBuffer<float>(sampler.Input);
                var values = ConvertBuffer(sampler.Output);

                if (values.Length % times.Length != 0)
                    throw new InvalidOperationException();

                var ratio = values.Length / times.Length;
                var valueType = values.GetType().GetElementType()!;

                var gltfSampler = new GltfSampler
                {
                    Interpolation = sampler.Interpolation,
                    Values = [.. times.Select((time, i) =>
                    {
                        object value;

                        if (ratio == 1)
                            value = values.GetValue(i)!;
                        else
                        {
                            var array = Array.CreateInstance(valueType, ratio);
                            Array.Copy(values, i * ratio, array, 0, ratio);
                            value = array;
                        }

                        return new GltfSamplerValue
                        {
                            Time = time,
                            Value = value
                        };
                    })]
                };

                samplers.Add(gltfSampler);
            }


            var group = new AnimationGroup()
            {
                IterationCount = 1
            };

            foreach (var channel in anim.Channels)
            {
                CheckExtensions(channel.Extensions);
                CheckExtensions(channel.Target.Extensions);

                if (channel.Target.Node == null)
                    continue;

                var sampler = samplers[channel.Sampler];
                var obj3d = _nodes[channel.Target.Node.Value];
                var path = channel.Target.Path;

                Debug.Assert(sampler.Values != null);

                TimeFunctionDelegate timeFunc;

                if (sampler.Interpolation == InterpolationEnum.STEP)
                    timeFunc = TimeFunctions.Step;
                else if (sampler.Interpolation == InterpolationEnum.LINEAR)
                    timeFunc = TimeFunctions.Linear;
                else
                    throw new NotSupportedException();

                if (path == "scale")
                {
                    group.Add(new StepAnimation<Vector3>()
                    {
                        Steps = [.. sampler.Values.Select(a => new AnimationStep<Vector3>
                        {
                            Time = a.Time,
                            Value = (Vector3)a.Value,
                            TimeFunction = timeFunc
                        })],
                        IterationCount = 1,
                        Name = anim.Name,
                        SetTarget = t => obj3d.Transform.Scale = t.Value
                    });
                }
                else if (path == "translation")
                {
                    group.Add(new StepAnimation<Vector3>()
                    {
                        Steps = [.. sampler.Values.Select(a => new AnimationStep<Vector3>
                        {
                            Time = a.Time,
                            Value = (Vector3)a.Value,
                            TimeFunction = timeFunc
                        })],
                        IterationCount = 1,
                        Name = anim.Name,
                        SetTarget = t => obj3d.Transform.Position = t.Value
                    });
                }
                else if (path == "rotation")
                {
                    group.Add(new StepAnimation<Quaternion>()
                    {
                        Steps = [.. sampler.Values.Select(a => new AnimationStep<Quaternion>
                        {
                            Time = a.Time,
                            Value = ((Vector4)a.Value).ToQuaternion(),
                            TimeFunction = timeFunc
                        })],
                        IterationCount = 1,
                        Name = anim.Name,
                        SetTarget = t => obj3d.Transform.Orientation = t.Value
                    });
                }
                else if (path == "weights")
                {
                    group.Add(new StepAnimation<float[]>()
                    {
                        Steps = [.. sampler.Values.Select(a => new AnimationStep<float[]>
                        {
                            Time = a.Time,
                            Value = (float[])a.Value,
                            TimeFunction = timeFunc
                        })],

                        IterationCount = 1,
                        Name = anim.Name,
                        SetTarget = t =>
                        {
                            foreach (var meshMorph in obj3d.ComponentsDeep<MeshMorph>())
                                meshMorph.Weights = t.Value;
                        }
                    });
                }
                else
                    throw new NotSupportedException();
            }

            if (!root.TryComponent<AnimationsHost>(out var animHost))
                animHost = root.AddComponent<AnimationsHost>();

            animHost.AddAnimation(group);
        }

        protected void ProcessAnimations(Object3D root)
        {
            if (_model?.Animations == null)
                return;

            foreach (var anim in _model.Animations)
                ProcessAnimation(anim, root);
        }

        protected Group3D ProcessScene(Scene glScene)
        {
            var scene = new Group3D();

            foreach (var nodeId in glScene.Nodes)
                ProcessNode(nodeId, scene, false);


            return scene;
        }

        public void Dispose()
        {
            _buffers.Clear();
            _log.Clear();
            _images.Clear();
            _mats.Clear();
            _meshes.Clear();
            _textures.Clear();
            _skins.Clear();
            _nodes.Clear();

            GC.SuppressFinalize(this);
        }

        internal void LoadModel(string filePath, GltfLoaderOptions? options)
        {
            if (options != null)
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

            if (_model!.Skins != null)
            {
                for (var i = 0; i < _model.Skins.Length; i++)
                    ProcessSkin(i);
            }

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


            ProcessAnimations(curRoot);

            Log.Info(this, "GLFT scene loaded '{0}'", _filePath!);

            return curRoot;
        }

        public void ExecuteLoadTasks()
        {
            Task.WaitAll(_tasks.ToArray());

            _tasks.Clear();
        }

        protected void GenerateId(EngineObject obj, params object[] parts)
        {
            var text = string.Join('|', parts) + "|" + _filePath;
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(text));
            //TODO: id must be unqiue per scene, multiple instances can have same id
            //obj.Id = new Guid(hash);
        }

        protected void AssignAsset<T>(T obj, string name, params object[] parts) where T : EngineObject
        {
            obj.AddComponent(new AssetSource
            {
                Asset = new BaseAsset<GltfLoaderOptions, GltfAssetLoader>(
                    GltfAssetLoader.Instance,
                    name,
                    typeof(T),
                    new Uri("res://gltf/" + string.Join('/', parts) + "?src=" + _filePath),
                    _options)
            });

            GenerateId(obj, parts);
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
