using Common.Interop;
using glTFLoader.Schema;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using XrEngine.Components;
using XrEngine.Compression;
using GltfAccessor = glTFLoader.Schema.Accessor;
using GltfBufferView = glTFLoader.Schema.BufferView;
using GltfImage = glTFLoader.Schema.Image;
using GltfMaterial = glTFLoader.Schema.Material;
using GltfMesh = glTFLoader.Schema.Mesh;
using GltfNode = glTFLoader.Schema.Node;
using GltfSampler = glTFLoader.Schema.Sampler;
using GltfSkin = glTFLoader.Schema.Skin;
using GltfTexture = glTFLoader.Schema.Texture;
using GltfTextureInfo = glTFLoader.Schema.TextureInfo;

namespace XrEngine.Gltf
{

    [AIGenerated]
    public class GltfExporter
    {
        readonly List<GltfNode> _nodes = [];
        readonly List<GltfMesh> _meshes = [];
        readonly List<GltfMaterial> _materials = [];
        readonly List<GltfTexture> _textures = [];
        readonly List<GltfImage> _images = [];
        readonly List<GltfSampler> _samplers = [];
        readonly List<GltfAccessor> _accessors = [];
        readonly List<GltfBufferView> _bufferViews = [];
        readonly List<GltfSkin> _skins = [];

        readonly Dictionary<Object3D, int> _nodeIds = [];
        readonly List<Object3D> _nodeObjects = [];
        readonly Dictionary<TriangleMesh, int> _meshIds = [];
        readonly Dictionary<PbrMaterial, int> _materialIds = [];
        readonly Dictionary<Texture2D, int> _textureIds = [];
        readonly Dictionary<MeshSkin, int> _skinIds = [];
        readonly List<int> _sceneRoots = [];

        readonly MemoryStream _bin = new();
        Matrix4x4 _exportFrameInv = Matrix4x4.Identity;
        readonly IMemoryBuffer<byte>[] _readBuffers = [MemoryBuffer.Create<byte>(16), MemoryBuffer.Create<byte>(16)];

        public async Task ExportAsync(Object3D root, Stream output)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(output);

            if (!output.CanWrite)
                throw new ArgumentException("Output stream is not writable.", nameof(output));

            await EngineApp.RenderThread;

            Reset();

            if (root.Parent != null && !Matrix4x4.Invert(root.Parent.WorldMatrix, out _exportFrameInv))
                throw new InvalidOperationException("Cannot invert export root parent world matrix.");

            var rootNode = BuildNodeTree(root);
            _sceneRoots.Add(rootNode);

            for (var i = 0; i < _nodeObjects.Count; i++)
                ProcessNodeContent(_nodeObjects[i], _nodes[i]);

            var model = BuildModel();

            using var writer = new BinaryWriter(output, Encoding.UTF8, true);
            glTFLoader.Interface.SaveBinaryModel(model, _bin.ToArray(), writer);
        }

        public async Task ExportAsync(Object3D root, string path)
        {
            await using var stream = File.Create(path);
            await ExportAsync(root, stream);
        }

        void Reset()
        {
            _nodes.Clear();
            _meshes.Clear();
            _materials.Clear();
            _textures.Clear();
            _images.Clear();
            _samplers.Clear();
            _accessors.Clear();
            _bufferViews.Clear();
            _skins.Clear();

            _nodeIds.Clear();
            _nodeObjects.Clear();
            _meshIds.Clear();
            _materialIds.Clear();
            _textureIds.Clear();
            _skinIds.Clear();
            _sceneRoots.Clear();

            _exportFrameInv = Matrix4x4.Identity;
            _bin.SetLength(0);
            _bin.Position = 0;
        }

        int BuildNodeTree(Object3D obj)
        {
            if (_nodeIds.TryGetValue(obj, out var existing))
                return existing;

            var node = new GltfNode();
            var nodeId = _nodes.Count;

            _nodeIds.Add(obj, nodeId);
            _nodeObjects.Add(obj);
            _nodes.Add(node);

            if (!string.IsNullOrWhiteSpace(obj.Name))
                node.Name = obj.Name;

            WriteTransform(node, obj.Transform.Matrix);

            if (obj is Group3D group && group.Children.Count > 0)
            {
                node.Children = new int[group.Children.Count];

                for (var i = 0; i < group.Children.Count; i++)
                    node.Children[i] = BuildNodeTree(group.Children[i]);
            }

            return nodeId;
        }

