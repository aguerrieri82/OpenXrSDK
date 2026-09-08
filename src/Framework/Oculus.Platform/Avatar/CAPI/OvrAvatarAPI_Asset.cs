/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
// Native declarations ported from Meta Avatars SDK 40.0.1. Unity helpers omitted.
#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        public const string assetLogScope = "OvrAvatarAPI_Asset";
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar2Asset_GetNumberOutstandingAssets();
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar2Asset_GetEntityPendingCount(ovrAvatar2EntityId entityId);
        public enum ovrAvatar2LoadRequestState : Int32
        {
            // Invalid state
            None = 0,
            // User Avatar specification requested
            PendingSpecification,
            // CDN asset load in progress (network or from cache)
            CdnLoad,
            // Loading assets from URI
            LoadFromUri,
            // Loading assets from memory
            LoadFromMemory,
            // Parsing asset files after load
            ParseAsset,
            // Awaiting "ready to render" from client
            ClientLoad,
            // Load successful; assets have been applied to the entity. hierarchyVersion/allNodesVersion likely changed
            Success,
            // Failed; see LoadFailedReason for details
            Failed,
            // Cancelled before completion (e.g. entity destroyed)
            Cancelled
        }

        public enum ovrAvatar2LoadRequestFailure : Int32
        {
            None = 0,
            CdnLoadInProgress,
            NoAssetProfile,
            SpecRequestFailed,
            SpecParseFailed,
            SpecRequestCancelled,
            MissingAvatar,
            SpecHadInvalidAnimSet,
            SpecHadInvalidModel,
            InvalidUri,
            AssetNotFound,
            MismatchedLoadFilters,
            HttpLoadFailed,
            CacheLoadFailed,
            DiskLoadFailed,
            ParseFailed,
            Unknown,
        }

        public enum ovrAvatar2LoadRequestType : Int32
        {
            User,
            Memory,
            Uri,
        }

        public struct ovrAvatar2LoadRequestInfo
        {
            public ovrAvatar2LoadRequestId id;
            public ovrAvatar2EntityId entityId;
            public ovrAvatar2LoadRequestState state;
            public ovrAvatar2LoadRequestFailure failedReason;
            public ovrAvatar2LoadRequestType type;
            public Int64 responseCode;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_GetLoadRequestInfo(ovrAvatar2LoadRequestId loadRequestId, out ovrAvatar2LoadRequestInfo loadRequestInfo);
        public enum ovrAvatar2AssetStatus : Int32
        {
            ovrAvatar2AssetStatus_LoadFailed = 0,
            ovrAvatar2AssetStatus_Loaded,
            ovrAvatar2AssetStatus_Unloaded,
            ovrAvatar2AssetStatus_Updated,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Asset_Resource
        {
            public ovrAvatar2AssetStatus status;
            public ovrAvatar2Id assetID;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_ReleaseResource(ovrAvatar2Id resourceId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_ResourceReadyToRender(ovrAvatar2Id resourceID);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Asset_GetName(ovrAvatar2Id id, byte* nameBuffer, UInt32 bufferByteSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_GetPrimitiveCount(ovrAvatar2Id resourceId, out UInt32 count);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_GetPrimitiveByIndex(ovrAvatar2Id resourceId, UInt32 primitiveIndex, out ovrAvatar2Primitive primitive);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_GetImageCount(ovrAvatar2Id resourceId, out UInt32 count);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Asset_GetImageByIndex(ovrAvatar2Id resourceId, UInt32 imageIndex, out ovrAvatar2Image image);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern ovrAvatar2Result ovrAvatar2Asset_GetImageDataByIndex(ovrAvatar2Id resourceId, UInt32 imageIndex, byte* buffer, UInt32 bufferSize);
        public enum ovrAvatar2ImageFormat : UInt32
        {
            //<summary>Invalid image format</summary>
            Invalid = 0,
            RGBA32 = 0xe3dd9a1e, //< RGBA 32bit uncompressed texture
            DXT1 = 0xb9ee766e, //< DXT1/BC1 compressed texture
            DXT5 = 0x9a853814, //< DXT5/BC3 compressed texture
            BC5U = 0xcee1cf1a, //< BC5 compressed texture (unsigned)
            BC5S = 0x57c603f3, //< BC5 compressed texture (signed)
            BC7U = 0xaa33790d, //< BC7 compressed texture (unsigned)
            ASTC_RGBA_4x4 = 0xdc2b8f4c, //< ASTC 4x4 compressed texture
            ASTC_RGBA_6x6 = 0xbd4fed74, //< ASTC 6x6 compressed texture
            ASTC_RGBA_8x8 = 0xa75606e7, //< ASTC 8x8 compressed texture
            ASTC_RGBA_12x12 = 0x7dfcb3d0, //< ASTC 12x12 compressed texture
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Image
        {
            public ovrAvatar2Id id;
            public ovrAvatar2ImageFormat format;
            public UInt32 sizeX;
            public UInt32 sizeY;
            public UInt32 mipCount;
            public UInt32 imageDataSize;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Asset_GetImageName(ovrAvatar2Id primitiveId, byte* nameBuffer, UInt32 bufferByteSize);
        public enum ovrAvatar2AlphaMode : Int32
        {
            Opaque = 0,
            Mask = 1,
            Blend = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Primitive
        {
            public ovrAvatar2Id id;
            public ovrAvatar2VertexBufferId vertexBufferId;
            public ovrAvatar2MorphTargetBufferId morphTargetBufferId;
            public ovrAvatar2CompactSkinningDataId compactSkinningDataId;
            public UInt32 indexCount;
            public UInt16 minIndexValue;
            public UInt16 maxIndexValue;
            public ovrAvatar2AlphaMode alphaMode;
            public UInt32 textureCount;
            public UInt32 jointCount;
            public UInt32 skeleton;
            public UInt32 skinningCost;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Asset_GetPrimitiveName(ovrAvatar2Id primitiveId, byte* nameBuffer, UInt32 bufferByteSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Primitive_GetMinMaxPosition(ovrAvatar2Id primitiveId, out ovrAvatar2Vector3f minPosition, out ovrAvatar2Vector3f maxPosition);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Primitive_GetSkinnedMinMaxPosition(ovrAvatar2Id primitiveId, out ovrAvatar2Vector3f minPosition, out ovrAvatar2Vector3f maxPosition);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Asset_GetLodFlags(ovrAvatar2Id primitiveId, out ovrAvatar2EntityLODFlags lodFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Asset_GetManifestationFlags(ovrAvatar2Id primitiveId, out ovrAvatar2EntityManifestationFlags manifestationFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Asset_GetViewFlags(ovrAvatar2Id primitiveId, out ovrAvatar2EntityViewFlags viewFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Asset_GetSubMeshInclusionFlags(ovrAvatar2Id primitiveId, out ovrAvatar2EntitySubMeshInclusionFlags subMeshInclusionFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Asset_GetQuality(ovrAvatar2Id primitiveId, out ovrAvatar2EntityQuality quality);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern CAPI.ovrAvatar2Result ovrAvatar2Primitive_GetIndexData(ovrAvatar2Id primitiveId, UInt16* indexBuffer, UInt32 bytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetVertexCount(ovrAvatar2VertexBufferId id, out UInt32 vertexCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetMinMaxPosition(ovrAvatar2VertexBufferId id, out ovrAvatar2Vector3f minPosition, out ovrAvatar2Vector3f maxPosition);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetPositions(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector3f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetNormals(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector3f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetTangents(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector4f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetColors(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector4f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetColorsORMT(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector4f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetTexCoord0(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector2f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetTexCoord1(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector2f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetTexCoord2(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector2f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetJointIndices(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector4us[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetJointWeights(ovrAvatar2VertexBufferId vertexBufferId, /*ovrAvatar2Vector4f[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetSubMeshIds(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*Uint32[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetSubMeshIdsFloat(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*float[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetSubMeshTypes(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*Uint32[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetSubMeshTypesFloat(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*float[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetSubMeshTypesLogFloat(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*float[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetMaterialTypes(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*Uint32[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2VertexBuffer_GetMaterialTypesFloat(ovrAvatar2Id primitiveId, ovrAvatar2VertexBufferId vertexBufferId, /*float[]*/ IntPtr buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2MorphTarget_GetByName(ovrAvatar2MorphTargetBufferId primitiveId, string name, out UInt32 morphTargetIndex);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2VertexBuffer_GetMorphTargetCount(ovrAvatar2MorphTargetBufferId id, out UInt32 morphTargetCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2MorphTarget_GetVertexPositions(ovrAvatar2MorphTargetBufferId primitiveId, UInt32 morphTargetIndex, ovrAvatar2Vector3f* buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2MorphTarget_GetVertexNormals(ovrAvatar2MorphTargetBufferId primitiveId, UInt32 morphTargetIndex, ovrAvatar2Vector3f* buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2MorphTarget_GetVertexTangents(ovrAvatar2MorphTargetBufferId primitiveId, UInt32 morphTargetIndex, ovrAvatar2Vector3f* buffer, UInt32 bytes, UInt32 stride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Asset_GetMorphTargetName(ovrAvatar2MorphTargetBufferId primitiveId, UInt32 morphTargetIndex, byte* nameBuffer, UInt32 bufferByteSize);
        public enum ovrAvatar2MaterialTextureType : Int32
        {
            BaseColor = 0, // sRGB color space, linear alpha
            Normal = 1, // Linear color space
            Occulusion = 2, // Linear color space
            MetallicRoughness = 3, // Linear color space
            Emissive = 4, // sRGB color space, linear alpha
            UsedInExtension = 5, // Handled by material extensions
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2MaterialTexture
        {
            public ovrAvatar2MaterialTextureType type;
            public ovrAvatar2Vector4f factor;
            public ovrAvatar2Id imageId;
        }

        public enum ovrAvatar2MaterialExtensionEntryType : Int32
        {
            ImageId = 0,
            Float = 1,
            Int = 2,
            Vector3f = 3,
            Vector4f = 4,
            Invalid = Int32.MaxValue,
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2MaterialExtensionEntry
        {
            public ovrAvatar2MaterialExtensionEntryType entryType;
            public UInt32 nameBufferSize;
            public UInt32 dataBufferSize;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Primitive_GetMaterialTextureByIndex(ovrAvatar2Id primitiveId, UInt32 materialTextureIndex, out ovrAvatar2MaterialTexture materialTexture);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Asset_GetPrimitiveMaterialName(ovrAvatar2Id primitiveId, byte* nameBuffer, UInt32 bufferByteSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Asset_DeduceMaterialSubMeshFromName(ovrAvatar2EntitySubMeshInclusionFlags* destType, byte* matName);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Primitive_GetNumMaterialExtensions(ovrAvatar2Id id, out UInt32 numExtensions);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern CAPI.ovrAvatar2Result ovrAvatar2Primitive_Prototype_GetMaterialBuffers(ovrAvatar2Id id, float* types, // must be of size subMeshCount * slotsPerSubMesh
 float* offsets, // must be of size subMeshCount * slotsPerSubMesh
 float* parameterBuffer, UInt32 slotsPerSubMesh, out UInt32 subMeshCount, UInt32 maxSubMeshCount, out UInt32 parameterBufferSize, UInt32 maxParameterBufferSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Primitive_GetMaterialExtensionName(ovrAvatar2Id id, UInt32 extensionIndex, byte* nameBuffer, UInt32* nameBufferSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Primitive_GetNumEntriesInMaterialExtensionByIndex(ovrAvatar2Id id, UInt32 extensionIndex, out UInt32 numEntries);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Primitive_MaterialExtensionEntryMetaDataByIndex(ovrAvatar2Id id, UInt32 materialExtensionIndex, UInt32 entryIndex, out ovrAvatar2MaterialExtensionEntry entry);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Primitive_MaterialExtensionEntryDataByIndex(ovrAvatar2Id id, UInt32 materialExtensionIndex, UInt32 entryIndex, byte* nameBuffer, UInt32 nameBufferSize, byte* dataBuffer, UInt32 dataBufferSize);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2JointInfo
        {
            public Int32 jointIndex;
            public ovrAvatar2Matrix4f inverseBind;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Primitive_GetJointInfo(ovrAvatar2Id primitiveId, ovrAvatar2JointInfo* buffer, UInt32 bytes);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2PrimitiveSubmesh
        {
            public UInt32 indexStart;
            public UInt32 indexCount;
            public UInt16 vertexStart;
            public UInt16 vertexCount;
            public ovrAvatar2Vector2f minUVValues;
            public ovrAvatar2Vector2f maxUVValues;
            public ovrAvatar2Vector3f minValues;
            public ovrAvatar2Vector3f maxValues;
            public ovrAvatar2EntitySubMeshInclusionFlags inclusionFlags;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Primitive_GetSubMeshCount(ovrAvatar2Id primitiveId, out UInt32 subMeshCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Primitive_GetSubMeshByIndex(ovrAvatar2Id primitiveId, UInt32 subMeshIndex, out ovrAvatar2PrimitiveSubmesh subMesh);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Primitive_GetSubMeshMaterialName(ovrAvatar2Id primitiveId, UInt32 subMeshIndex, byte* nameBuffer, UInt32 bufferByteSize);
    }
}
