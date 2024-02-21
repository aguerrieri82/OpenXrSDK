
using Newtonsoft.Json.Linq;
using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
using SkiaSharp;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Xr.Engine.Gltf
{
    public class GltfLoader
    {
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

        GltfLoader()
        {

        }

        public EngineObject Load(string filePath, IAssetManager assetManager)
        {
            string[] supportedExt = { "KHR_draco_mesh_compression", "KHR_materials_pbrSpecularGlossiness" };

            var model = glTFLoader.Interface.LoadModel(filePath);
            var buffer = glTFLoader.Interface.LoadBinaryBuffer(model, 0, filePath);
            var mats = new Dictionary<glTFLoader.Schema.Material, PbrMaterial>();
            var images = new Dictionary<glTFLoader.Schema.Image, TextureData>();
            var log = new StringBuilder();

            var basePath = Path.GetDirectoryName(filePath); 

            Dictionary<int, byte[]> buffers = [];

            byte[] LoadBuffer(int index)
            {
                if (!buffers.TryGetValue(index, out var buffer))
                {
                    buffer = glTFLoader.Interface.LoadBinaryBuffer(model, index, filePath);
                    buffers[index] = buffer;
                }
                return buffer;
            }

            void CheckExtensions(Dictionary<string, object>? ext)
            {
                if (ext == null)
                    return;
                foreach (var key in ext.Keys)
                {
                    if (!supportedExt.Contains(key))
                        log.AppendLine($"Extensions '{key}' not supported");

                }
            }

            T? TryLoadExtension<T>(Dictionary<string, object>? ext) where T : struct
            {
                if (ext != null && ext.TryGetValue(typeof(T).Name, out var extension))
                    return ((JObject)extension).ToObject<T>();
                return null;
            }

            TextureData LoadImage(glTFLoader.Schema.Image img, bool useSrgb = false)
            {
                if (images.TryGetValue(img, out var result))
                    return result;

                CheckExtensions(img.Extensions);

                byte[] data;

                if (img.BufferView != null)
                {
                    var view = model.BufferViews[img.BufferView.Value];
                    var buffer = LoadBuffer(view.Buffer);
                    data = new Span<byte>(buffer, view.ByteOffset, view.ByteLength).ToArray();
                }
                else if (img.Uri != null)
                {
                    data = assetManager.OpenAsset(Path.Join(basePath!, img.Uri))
                        .ToMemory()
                        .ToArray();
                }
                else
                    throw new NotSupportedException();

                using var image = SKBitmap.Decode(data);
                using var newImage = new SKBitmap();
                image.CopyTo(newImage, SKColorType.Rgba8888);

                result = new TextureData
                {
                    Width = (uint)newImage.Width,
                    Height = (uint)newImage.Height,
                    Data = newImage.GetPixelSpan().ToArray(),
                    Format = newImage.ColorType switch
                    {
                        SKColorType.Srgba8888 => TextureFormat.SRgba32,
                        SKColorType.Bgra8888 => useSrgb ? TextureFormat.SBgra32 : TextureFormat.Bgra32,
                        SKColorType.Rgba8888 => useSrgb ? TextureFormat.SRgba32 : TextureFormat.Rgba32,
                        SKColorType.Gray8 => TextureFormat.Gray8,
                        _ => throw new NotSupportedException()
                    }
                };

                images[img] = result;

                return result;
            }

            Texture2D CreateTexture(TextureData data, glTFLoader.Schema.Texture texture, string? name)
            {
                var result = Texture2D.FromData([data]);
                result.Name = texture.Name ?? name;

                bool hasMinFilter = false;

                if (texture.Sampler != null)
                {
                    var sampler = model.Samplers[texture.Sampler.Value];
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

                return result;
            }

            Texture2D DecodeTextureOcclusion(glTFLoader.Schema.MaterialOcclusionTextureInfo info)
            {
                var textInfo = model.Textures[info.Index];

                var imageInfo = model.Images[textInfo.Source!.Value];

                var image = LoadImage(imageInfo);

                CheckExtensions(info.Extensions);

                return CreateTexture(image, textInfo, imageInfo.Name);
            }

            Texture2D DecodeTextureNormal(glTFLoader.Schema.MaterialNormalTextureInfo info)
            {
                var textInfo = model.Textures[info.Index];

                var imageInfo = model.Images[textInfo.Source!.Value];

                var image = LoadImage(imageInfo);

                CheckExtensions(info.Extensions);

                return CreateTexture(image, textInfo, imageInfo.Name);
            }

            Texture2D DecodeTextureBase(glTFLoader.Schema.TextureInfo info, bool useSRgb = false)
            {
                var textInfo = model.Textures[info.Index];

                var imageInfo = model.Images[textInfo.Source!.Value];

                var image = LoadImage(imageInfo, useSRgb);

                CheckExtensions(info.Extensions);

                return CreateTexture(image, textInfo, imageInfo.Name);
            }

            PbrMaterial DecodeMaterial(glTFLoader.Schema.Material gltMat)
            {
                if (mats.TryGetValue(gltMat, out var result))
                    return result;

                result = new PbrMaterial();
                result.Name = gltMat.Name;
                result.AlphaCutoff = gltMat.AlphaCutoff;

                result.EmissiveFactor = MathUtils.ToVector3(gltMat.EmissiveFactor);
                result.AlphaMode = (AlphaMode)gltMat.AlphaMode;
                result.DoubleSided = gltMat.DoubleSided;

                if (gltMat.PbrMetallicRoughness != null)
                {
                    result.MetallicRoughness = new PbrMetallicRoughness
                    {
                        RoughnessFactor = gltMat.PbrMetallicRoughness.RoughnessFactor,
                        MetallicFactor = gltMat.PbrMetallicRoughness.MetallicFactor,
                        BaseColorFactor = MathUtils.ToColor(gltMat.PbrMetallicRoughness.BaseColorFactor)
                    };

                    if (gltMat.PbrMetallicRoughness.BaseColorTexture != null)
                    {
                        result.MetallicRoughness.BaseColorTexture = DecodeTextureBase(gltMat.PbrMetallicRoughness.BaseColorTexture, true);
                        result.MetallicRoughness.BaseColorUVSet = gltMat.PbrMetallicRoughness.BaseColorTexture.TexCoord;
                    }

                    if (gltMat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                    {
                        result.MetallicRoughness.MetallicRoughnessTexture = DecodeTextureBase(gltMat.PbrMetallicRoughness.MetallicRoughnessTexture);
                        result.MetallicRoughness.MetallicRoughnessUVSet = gltMat.PbrMetallicRoughness.MetallicRoughnessTexture.TexCoord;
                    }

                    result.Type = PbrMaterialType.Metallic;
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
                    result.SpecularGlossiness = new PbrSpecularGlossiness
                    {
                        DiffuseFactor = MathUtils.ToColor(specGlos.Value.diffuseFactor),
                        SpecularFactor = MathUtils.ToColor(specGlos.Value.specularFactor),
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

                    result.Type = PbrMaterialType.Specular;
                }

                //result.MetallicRoughness.BaseColorFactor = new Color(8, 8, 8, 1);
                //result.OcclusionStrength = 1.5f;
                //result.NormalTexture = null;
                //result.OcclusionTexture = null;
                //result.MetallicRoughness!.MetallicRoughnessTexture = null;
                //result.MetallicRoughness.MetallicFactor = 0;
                //result.MetallicRoughness.RoughnessFactor = 0f;
                //result.MetallicRoughness!.BaseColorTexture = null;

                mats[gltMat] = result;

                return result;
            }

            unsafe T[] ConvertBuffer<T>(byte[] buffer, glTFLoader.Schema.BufferView view, glTFLoader.Schema.Accessor acc) where T : unmanaged
            {
                Debug.Assert(view.ByteStride == null || view.ByteStride == sizeof(T));
                Debug.Assert(acc.Sparse == null);

                fixed (byte* pBuffer = buffer)
                    return new Span<T>((T*)(pBuffer + view.ByteOffset + acc.ByteOffset), acc.Count).ToArray();
            }


            Object3D ProcessMesh(glTFLoader.Schema.Mesh gltMesh)
            {
                CheckExtensions(gltMesh.Extensions);

                var group = gltMesh.Primitives.Length > 1 ? new Group() : null;


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
                            var view = model.BufferViews[draco.Value.BufferView];
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
                                            var tValues = DracoDecoder.ReadAttribute<Vector4>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector4 b) => a.Tangent = b, tValues);
                                            geo.ActiveComponents |= VertexComponent.Tangent;
                                            break;
                                        case "TEXCOORD_0":
                                            var uValues = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                            geo.ActiveComponents |= VertexComponent.UV0;
                                            break;
                                        default:
                                            log.AppendLine($"{attr.Key} data not supported");
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
                                var acc = model.Accessors[attr.Value];
                                
                                var view = model.BufferViews[acc.BufferView!.Value];

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
                                        var tValues = ConvertBuffer<Vector4>(buffer, view, acc);
                                        geo.SetVertexData((ref VertexData a, Vector4 b) => a.Tangent = b, tValues);
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
                                        log.AppendLine($"{attr.Key} data not supported");
                                        break;
                                }

                            }

                            if (primitive.Indices != null)
                            {
                                var acc = model.Accessors[primitive.Indices.Value];

                                Debug.Assert(acc.Type == glTFLoader.Schema.Accessor.TypeEnum.SCALAR);

                                var view = model.BufferViews[acc.BufferView!.Value];

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
                            var glftMat = model.Materials[primitive.Material.Value];
                            curMesh.Materials.Add(DecodeMaterial(glftMat));
                            //curMesh.Materials.Add(new StandardMaterial() { Color = Color.White });
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
                        return curMesh;
                    
                    group.AddChild(curMesh);
                }

                return group!;
            }

            Camera ProcessCamera(glTFLoader.Schema.Camera gltCamera)
            {
                CheckExtensions(gltCamera.Extensions);
                throw new NotSupportedException();
            }

            Object3D ProcessNode(glTFLoader.Schema.Node node, Group group)
            {
                CheckExtensions(node.Extensions);

                Object3D obj;

                if (node.Mesh != null)
                {
                    obj = ProcessMesh(model.Meshes[node.Mesh.Value]);

                    Debug.Assert(node.Children == null);
                }
                else if (node.Camera != null)
                {
                    obj = ProcessCamera(model.Cameras[node.Camera.Value]);

                    Debug.Assert(node.Children == null);
                }
                else if (node.Children != null)
                {
                    obj = new Group();

                    foreach (var childNode in node.Children)
                        ProcessNode(model.Nodes[childNode], (Group)obj);
                }
                else
                    throw new NotSupportedException();

                obj.Name = node.Name;

                obj.Transform.SetMatrix(MathUtils.CreateMatrix(node.Matrix));

                group.AddChild(obj);

                return obj;
            }

            var root = new Group();

            ProcessNode(model.Nodes[0], root);

            return root;
        }

        public static readonly GltfLoader Instance = new();
    }
}