        void ProcessNodeContent(Object3D obj, GltfNode node)
        {
            if (obj is not TriangleMesh mesh)
                return;

            if (mesh.Geometry == null)
                return;

            node.Mesh = ProcessMesh(mesh);

            if (mesh.TryComponent<MeshSkin>(out var skin))
                node.Skin = ProcessSkin(skin);
        }

        int ProcessMesh(TriangleMesh mesh)
        {
            var geometry = mesh.Geometry!;

            if (_meshIds.TryGetValue(mesh, out var existing))
                return existing;

            var vertices = ReadVertices(mesh);
            var indices = ReadIndices(mesh);

            if (vertices.Length == 0)
                throw new InvalidOperationException($"Mesh '{mesh.Name}' has no vertices.");

            var attributes = new Dictionary<string, int>
            {
                ["POSITION"] = AddVertexAccessor(vertices, a => a.Pos, true)
            };

            if ((geometry.ActiveComponents & VertexComponent.Normal) != 0)
                attributes["NORMAL"] = AddVertexAccessor(vertices, a => a.Normal);

            if ((geometry.ActiveComponents & VertexComponent.Tangent) != 0)
                attributes["TANGENT"] = AddVertexAccessor(vertices, a => a.Tangent);

            if ((geometry.ActiveComponents & VertexComponent.UV0) != 0)
                attributes["TEXCOORD_0"] = AddVertexAccessor(vertices, a => a.UV);

            if ((geometry.ActiveComponents & VertexComponent.UV1) != 0)
                attributes["TEXCOORD_1"] = AddVertexAccessor(vertices, a => a.UV1);

            geometry.TryComponent<SkinnedGeometry>(out var skinned);
            mesh.TryComponent<MeshSkin>(out var meshSkin);

            if (skinned != null && meshSkin == null)
                throw new InvalidOperationException($"Mesh '{mesh.Name}' has skin vertex attributes but no MeshSkin component.");

            if (meshSkin != null && (skinned?.Skin == null || skinned.Skin.Length == 0))
                throw new InvalidOperationException($"Mesh '{mesh.Name}' has a MeshSkin component but no skin vertex attributes.");

            if (skinned?.Skin != null && skinned.Skin.Length > 0)
            {
                if (skinned.Skin.Length != vertices.Length)
                    throw new InvalidOperationException($"Mesh '{mesh.Name}' skin vertex count does not match geometry vertex count.");

                attributes["JOINTS_0"] = AddJointAccessor(skinned.Skin);
                attributes["WEIGHTS_0"] = AddSkinWeightAccessor(skinned.Skin);
            }

            var primitive = new MeshPrimitive
            {
                Mode = MeshPrimitive.ModeEnum.TRIANGLES,
                Attributes = attributes
            };

            if (indices.Length > 0)
            {
                foreach (var index in indices)
                {
                    if (index >= vertices.Length)
                        throw new InvalidOperationException($"Mesh '{mesh.Name}' contains index {index} outside the vertex buffer.");
                }

                primitive.Indices = AddIndexAccessor(indices);
            }

            var material = mesh.Materials.OfType<PbrMaterial>().FirstOrDefault(a => a.IsEnabled) ??
                           mesh.Materials.OfType<PbrMaterial>().FirstOrDefault();

            if (material != null)
                primitive.Material = ProcessMaterial(material);

            geometry.TryComponent<MorphedGeometry>(out var morph);

            if (morph?.Targets != null && morph.Targets.Length > 0)
            {
                var targets = new Dictionary<string, int>[morph.Targets.Length];

                for (var i = 0; i < morph.Targets.Length; i++)
                {
                    var target = morph.Targets[i];
                    var glTarget = new Dictionary<string, int>();

                    foreach (var component in target.Components)
                    {
                        if (component.Values == null || component.Values.Length == 0)
                            continue;

                        if (component.Values.Length != vertices.Length)
                            throw new InvalidOperationException($"Mesh '{mesh.Name}' morph target vertex count does not match geometry vertex count.");

                        switch (component.Component)
                        {
                            case VertexComponent.MorphPosition:
                                glTarget["POSITION"] = AddAccessor(component.Values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC3);
                                break;

                            case VertexComponent.MorphNormal:
                                glTarget["NORMAL"] = AddAccessor(component.Values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC3);
                                break;

                            case VertexComponent.MorphTangent:
                                glTarget["TANGENT"] = AddAccessor(component.Values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC3);
                                break;

                            case VertexComponent.MorphUV0:
                                throw new NotSupportedException("MorphUV0 is not a core glTF morph target semantic.");

                            default:
                                throw new NotSupportedException($"Morph component '{component.Component}' is not supported by the GLB exporter.");
                        }
                    }

                    if (glTarget.Count == 0)
                        throw new InvalidOperationException($"Mesh '{mesh.Name}' contains an empty morph target.");

                    targets[i] = glTarget;
                }

                primitive.Targets = targets;
            }

            var glMesh = new GltfMesh
            {
                Primitives = [primitive]
            };

            if (!string.IsNullOrWhiteSpace(mesh.Name))
                glMesh.Name = mesh.Name;

            mesh.TryComponent<MeshMorph>(out var meshMorph);

            if (meshMorph?.Weights != null && meshMorph.Weights.Length > 0)
            {
                if (morph?.Targets == null || meshMorph.Weights.Length != morph.Targets.Length)
                    throw new InvalidOperationException($"Mesh '{mesh.Name}' morph weight count does not match morph target count.");

                glMesh.Weights = meshMorph.Weights;
            }

            if (morph != null && morph.Targets!.Length > 0)
            {
                glMesh.Extras = JsonSerializer.SerializeToElement(new
                {
                    targetNames = morph.Targets.Select(a => a.Name ?? "").ToArray()
                });
            }

            var meshId = _meshes.Count;
            _meshIds.Add(mesh, meshId);
            _meshes.Add(glMesh);

            return meshId;
        }

