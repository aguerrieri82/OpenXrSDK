﻿
using Newtonsoft.Json.Linq;
using OpenXr.Engine;
using OpenXr.Engine.Abstraction;
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

        GltfLoader()
        {

        }

        public EngineObject Load(string filePath, IAssetManager assetManager)
        {
            string[] supportedExt = { "KHR_draco_mesh_compression" };

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

            void CheckExtensions(Dictionary<string, object> ? ext)
            {
                if (ext == null)
                    return;
                foreach (var key in ext.Keys) 
                { 
                    if (!supportedExt.Contains(key))
                        log.AppendLine($"Extensions '{key}' not supported");

                }
            }

            T ? TryLoadExtension<T>(Dictionary<string, object>? ext) where T: struct
            {
                if (ext != null && ext.TryGetValue(typeof(T).Name, out var extension))
                    return ((JObject)extension).ToObject<T>();
                return null;
            }

            TextureData LoadImage(glTFLoader.Schema.Image img)
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

                var image = SkiaSharp.SKBitmap.Decode(data);

                Debug.Assert(image.ColorType == SkiaSharp.SKColorType.Bgra8888);

                return new TextureData
                {
                    Width = (uint)image.Width,
                    Height = (uint)image.Height,
                    Data = image.GetPixelSpan().ToArray(),
                    Format = TextureFormat.Bgra32
                };
            }

            Texture2D CreateTexture(TextureData data, glTFLoader.Schema.Texture texture)
            {
                var result = Texture2D.FromData([data]);
                result.Name = texture.Name;
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

            Texture2D DecodeTexture3(glTFLoader.Schema.MaterialOcclusionTextureInfo info)
            {
                var image = LoadImage(model.Images[info.Index]);

                CheckExtensions(info.Extensions);
                //info.Strength;

                return CreateTexture(image, model.Textures[info.TexCoord]);
            }

            Texture2D DecodeTexture1(glTFLoader.Schema.MaterialNormalTextureInfo info)
            {
                var image = LoadImage(model.Images[info.Index]);
                CheckExtensions(info.Extensions);
                //info.Scale
                return CreateTexture(image, model.Textures[info.TexCoord]);
            }

            Texture2D DecodeTexture2(glTFLoader.Schema.TextureInfo info)
            {
                var image = LoadImage(model.Images[info.Index]);
                CheckExtensions(info.Extensions);

                return CreateTexture(image, model.Textures[info.TexCoord]);
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
                        result.MetallicRoughness.BaseColorTexture = DecodeTexture2(gltMat.PbrMetallicRoughness.BaseColorTexture);

                    if (gltMat.PbrMetallicRoughness.MetallicRoughnessTexture != null)
                        result.MetallicRoughness.MetallicRoughnessTexture = DecodeTexture2(gltMat.PbrMetallicRoughness.MetallicRoughnessTexture);
                }

                if (gltMat.EmissiveTexture != null)
                    result.EmissiveTexture = DecodeTexture2(gltMat.EmissiveTexture);

                if (gltMat.NormalTexture != null)
                    result.NormalTexture = DecodeTexture1(gltMat.NormalTexture);

                if (gltMat.OcclusionTexture != null)
                    result.OcclusionTexture = DecodeTexture3(gltMat.OcclusionTexture);

                if (gltMat.Extensions != null)
                {
                    foreach (var ext in gltMat.Extensions)
                    {

                    }
                }

                return result;
            }

            unsafe T[] ConvertBuffer<T>(byte[] buffer, glTFLoader.Schema.BufferView view) where T : unmanaged
            {
                fixed (byte* pBuffer = buffer)
                    return new Span<T>((T*)(pBuffer + view.ByteOffset), view.ByteLength / sizeof(T)).ToArray();
            }


            Mesh ProcessMesh(glTFLoader.Schema.Mesh gltMesh)
            {
                CheckExtensions(gltMesh.Extensions);

                var result = new Mesh();

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

                                //COLOR
                                //TANGENT
                                foreach (var attr in draco.Value.Attributes)
                                {
                                    switch (attr.Key)
                                    {
                                        case "POSITION":
                                            var vValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector3 b) => a.Pos = b, vValues);
                                            break;
                                        case "NORMAL":
                                            var nValues = DracoDecoder.ReadAttribute<Vector3>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                            break;
                                        case "COLOR":
                                            log.AppendLine("Color data not supported");
                                            break;
                                        case "TEXCOORD_0":
                                            var uValues = DracoDecoder.ReadAttribute<Vector2>(mesh, attr.Value);
                                            geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
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
                                        break;
                                    case "NORMAL":
                                        var nValues = ConvertBuffer<Vector3>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector3 b) => a.Normal = b, nValues);
                                        break;
                                    case "TEXCOORD_0":
                                        var uValues = ConvertBuffer<Vector2>(buffer, view);
                                        geo.SetVertexData((ref VertexData a, Vector2 b) => a.UV = b, uValues);
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
                                        .Select(a=> (uint)a)
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