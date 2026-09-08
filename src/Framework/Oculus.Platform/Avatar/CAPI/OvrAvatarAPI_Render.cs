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

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe readonly ref struct ovrAvatar2EntityRenderState
        {
            public readonly ovrAvatar2Transform rootTransform;
            public readonly UInt32 primitiveCount;
            public readonly ovrAvatar2HierarchyVersion hierarchyVersion;
            public readonly ovrAvatar2EntityRenderStateVersion allNodesVersion;
            public readonly ovrAvatar2EntityRenderStateVersion visibleNodesVersion;
            public readonly ovrAvatar2NodeId* allMeshNodes;
            public readonly ovrAvatar2NodeId* visibleMeshNodes;
            public readonly UInt32 allMeshNodesCount;
            public readonly UInt32 visibleMeshNodesCount;
        }

        public enum ovrAvatar2PrimitiveRenderInstanceID : Int32
        {
            Invalid = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ovrAvatar2PrimitiveRenderState
        {
            public readonly ovrAvatar2PrimitiveRenderInstanceID id;
            public readonly ovrAvatar2Id primitiveId;
            public readonly ovrAvatar2NodeId meshNodeId;
            public readonly ovrAvatar2Transform localTransform;
            public readonly ovrAvatar2Transform worldTransform;
            public readonly ovrAvatar2Pose pose;
            public readonly UInt32 morphTargetCount;
            public readonly ovrAvatar2Transform skinningOrigin;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Render_QueryRenderState(ovrAvatar2EntityId entityId, out ovrAvatar2EntityRenderState outState);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Render_GetPrimitiveRenderStateByIndex(ovrAvatar2EntityId entityId, UInt32 primitiveRenderStateIndex, out ovrAvatar2PrimitiveRenderState outState);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Render_GetPrimitiveRenderStatesByIndex(ovrAvatar2EntityId entityId, UInt32* primitiveRenderStateIndices, ovrAvatar2PrimitiveRenderState* outState, UInt32 numRenderStates);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2PrimitiveRenderState* ovrAvatarXRender_GetPrimitiveRenderStates(ovrAvatar2EntityId entityId, out UInt32 outCount, out UInt32 outStride);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Render_GetSkinTransforms(ovrAvatar2EntityId entityId, ovrAvatar2PrimitiveRenderInstanceID instanceId, /*ovrAvatar2Matrix4f[]*/ IntPtr skinTransforms, UInt32 bytes, bool interleaveNormalMatrices);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Render_GetMorphTargetWeights(ovrAvatar2EntityId entityId, ovrAvatar2PrimitiveRenderInstanceID instanceId, /*float[]*/ IntPtr morphTargetWeights, UInt32 bytes);
    }
}