        int ProcessSkin(MeshSkin skin)
        {
            if (_skinIds.TryGetValue(skin, out var existing))
                return existing;

            if (skin.Joints == null || skin.Joints.Length == 0)
                throw new InvalidOperationException("MeshSkin has no joints.");

            var jointIds = new int[skin.Joints.Length];
            var jointSet = skin.Joints.Cast<Object3D>().ToHashSet();

            for (var i = 0; i < skin.Joints.Length; i++)
                jointIds[i] = EnsureSkinJointNode(skin.Joints[i], jointSet);

            var glSkin = new GltfSkin
            {
                Joints = jointIds
            };

            if (skin.InverseBindMatrices != null && skin.InverseBindMatrices.Length > 0)
            {
                if (skin.InverseBindMatrices.Length != skin.Joints.Length)
                    throw new InvalidOperationException("Inverse bind matrix count does not match skin joint count.");

                glSkin.InverseBindMatrices = AddAccessor(skin.InverseBindMatrices, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.MAT4);
            }

            var skinId = _skins.Count;
            _skinIds.Add(skin, skinId);
            _skins.Add(glSkin);

            return skinId;
        }

        int EnsureSkinJointNode(Object3D joint, HashSet<Object3D> jointSet)
        {
            if (_nodeIds.TryGetValue(joint, out var existing))
                return existing;

            Object3D? parent = joint.Parent;

            while (parent != null && !jointSet.Contains(parent) && !_nodeIds.ContainsKey(parent))
                parent = parent.Parent;

            int? parentNode = null;
            Matrix4x4 matrix;

            if (parent != null)
            {
                parentNode = _nodeIds.TryGetValue(parent, out var existingParent)
                    ? existingParent
                    : EnsureSkinJointNode(parent, jointSet);

                if (!Matrix4x4.Invert(parent.WorldMatrix, out var parentWorldInv))
                    throw new InvalidOperationException($"Cannot invert parent world matrix for skin joint '{joint.Name}'.");

                matrix = joint.WorldMatrix * parentWorldInv;
            }
            else
                matrix = joint.WorldMatrix * _exportFrameInv;

            var node = new GltfNode();
            var nodeId = _nodes.Count;

            _nodeIds.Add(joint, nodeId);
            _nodeObjects.Add(joint);
            _nodes.Add(node);

            if (!string.IsNullOrWhiteSpace(joint.Name))
                node.Name = joint.Name;

            WriteTransform(node, matrix);

            if (parentNode != null)
                AddNodeChild(parentNode.Value, nodeId);
            else if (!_sceneRoots.Contains(nodeId))
                _sceneRoots.Add(nodeId);

            return nodeId;
        }

