using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Common.Interop;
using XrEngine.Components;
using XrMath;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    internal class OculusAvatarResources
    {
        internal sealed class PrimitiveData
        {
            public AvatarApi.ovrAvatar2Primitive Native;
            public string Name = "";
            public Geometry3D Geometry = null!;
            public AvatarApi.ovrAvatar2JointInfo[] Joints = [];
            public AvatarApi.ovrAvatar2MaterialTexture[] Textures = [];
            public AvatarMeshData Details = null!;
        }

        private sealed class ImageData
        {
            public AvatarApi.ovrAvatar2Image Native;
            public byte[] Bytes = [];
            public Texture2D? Linear;
            public Texture2D? Srgb;
        }

        private readonly Dictionary<AvatarApi.ovrAvatar2Id, PrimitiveData> _primitives = new();
        private readonly Dictionary<AvatarApi.ovrAvatar2Id, ImageData> _images = new();

        private delegate AvatarApi.ovrAvatar2Result ReadVertexBuffer(
            AvatarApi.ovrAvatar2VertexBufferId id, IntPtr buffer, uint bytes, uint stride);

        private unsafe delegate AvatarApi.ovrAvatar2Result ReadMorphAttribute(
            AvatarApi.ovrAvatar2MorphTargetBufferId id,
            uint targetIndex,
            AvatarApi.ovrAvatar2Vector3f* buffer,
            uint bytes,
            uint stride);

        public void Read(AvatarApi.ovrAvatar2Asset_Resource resource)
        {
            if (resource.status == AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_LoadFailed)
                throw new InvalidOperationException($"Avatar resource {resource.assetID} failed to load.");

            if (resource.status != AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Loaded &&
                resource.status != AvatarApi.ovrAvatar2AssetStatus.ovrAvatar2AssetStatus_Updated)
                return;

            try
            {
                ReadImages(resource.assetID);
                Check(AvatarApi.ovrAvatar2Asset_GetPrimitiveCount(resource.assetID, out var count));

                for (uint i = 0; i < count; i++)
                {
                    Check(AvatarApi.ovrAvatar2Asset_GetPrimitiveByIndex(resource.assetID, i, out var primitive));
                    _primitives[primitive.id] = ReadPrimitive(primitive);
                }

                Check(AvatarApi.ovrAvatar2Asset_ResourceReadyToRender(resource.assetID));
            }
            finally
            {
                AvatarApi.ovrAvatar2Asset_ReleaseResource(resource.assetID);
            }
        }

        public PrimitiveData GetPrimitive(AvatarApi.ovrAvatar2Id id)
        {
            return _primitives[id];
        }

        public void Clear()
        {
            _primitives.Clear();
            _images.Clear();
        }

        private unsafe void ReadImages(AvatarApi.ovrAvatar2Id resource)
        {
            Check(AvatarApi.ovrAvatar2Asset_GetImageCount(resource, out var count));

            for (uint i = 0; i < count; i++)
            {
                Check(AvatarApi.ovrAvatar2Asset_GetImageByIndex(resource, i, out var image));

                var bytes = new byte[image.imageDataSize];

                fixed (byte* pointer = bytes)
                {
                    var result = AvatarApi.ovrAvatar2Asset_GetImageDataByIndex(resource, i, pointer, image.imageDataSize);
                    Check(result);
                }

                _images[image.id] = new ImageData { Native = image, Bytes = bytes };
            }
        }

        public PbrMaterial CreateMaterial(PrimitiveData primitive)
        {
            var material = new PbrMaterial
            {
                Name = primitive.Name,
                Color = Color.White,
                Metalness = 0,
                Roughness = 1,
                Alpha = primitive.Native.alphaMode switch
                {
                    AvatarApi.ovrAvatar2AlphaMode.Mask => AlphaMode.Mask,
                    AvatarApi.ovrAvatar2AlphaMode.Blend => AlphaMode.Blend,
                    _ => AlphaMode.Opaque
                },
                AlphaCutoff = 0.5f
            };

            foreach (var entry in primitive.Textures)
            {
                var srgb = entry.type == AvatarApi.ovrAvatar2MaterialTextureType.BaseColor ||
                    entry.type == AvatarApi.ovrAvatar2MaterialTextureType.Emissive;

                var texture = GetTexture(entry.imageId, srgb);
                var factor = entry.factor;

                switch (entry.type)
                {
                    case AvatarApi.ovrAvatar2MaterialTextureType.BaseColor:
                        material.ColorMap = texture;
                        material.Color = new Color(factor.x, factor.y, factor.z, factor.w);
                        break;
                    case AvatarApi.ovrAvatar2MaterialTextureType.Normal:
                        material.NormalMap = texture;
                        material.NormalScale = factor.x;
                        break;
                    case AvatarApi.ovrAvatar2MaterialTextureType.MetallicRoughness:
                        material.MetallicRoughnessMap = texture;
                        material.Metalness = factor.x;
                        material.Roughness = factor.y;
                        break;
                    case AvatarApi.ovrAvatar2MaterialTextureType.Occulusion:
                        material.OcclusionMap = texture;
                        material.OcclusionStrength = factor.x;
                        break;
                    case AvatarApi.ovrAvatar2MaterialTextureType.Emissive:
                        material.EmissiveMap = texture;
                        material.EmissiveColor = new Color(factor.x, factor.y, factor.z, factor.w);
                        break;
                    case AvatarApi.ovrAvatar2MaterialTextureType.UsedInExtension:
                        if (texture != null)
                            primitive.Details.ExtensionTextures[entry.imageId] = texture;
                        break;
                }
            }

            foreach (var property in primitive.Details.MaterialExtensions)
            {
                if (property.Type != AvatarApi.ovrAvatar2MaterialExtensionEntryType.ImageId)
                    continue;

                var id = (AvatarApi.ovrAvatar2Id)BitConverter.ToInt32(property.Data);
                var texture = GetTexture(id, false);

                if (texture != null)
                    primitive.Details.ExtensionTextures[id] = texture;
            }

            return material;
        }

        private Texture2D? GetTexture(AvatarApi.ovrAvatar2Id id, bool srgb)
        {
            if (!_images.TryGetValue(id, out var image))
                return null;

            if (srgb)
                return image.Srgb ??= CreateTexture(image, true);

            return image.Linear ??= CreateTexture(image, false);
        }

        private static Texture2D CreateTexture(ImageData image, bool srgb)
        {
            var format = image.Native.format;

            var compression = format switch
            {
                AvatarApi.ovrAvatar2ImageFormat.RGBA32 => TextureCompressionFormat.Uncompressed,
                AvatarApi.ovrAvatar2ImageFormat.BC5U => TextureCompressionFormat.Uncompressed,
                AvatarApi.ovrAvatar2ImageFormat.BC5S => TextureCompressionFormat.Uncompressed,
                AvatarApi.ovrAvatar2ImageFormat.DXT1 => TextureCompressionFormat.Bc1,
                AvatarApi.ovrAvatar2ImageFormat.DXT5 => TextureCompressionFormat.Bc3,
                AvatarApi.ovrAvatar2ImageFormat.BC7U => TextureCompressionFormat.Bc7,
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_4x4 or
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_6x6 or
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_8x8 or
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_12x12 => TextureCompressionFormat.Astc,
                _ => throw new NotSupportedException($"Avatar texture format: {format}.")
            };

            uint blockSize = format switch
            {
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_6x6 => 6,
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_8x8 => 8,
                AvatarApi.ovrAvatar2ImageFormat.ASTC_RGBA_12x12 => 12,
                _ => 4
            };

            var textureFormat = srgb ? TextureFormat.SRgba8 : TextureFormat.Rgba8;
            var levels = new List<TextureData>();

            uint width = image.Native.sizeX;
            uint height = image.Native.sizeY;
            int offset = 0;

            for (uint mip = 0; mip < Math.Max(image.Native.mipCount, 1); mip++)
            {
                uint mipByteCount;

                if (format == AvatarApi.ovrAvatar2ImageFormat.RGBA32)
                    mipByteCount = width * height * 4;
                else
                {
                    var blocksX = (width + blockSize - 1) / blockSize;
                    var blocksY = (height + blockSize - 1) / blockSize;

                    uint bytesPerBlock = 16;

                    if (format == AvatarApi.ovrAvatar2ImageFormat.DXT1)
                        bytesPerBlock = 8;

                    mipByteCount = blocksX * blocksY * bytesPerBlock;
                }

                var byteCount = checked((int)mipByteCount);
                var bytes = image.Bytes.AsSpan(offset, byteCount).ToArray();

                offset += byteCount;

                if (format == AvatarApi.ovrAvatar2ImageFormat.BC5U || format == AvatarApi.ovrAvatar2ImageFormat.BC5S)
                    bytes = DecodeNormalMap(bytes, (int)width, (int)height, format == AvatarApi.ovrAvatar2ImageFormat.BC5S);

                levels.Add(new TextureData
                {
                    Width = width,
                    Height = height,
                    Depth = 1,
                    MipLevel = mip,
                    Format = textureFormat,
                    Compression = compression,
                    BlockSize = blockSize,
                    Content = MemoryBuffer.Create(bytes)
                });

                width = Math.Max(width / 2, 1);
                height = Math.Max(height / 2, 1);
            }

            return new Texture2D(levels)
            {
                Name = $"Avatar texture {image.Native.id}",
                MinFilter = levels.Count > 1 ? ScaleFilter.LinearMipmapLinear : ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                WrapS = WrapMode.Repeat,
                WrapT = WrapMode.Repeat
            };
        }

        private static unsafe byte[] DecodeNormalMap(byte[] source, int width, int height, bool signed)
        {
            var pixels = new byte[width * height * 4];
            var format = signed ? EngineNativeLib.BcFormat.Bc5Signed : EngineNativeLib.BcFormat.Bc5;

            fixed (byte* input = source)
            fixed (byte* output = pixels)
            {
                if (!EngineNativeLib.ImageDecodeBC(input, width, height, format, output))
                    throw new InvalidOperationException("Cannot decode avatar BC5 normal map.");
            }

            for (int i = 0; i < pixels.Length; i += 4)
            {
                var x = pixels[i] / 255f * 2 - 1;
                var y = pixels[i + 1] / 255f * 2 - 1;
                var z = MathF.Sqrt(MathF.Max(0, 1 - x * x - y * y));
                pixels[i + 2] = (byte)MathF.Round((z * 0.5f + 0.5f) * 255);
            }

            return pixels;
        }

        private static unsafe PrimitiveData ReadPrimitive(AvatarApi.ovrAvatar2Primitive primitive)
        {
            var id = primitive.vertexBufferId;
            Check(AvatarApi.ovrAvatar2VertexBuffer_GetVertexCount(id, out var count));

            var positions = ReadBuffer<Vector3>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetPositions);
            var normals = ReadBuffer<Vector3>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetNormals, true);
            var tangents = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTangents, true);
            var uv = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord0, true);
            var uv1 = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord1, true);
            var vertices = new VertexData[count];

            for (int i = 0; i < count; i++)
            {
                vertices[i].Pos = positions[i];

                if (normals.Length > 0)
                    vertices[i].Normal = normals[i];

                if (tangents.Length > 0)
                    vertices[i].Tangent = tangents[i];

                if (uv.Length > 0)
                    vertices[i].UV = uv[i];

                if (uv1.Length > 0)
                    vertices[i].UV1 = uv1[i];
            }

            var indices = new ushort[primitive.indexCount];

            fixed (ushort* pointer = indices)
            {
                var byteCount = (uint)indices.Length * sizeof(ushort);
                var result = AvatarApi.ovrAvatar2Primitive_GetIndexData(primitive.id, pointer, byteCount);
                Check(result);
            }

            var geometry = new Geometry3D
            {
                Vertices = vertices,
                Indices = Array.ConvertAll(indices, value => (uint)value),
                ActiveComponents = VertexComponent.Position
            };

            if (normals.Length > 0)
                geometry.ActiveComponents |= VertexComponent.Normal;

            if (tangents.Length > 0)
                geometry.ActiveComponents |= VertexComponent.Tangent;

            if (uv.Length > 0)
                geometry.ActiveComponents |= VertexComponent.UV0;

            if (uv1.Length > 0)
                geometry.ActiveComponents |= VertexComponent.UV1;

            var details = new AvatarMeshData
            {
                VertexColors = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetColors, true),
                OrmtColors = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetColorsORMT, true),
                Uv2 = ReadBuffer<Vector2>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetTexCoord2, true),
                MaterialExtensions = ReadMaterialExtensions(primitive.id)
            };

            var data = new PrimitiveData
            {
                Native = primitive,
                Name = ReadName(primitive.id),
                Geometry = geometry,
                Joints = new AvatarApi.ovrAvatar2JointInfo[primitive.jointCount],
                Textures = new AvatarApi.ovrAvatar2MaterialTexture[primitive.textureCount],
                Details = details
            };

            for (uint i = 0; i < data.Textures.Length; i++)
                Check(AvatarApi.ovrAvatar2Primitive_GetMaterialTextureByIndex(primitive.id, i, out data.Textures[i]));

            if (primitive.jointCount > 0)
            {
                var jointIndices = ReadBuffer<AvatarApi.ovrAvatar2Vector4us>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetJointIndices);
                var weights = ReadBuffer<Vector4>(id, count, AvatarApi.ovrAvatar2VertexBuffer_GetJointWeights);
                var skin = new SkinData[count];

                for (int i = 0; i < count; i++)
                {
                    var joint = jointIndices[i];

                    skin[i].JointIndices = new Vector4I(joint.x, joint.y, joint.z, joint.w);
                    skin[i].JointWeights = weights[i];
                }

                var skinned = geometry.AddComponent(new SkinnedGeometry());

                skinned.Skin = skin;
                geometry.ActiveComponents |= VertexComponent.Skin;

                fixed (AvatarApi.ovrAvatar2JointInfo* pointer = data.Joints)
                {
                    var byteCount = primitive.jointCount * (uint)sizeof(AvatarApi.ovrAvatar2JointInfo);
                    var result = AvatarApi.ovrAvatar2Primitive_GetJointInfo(primitive.id, pointer, byteCount);
                    Check(result);
                }
            }

            ReadMorphTargets(data, count);
            geometry.UpdateBounds();

            return data;
        }

        private static unsafe T[] ReadBuffer<T>(
            AvatarApi.ovrAvatar2VertexBufferId id, uint count, ReadVertexBuffer read, bool optional = false)
            where T : unmanaged
        {
            var data = new T[count];
            var stride = (uint)sizeof(T);
            var byteCount = checked(count * stride);

            fixed (T* pointer = data)
            {
                var result = read(id, (IntPtr)pointer, byteCount, stride);

                if (optional && result == AvatarApi.ovrAvatar2Result.DataNotAvailable)
                    return [];

                Check(result);
            }

            return data;
        }

        private static unsafe Vector3[] ReadMorphValues(
            AvatarApi.ovrAvatar2MorphTargetBufferId id,
            uint targetIndex,
            uint vertexCount,
            ReadMorphAttribute read,
            bool optional = false)
        {
            var values = new Vector3[vertexCount];
            var stride = (uint)sizeof(Vector3);
            var byteCount = vertexCount * stride;
            AvatarApi.ovrAvatar2Result result;

            fixed (Vector3* pointer = values)
            {
                var buffer = (AvatarApi.ovrAvatar2Vector3f*)pointer;
                result = read(id, targetIndex, buffer, byteCount, stride);
            }

            if (optional && result == AvatarApi.ovrAvatar2Result.DataNotAvailable)
                return [];

            Check(result);

            return values;
        }

        private static unsafe void ReadMorphTargets(PrimitiveData primitive, uint vertexCount)
        {
            var id = primitive.Native.morphTargetBufferId;

            if (id == AvatarApi.ovrAvatar2MorphTargetBufferId.Invalid)
                return;

            var result = AvatarApi.ovrAvatar2VertexBuffer_GetMorphTargetCount(id, out var count);

            if (result == AvatarApi.ovrAvatar2Result.DataNotAvailable || result == AvatarApi.ovrAvatar2Result.NotFound)
                return;

            Check(result);

            if (count == 0)
                return;

            var targets = new MorphTarget[count];

            primitive.Details.MorphTargetNames = new string[count];

            for (uint i = 0; i < count; i++)
            {
                var positions = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexPositions);

                var normals = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexNormals, optional: true);

                var tangents = ReadMorphValues(id, i, vertexCount,
                    AvatarApi.ovrAvatar2MorphTarget_GetVertexTangents, optional: true);

                var name = ReadMorphName(id, i);

                var components = new List<MorphComponent>
                {
                    new()
                    {
                        Component = VertexComponent.MorphPosition,
                        Values = positions,
                    }
                };

                if (normals.Length > 0)
                {
                    components.Add(new MorphComponent
                    {
                        Component = VertexComponent.MorphNormal,
                        Values = normals,
                    });
                }

                if (tangents.Length > 0)
                {
                    components.Add(new MorphComponent
                    {
                        Component = VertexComponent.MorphTangent,
                        Values = tangents,
                    });
                }

                primitive.Details.MorphTargetNames[i] = name;

                targets[i].Components = components.ToArray();
                targets[i].Name = name;
            }

            primitive.Geometry.AddComponent(new MorphedGeometry { Targets = targets });
        }

        private static unsafe string ReadMorphName(AvatarApi.ovrAvatar2MorphTargetBufferId id, uint index)
        {
            var name = new byte[1024];

            fixed (byte* pointer = name)
            {
                var result = AvatarApi.ovrAvatar2Asset_GetMorphTargetName(id, index, pointer, (uint)name.Length);
                Check(result);
            }

            return DecodeName(name);
        }

        private static unsafe string ReadName(AvatarApi.ovrAvatar2Id id)
        {
            var bytes = new byte[1024];

            fixed (byte* pointer = bytes)
            {
                var result = AvatarApi.ovrAvatar2Asset_GetPrimitiveName(id, pointer, (uint)bytes.Length);
                Check(result);
            }

            return DecodeName(bytes);
        }

        private static unsafe List<OculusAvatarMaterialProperty> ReadMaterialExtensions(AvatarApi.ovrAvatar2Id id)
        {
            var properties = new List<OculusAvatarMaterialProperty>();

            Check(AvatarApi.ovrAvatar2Primitive_GetNumMaterialExtensions(id, out var count));

            for (uint i = 0; i < count; i++)
            {
                uint length = 1024;
                var name = new byte[length];

                fixed (byte* pointer = name)
                {
                    var result = AvatarApi.ovrAvatar2Primitive_GetMaterialExtensionName(id, i, pointer, &length);
                    Check(result);
                }

                var extension = DecodeName(name);

                Check(AvatarApi.ovrAvatar2Primitive_GetNumEntriesInMaterialExtensionByIndex(id, i, out var entries));

                for (uint j = 0; j < entries; j++)
                {
                    Check(AvatarApi.ovrAvatar2Primitive_MaterialExtensionEntryMetaDataByIndex(id, i, j, out var entry));

                    var entryName = new byte[entry.nameBufferSize];
                    var data = new byte[entry.dataBufferSize];

                    fixed (byte* namePointer = entryName)
                    fixed (byte* dataPointer = data)
                    {
                        var result = AvatarApi.ovrAvatar2Primitive_MaterialExtensionEntryDataByIndex(
                            id, i, j, namePointer, entry.nameBufferSize, dataPointer, entry.dataBufferSize);

                        Check(result);
                    }

                    properties.Add(new OculusAvatarMaterialProperty
                    {
                        Extension = extension,
                        Name = DecodeName(entryName),
                        Type = entry.entryType,
                        Data = data
                    });
                }
            }

            return properties;
        }

        internal static void Check(AvatarApi.ovrAvatar2Result result)
        {
            if (result != AvatarApi.ovrAvatar2Result.Success)
                throw new InvalidOperationException($"Avatar SDK: {result}.");
        }

        internal static string DecodeName(byte[] bytes)
        {
            var length = Array.IndexOf(bytes, (byte)0);
            return Encoding.UTF8.GetString(bytes, 0, length < 0 ? bytes.Length : length);
        }
    }
}