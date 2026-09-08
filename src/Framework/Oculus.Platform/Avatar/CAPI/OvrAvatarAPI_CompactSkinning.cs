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
using static Oculus.Avatar2.CAPI;

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        public const string compactSkinningLogScope = "OvrAvatarAPI_CompactSkinning";
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct OvrAvatar2CompactSkinningMetaData
        {
            public readonly UInt32 numMorphedVerts;
            public readonly UInt32 numJointsOnlyVerts;
            public readonly UInt32 numStaticVerts;
            public readonly UInt32 maxAmountOfMorphsAffectingSingleVert;
            public readonly UInt32 isInClientCoordSpace;
            public readonly ovrAvatar2Matrix4f clientCoordSpaceTransform;
            public readonly ovrAvatar2Matrix4f clientCoordSpaceNormalTransform;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactMeshData_GetMetaData(ovrAvatar2VertexBufferId id, ovrAvatar2CompactMeshAttributes attributes, ovrAvatar2BufferMetaData* metaData, UInt64 metaDataSize, out UInt64 dataBufferSizeBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactMeshData_CopyBuffer(ovrAvatar2Id primitiveID, ovrAvatar2VertexBufferId vertBufferID, ovrAvatar2CompactMeshAttributes attributes, ovrAvatar2DataBlock dataBuffer);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetPositionsMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyPositions(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetNormalsMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyNormals(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetTangentsMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyTangents(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetVertexReorderMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyVertexReorder(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetVertexInverseReorderMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyVertexInverseReorder(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetNumMorphsBufferMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyNumMorphsBuffer(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMorphPositionDeltasMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyMorphPositionDeltas(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMorphNormalDeltasMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyMorphNormalDeltas(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMorphTangentDeltasMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyMorphTangentDeltas(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes, out ovrAvatar2Vector3f normalizationOffset, out ovrAvatar2Vector3f normalizationScale);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMorphIndicesMetaData(ovrAvatar2CompactSkinningDataId id, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyMorphIndices(ovrAvatar2CompactSkinningDataId id, Byte* dataBuffer, UInt32 dataBufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMorphNextEntriesMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyMorphNextEntries(ovrAvatar2CompactSkinningDataId id, Byte* dataBuffer, UInt32 dataBufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetJointIndicesMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyJointIndices(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetJointWeightsMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out ovrAvatar2BufferMetaData metaData);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrCompactSkinningData_CopyJointWeights(ovrAvatar2CompactSkinningDataId compactSkinningDataId, Byte* dataBuffer, UInt32 bufferSizeBytes, UInt32 dataBufferStrideBytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrCompactSkinningData_GetMetaData(ovrAvatar2CompactSkinningDataId compactSkinningDataId, out OvrAvatar2CompactSkinningMetaData metaData);
    }
}