        void AddNodeChild(int parentNode, int childNode)
        {
            var parent = _nodes[parentNode];

            if (parent.Children == null)
            {
                parent.Children = [childNode];
                return;
            }

            if (Array.IndexOf(parent.Children, childNode) >= 0)
                return;

            var children = new int[parent.Children.Length + 1];
            parent.Children.CopyTo(children, 0);
            children[^1] = childNode;
            parent.Children = children;
        }

        int ProcessMaterial(PbrMaterial material)
        {
            if (_materialIds.TryGetValue(material, out var existing))
                return existing;

            var pbr = new MaterialPbrMetallicRoughness
            {
                BaseColorFactor = [Clamp01(material.Color.R), Clamp01(material.Color.G), Clamp01(material.Color.B), Clamp01(material.Color.A)],
                MetallicFactor = Clamp01(material.Metalness),
                RoughnessFactor = Clamp01(material.Roughness)
            };

            if (material.ColorMap != null)
                pbr.BaseColorTexture = TextureInfo(material.ColorMap);

            if (material.MetallicRoughnessMap != null)
                pbr.MetallicRoughnessTexture = TextureInfo(material.MetallicRoughnessMap);

            var glMaterial = new GltfMaterial
            {
                PbrMetallicRoughness = pbr,
                DoubleSided = material.DoubleSided
            };

            if (!string.IsNullOrWhiteSpace(material.Name))
                glMaterial.Name = material.Name;

            if (material.NormalMap != null)
            {
                glMaterial.NormalTexture = new MaterialNormalTextureInfo
                {
                    Index = ProcessTexture(material.NormalMap),
                    TexCoord = (int)material.NormalMap.DefaultUvSet,
                    Scale = material.NormalScale
                };
            }

            if (material.OcclusionMap != null)
            {
                glMaterial.OcclusionTexture = new MaterialOcclusionTextureInfo
                {
                    Index = ProcessTexture(material.OcclusionMap),
                    TexCoord = (int)material.OcclusionMap.DefaultUvSet,
                    Strength = material.OcclusionStrength
                };
            }

            if (material.EmissiveMap != null)
                glMaterial.EmissiveTexture = TextureInfo(material.EmissiveMap);

            if (material.EmissiveColor.R != 0 || material.EmissiveColor.G != 0 || material.EmissiveColor.B != 0)
                glMaterial.EmissiveFactor = [Clamp01(material.EmissiveColor.R), Clamp01(material.EmissiveColor.G), Clamp01(material.EmissiveColor.B)];

            switch (material.Alpha.ToString())
            {
                case "Mask":
                    glMaterial.AlphaMode = GltfMaterial.AlphaModeEnum.MASK;
                    glMaterial.AlphaCutoff = material.AlphaCutoff;
                    break;

                case "Blend":
                case "BlendMain":
                    glMaterial.AlphaMode = GltfMaterial.AlphaModeEnum.BLEND;
                    break;

                default:
                    glMaterial.AlphaMode = GltfMaterial.AlphaModeEnum.OPAQUE;
                    break;
            }

            var materialId = _materials.Count;
            _materialIds.Add(material, materialId);
            _materials.Add(glMaterial);

            return materialId;
        }

        GltfTextureInfo TextureInfo(Texture2D texture)
        {
            return new GltfTextureInfo
            {
                Index = ProcessTexture(texture),
                TexCoord = (int)texture.DefaultUvSet
            };
        }

        int ProcessTexture(Texture2D texture)
        {
            if (_textureIds.TryGetValue(texture, out var existing))
                return existing;

            if (texture.Width == 0 || texture.Height == 0)
                throw new InvalidOperationException($"Texture '{texture.Name}' has invalid dimensions.");

            var data = ReadTexture(texture, out var readFormat);

            var png = EncodePng(data, checked((int)texture.Width), checked((int)texture.Height), readFormat);

            var imageView = AddBufferView(png);

            var image = new GltfImage
            {
                BufferView = imageView,
                MimeType = GltfImage.MimeTypeEnum.image_png
            };

            if (!string.IsNullOrWhiteSpace(texture.Name))
                image.Name = texture.Name;

            var imageId = _images.Count;
            _images.Add(image);

            var glTexture = new GltfTexture
            {
                Source = imageId,
                Sampler = ProcessSampler(texture)
            };

            if (!string.IsNullOrWhiteSpace(texture.Name))
                glTexture.Name = texture.Name;

            var textureId = _textures.Count;
            _textureIds.Add(texture, textureId);
            _textures.Add(glTexture);

            return textureId;
        }

