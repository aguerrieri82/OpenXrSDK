
using Common.Interop;
using XrMath;
using AvatarApi = global::Oculus.Avatar2.CAPI;

namespace XrEngine.OpenXr.Oculus
{
    public partial class OculusAvatarManager
    {
        private sealed class ImageData
        {
            public AvatarApi.ovrAvatar2Image Native;
            public byte[] Bytes = [];
            public Texture2D? Linear;
            public Texture2D? Srgb;
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

        private PbrMaterial CreateMaterial(PrimitiveData primitive)
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

            // BC5 stores only X and Y. The PBR shader expects XYZ encoded in RGB.
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var x = pixels[i] / 255f * 2 - 1;
                var y = pixels[i + 1] / 255f * 2 - 1;
                var z = MathF.Sqrt(MathF.Max(0, 1 - x * x - y * y));
                pixels[i + 2] = (byte)MathF.Round((z * 0.5f + 0.5f) * 255);
            }

            return pixels;
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
    }
}
