
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
            var log = new StringBuilder();

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
                    data = assetManager.OpenAsset(img.Uri)
                        .ToMemory()
                        .ToArray();
                }
                else
                    throw new NotSupportedException();

                using var image = SKBitmap.Decode(data);
                using var newImg = new SKBitmap(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                image.CopyTo(newImg);   

                //using var stream = File.OpenWrite("d:\\" + img.Name + ".png");
                //image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);

                return new TextureData
                {
                    Width = (uint)image.Width,
                    Height = (uint)image.Height,
                    Data = newImg.GetPixelSpan().ToArray(),
                    Format = newImg.ColorType switch
                    {
                        SKColorType.Bgra8888 => useSrgb ? TextureFormat.SBgra32 : TextureFormat.Bgra32,
                        SKColorType.Rgba8888 => TextureFormat.Rgba32,
                        SKColorType.Gray8 => TextureFormat.Gray8,
                        _ => throw new NotSupportedException()
                    }
                };
            }

            Texture2D CreateTexture(TextureData data, glTFLoader.Schema.Texture texture, string? name)
            {
                var result = Texture2D.FromData([data]);
                result.Name = texture.Name ?? name;
                if (texture.Sampler != null)
                {
                    var sampler = model.Samplers[texture.Sampler.Value];
                    CheckExtensions(sampler.Extensions);

                    result.WrapS = (WrapMode)sampler.WrapS;
                    result.WrapT = (WrapMode)sampler.WrapT;
                    if (sampler.MagFilter != null)
                        result.MagFilter = (ScaleFilter)sampler.MagFilter;
                    if (sampler.MinFilter != null)
                        result.MinFilter = (ScaleFilter)sampler.MinFilter;
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

            Material DecodeMaterial(glTFLoader.Schema.Material gltMat)
            {
                var result = new PbrMaterial();
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

                //result.NormalTexture = null;
                //result.OcclusionTexture = null;
                //result.MetallicRoughness!.MetallicRoughnessTexture = null;
                //result.MetallicRoughness!.BaseColorTexture = null;
                return result;
            }

            unsafe T[] ConvertBuffer<T>(byte[] buffer, glTFLoader.Schema.BufferView view) where T : unmanaged
            {
                fixed (byte* pBuffer = buffer)
                    return new Span<T>((T*)(pBuffer + view.ByteOffset), view.ByteLength / sizeof(T)).ToArray();
            }


            TriangleMesh ProcessMesh(glTFLoader.Schema.Mesh gltMesh)
            {
                CheckExtensions(gltMesh.Extensions);

                var result = new TriangleMesh();

                foreach (var primitive in gltMesh.Primitives)
                {
                    CheckExtensions(primitive.Extensions);

                    var draco = TryLoadExtension<KHR_draco_mesh_compression>(primitive.Extensions);

                    if (primitive.Mode == glTFLoader.Schema.MeshPrimitive.ModeEnum.TRIANGLES)
                    {
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

                                result.Geometry = geo;

                                foreach (var attr in draco.Value.Attributes)
                                {
                                    switch (attr.Key)
                                    {
                                        case "POSITION":
                                            var vValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                            geo.ActiveComponents |= VertexComponent.Position;
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
                                var view = model.BufferViews[attr.Value];
                                var buffer = LoadBuffer(view.Buffer);
                                switch (attr.Key)
                                {
                                    case "POSITION":
                                        var vValues = ConvertBuffer<Vector3>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                        geo.ActiveComponents |= VertexComponent.Position;
                                        break;
                                    case "NORMAL":
                                        var nValues = ConvertBuffer<Vector3>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                        geo.ActiveComponents |= VertexComponent.Normal;
                                        break;
                                    case "TANGENT":
                                        var tValues = ConvertBuffer<Vector4>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector4 b) => a.Tangent = b, tValues);
                                        geo.ActiveComponents |= VertexComponent.Tangent;
                                        break;
                                    case "TEXCOORD_0":
                                        var uValues = ConvertBuffer<Vector2>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
                                        geo.ActiveComponents |= VertexComponent.UV0;
                                        break;
                                    default:
                                        log.AppendLine($"{attr.Key} data not supported");
                                        break;
                                }

                            }

                            if (primitive.Indices != null)
                            {
                                var view = model.BufferViews[primitive.Indices.Value];
                                var buffer = LoadBuffer(view.Buffer);

                                if (buffer.Length / 16 <= ushort.MaxValue)
                                    geo.Indices = ConvertBuffer<ushort>(buffer, view)
                                        .Select(a => (uint)a)
                                        .ToArray();
                                else
                                    geo.Indices = ConvertBuffer<uint>(buffer, view);
                            }

                            result.Geometry = geo;
                        }

                        if (primitive.Material != null)
                        {
                            var glftMat = model.Materials[primitive.Material.Value];
                            result.Materials.Add(DecodeMaterial(glftMat));
                        }
                    }
                    else
                        throw new NotSupportedException();
                }

                return result;
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