        int ProcessSampler(Texture2D texture)
        {
            var sampler = new GltfSampler
            {
                WrapS = (GltfSampler.WrapSEnum)texture.WrapS,
                WrapT = (GltfSampler.WrapTEnum)texture.WrapT,
                MagFilter = (GltfSampler.MagFilterEnum)texture.MagFilter,
                MinFilter = (GltfSampler.MinFilterEnum)texture.MinFilter
            };

            var samplerId = _samplers.Count;
            _samplers.Add(sampler);
            return samplerId;
        }

        byte[] ReadTexture(Texture2D texture, out TextureFormat readFormat)
        {
            if (texture.Data != null && texture.Data.Count > 0 && texture.Data[0].Content != null)
            {
                var data = texture.Data[0];

                if (data.Compression == TextureCompressionFormat.Uncompressed)
                {
                    readFormat = data.Format;
                    return data.Content!.AsArray();
                }

                if (data.Compression is TextureCompressionFormat.Bc1 or TextureCompressionFormat.Bc3 or TextureCompressionFormat.Bc7)
                {
                    var decoded = ImageUtils.DecodeBC(data);
                    readFormat = decoded.Format;
                    return decoded.Content!.AsArray();
                }

                if (data.Compression == TextureCompressionFormat.Astc)
                {
                    var decoded = AstcCompressor.Decode(data, texture.Type == TextureType.NormalMap);
                    readFormat = decoded.Format;
                    return decoded.Content!.AsArray();
                }
            }

            readFormat = texture.Format;

            EngineApp.Current.Renderer.ReadTexture(texture, texture.Format, 0, 0, _readBuffers);

            return _readBuffers[0].AsArray();
        }

        static VertexData[] ReadVertices(TriangleMesh mesh)
        {
            var vertices = mesh.Geometry!.Vertices;

            if (vertices != null && vertices.Length > 0)
                return vertices;

            if (mesh.VBuf == null)
                return [];

            vertices = [];
            mesh.VBuf.ReadArray(ref vertices);
            return vertices;
        }

        static uint[] ReadIndices(TriangleMesh mesh)
        {
            var indices = mesh.Geometry!.Indices;

            if (indices != null && indices.Length > 0)
                return indices;

            if (mesh.IBuf == null)
                return [];

            indices = [];
            mesh.IBuf.ReadArray(ref indices);
            return indices;
        }

        int AddVertexAccessor(VertexData[] source, Func<VertexData, Vector2> selector)
        {
            var values = new Vector2[source.Length];

            for (var i = 0; i < source.Length; i++)
                values[i] = selector(source[i]);

            return AddAccessor(values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC2, GltfBufferView.TargetEnum.ARRAY_BUFFER);
        }

        int AddVertexAccessor(VertexData[] source, Func<VertexData, Vector3> selector, bool bounds = false)
        {
            var values = new Vector3[source.Length];

            for (var i = 0; i < source.Length; i++)
                values[i] = selector(source[i]);

            if (!bounds)
                return AddAccessor(values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC3, GltfBufferView.TargetEnum.ARRAY_BUFFER);

            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);

            foreach (var value in values)
            {
                min = Vector3.Min(min, value);
                max = Vector3.Max(max, value);
            }

            return AddAccessor(values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC3, GltfBufferView.TargetEnum.ARRAY_BUFFER,
                [min.X, min.Y, min.Z], [max.X, max.Y, max.Z]);
        }

        int AddVertexAccessor(VertexData[] source, Func<VertexData, Vector4> selector)
        {
            var values = new Vector4[source.Length];

            for (var i = 0; i < source.Length; i++)
                values[i] = selector(source[i]);

            return AddAccessor(values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC4, GltfBufferView.TargetEnum.ARRAY_BUFFER);
        }

        int AddJointAccessor(SkinData[] skin)
        {
            var values = new ushort[skin.Length * 4];

            for (var i = 0; i < skin.Length; i++)
            {
                var src = skin[i].JointIndices;

                values[i * 4 + 0] = JointIndex(src.X);
                values[i * 4 + 1] = JointIndex(src.Y);
                values[i * 4 + 2] = JointIndex(src.Z);
                values[i * 4 + 3] = JointIndex(src.W);
            }

            return AddAccessor(values, GltfAccessor.ComponentTypeEnum.UNSIGNED_SHORT, GltfAccessor.TypeEnum.VEC4, GltfBufferView.TargetEnum.ARRAY_BUFFER, count: skin.Length);
        }

        int AddSkinWeightAccessor(SkinData[] skin)
        {
            var values = new Vector4[skin.Length];

            for (var i = 0; i < skin.Length; i++)
                values[i] = skin[i].JointWeights;

            return AddAccessor(values, GltfAccessor.ComponentTypeEnum.FLOAT, GltfAccessor.TypeEnum.VEC4, GltfBufferView.TargetEnum.ARRAY_BUFFER);
        }

        static ushort JointIndex(int value)
        {
            if ((uint)value > ushort.MaxValue)
                throw new NotSupportedException($"Joint index {value} cannot be represented as UNSIGNED_SHORT.");

            return (ushort)value;
        }

        int AddIndexAccessor(uint[] indices)
        {
            var max = 0u;

            foreach (var index in indices)
                max = Math.Max(max, index);

            if (max <= ushort.MaxValue)
            {
                var compact = new ushort[indices.Length];

                for (var i = 0; i < indices.Length; i++)
                    compact[i] = (ushort)indices[i];

                return AddAccessor(compact, GltfAccessor.ComponentTypeEnum.UNSIGNED_SHORT, GltfAccessor.TypeEnum.SCALAR, GltfBufferView.TargetEnum.ELEMENT_ARRAY_BUFFER);
            }

            return AddAccessor(indices, GltfAccessor.ComponentTypeEnum.UNSIGNED_INT, GltfAccessor.TypeEnum.SCALAR, GltfBufferView.TargetEnum.ELEMENT_ARRAY_BUFFER);
        }

        int AddAccessor<T>(T[] data, GltfAccessor.ComponentTypeEnum componentType, GltfAccessor.TypeEnum type,
            GltfBufferView.TargetEnum? target = null, float[]? min = null, float[]? max = null, int? count = null) where T : unmanaged
        {
            if (data.Length == 0)
                throw new ArgumentException("Accessor data is empty.", nameof(data));

            var accessor = new GltfAccessor
            {
                BufferView = AddBufferView(MemoryMarshal.AsBytes(data.AsSpan()), target),
                ComponentType = componentType,
                Count = count ?? data.Length,
                Type = type,
                Min = min,
                Max = max
            };

            var accessorId = _accessors.Count;
            _accessors.Add(accessor);
            return accessorId;
        }

        int AddBufferView(ReadOnlySpan<byte> data, GltfBufferView.TargetEnum? target = null)
        {
            if (data.Length == 0)
                throw new ArgumentException("Buffer view data is empty.", nameof(data));

            Align(_bin, 4, 0);

            var offset = checked((int)_bin.Position);
            _bin.Write(data);

            var view = new GltfBufferView
            {
                Buffer = 0,
                ByteOffset = offset,
                ByteLength = data.Length,
                Target = target
            };

            var viewId = _bufferViews.Count;
            _bufferViews.Add(view);
            return viewId;
        }

        glTFLoader.Schema.Gltf BuildModel()
        {
            var model = new glTFLoader.Schema.Gltf
            {
                Asset = new Asset
                {
                    Version = "2.0",
                    Generator = "XrEngine.GltfExporter"
                },
                Scene = 0,
                Scenes =
                [
                    new Scene
                    {
                        Nodes = _sceneRoots.ToArray()
                    }
                ],
                Nodes = _nodes.ToArray(),
                Buffers =
                [
                    new glTFLoader.Schema.Buffer
                    {
                        ByteLength = checked((int)_bin.Length)
                    }
                ]
            };

            if (_meshes.Count > 0)
                model.Meshes = _meshes.ToArray();

            if (_materials.Count > 0)
                model.Materials = _materials.ToArray();

            if (_textures.Count > 0)
                model.Textures = _textures.ToArray();

            if (_images.Count > 0)
                model.Images = _images.ToArray();

            if (_samplers.Count > 0)
                model.Samplers = _samplers.ToArray();

            if (_accessors.Count > 0)
                model.Accessors = _accessors.ToArray();

            if (_bufferViews.Count > 0)
                model.BufferViews = _bufferViews.ToArray();

            if (_skins.Count > 0)
                model.Skins = _skins.ToArray();

            return model;
        }

        static void WriteTransform(GltfNode node, Matrix4x4 matrix)
        {
            if (matrix.IsIdentity)
                return;

            if (Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation))
            {
                if (translation != Vector3.Zero)
                    node.Translation = [translation.X, translation.Y, translation.Z];

                if (rotation != Quaternion.Identity)
                    node.Rotation = [rotation.X, rotation.Y, rotation.Z, rotation.W];

                if (scale != Vector3.One)
                    node.Scale = [scale.X, scale.Y, scale.Z];

                return;
            }

            node.Matrix =
            [
                matrix.M11, matrix.M12, matrix.M13, matrix.M14,
                matrix.M21, matrix.M22, matrix.M23, matrix.M24,
                matrix.M31, matrix.M32, matrix.M33, matrix.M34,
                matrix.M41, matrix.M42, matrix.M43, matrix.M44
            ];
        }


        static unsafe byte[] EncodePng(byte[] data, int width, int height, TextureFormat format)
        {
            byte[]? converted = null;
            var colorType = PngLib.ColorTypeRgba;
            var bitDepth = 8;
            var swap16 = false;

            switch (format)
            {
                case TextureFormat.Rgba8:
                case TextureFormat.SRgba8:
                    break;

                case TextureFormat.Rgb8:
                case TextureFormat.SRgb8:
                    colorType = PngLib.ColorTypeRgb;
                    break;

                case TextureFormat.Gray8:
                    colorType = PngLib.ColorTypeGray;
                    break;

                case TextureFormat.Rgba16:
                case TextureFormat.SRgbaInt16:
                    bitDepth = 16;
                    swap16 = true;
                    break;

                case TextureFormat.Gray16:
                    colorType = PngLib.ColorTypeGray;
                    bitDepth = 16;
                    swap16 = true;
                    break;

                case TextureFormat.Bgra8:
                case TextureFormat.SBgra8:
                    converted = new byte[checked(width * height * 4)];
                    fixed (byte* pSrc = data)
                    fixed (byte* pDst = converted)
                        EngineNativeLib.ConvertRgbToBgr((uint)width, (uint)height, pSrc, pDst, 4);
                    data = converted;
                    break;

                case TextureFormat.Rg8:
                    converted = new byte[checked(width * height * 4)];
                    fixed (byte* pSrc = data)
                    fixed (byte* pDst = converted)
                    {
                        if (!EngineNativeLib.ImagePackToRgba8(pSrc, pDst, (uint)width, (uint)height, 2, 1))
                            throw new InvalidOperationException("Unable to convert Rg8 texture to RGBA8.");
                    }
                    data = converted;
                    break;

                default:
                    throw new NotSupportedException($"Texture format '{format}' cannot be exported as PNG without numeric conversion.");
            }

            var output = new PngLib.MemoryBuffer();

            try
            {
                fixed (byte* pData = data)
                    PngLib.EncodePng(pData, width, height, colorType, bitDepth, 6, swap16, ref output);

                if (output.Data == null || output.Size <= 0)
                    throw new InvalidOperationException($"PNG encoding failed for texture format '{format}'.");

                return output.Span.ToArray();
            }
            finally
            {
                output.Dispose();
            }
        }

        static void WritePngChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
        {
            Span<byte> size = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(size, (uint)data.Length);
            stream.Write(size);
            stream.Write(type);
            stream.Write(data);

            var crc = 0xFFFFFFFFu;
            crc = UpdateCrc(crc, type);
            crc = UpdateCrc(crc, data) ^ 0xFFFFFFFFu;

            Span<byte> crcData = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crcData, crc);
            stream.Write(crcData);
        }

        static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var value in data)
            {
                crc ^= value;

                for (var i = 0; i < 8; i++)
                    crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0u);
            }

            return crc;
        }

        static float Clamp01(float value)
        {
            return Math.Clamp(value, 0f, 1f);
        }

        static void Align(Stream stream, int alignment, byte value)
        {
            while ((stream.Position & (alignment - 1)) != 0)
                stream.WriteByte(value);
        }
    }
}
