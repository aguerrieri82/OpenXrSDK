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
using ovrAvatar2StringView = Oculus.Avatar2.Experimental.CAPI.ovrAvatar2StringView;

namespace Oculus.Avatar2
{
    using ovrAvatar2SizeType = UIntPtr;

    public static partial class CAPI
    {
        public const string entityLogScope = "OvrAvatarAPI_Entity";
        [Flags]
        [System.Serializable]
        //
        // Describes avatar rendering and animation capabilities.
        //
        public enum ovrAvatar2EntityFeatures : Int32
        {
            // Empty features flag, usually used for error signaling
            /* None value isn't needed in C# and conflicts w/ some Unity inspector logic for Flags */
            // None = 0,
            // Reserved for future use
            ReservedExtra = 1 << 0,
            // Render avatar geometry
            Rendering_Prims = 1 << 1,
            // Perform skinning on avatar
            Rendering_SkinningMatrices = 1 << 2,
            // 1 << 3 was previously for Rendering_ObjectSpaceTransforms. No longer
            // does anything. Maybe re-used for another purpose in the future.
            // Allow avatar animation
            Animation = 1 << 4,
            //  Use default avatar model
            UseDefaultModel = 1 << 5,
            // Use default animation hierarchy
            UseDefaultAnimHierarchy = 1 << 6,
            //  Do not use.
            AnalyticIk = 1 << 7,
            // Use default facial animations
            UseDefaultFaceAnimations = 1 << 8,
            // Display controllers in avatar hands (not implemented yet)
            ShowControllers = 1 << 9,
            // Reproportions avatar hand bones according to tracking information in hand tracking mode
            HandScaling = 1 << 10,
            // Allows to control the leg end-effector transforms using a two-bone IK solver.
            LegIk = 1 << 11,
            // Base set of features needed for entity rendering
            Rendering = Rendering_Prims | Rendering_SkinningMatrices,
            // Collection of all current feature flags
            All = Rendering_Prims | Rendering_SkinningMatrices | Animation | UseDefaultModel | UseDefaultAnimHierarchy | AnalyticIk | UseDefaultFaceAnimations | ShowControllers | HandScaling | LegIk,
            // Preset collection of feature flags for standard local avatar use case
            Preset_Default = Rendering | Animation | UseDefaultModel | UseDefaultAnimHierarchy | UseDefaultFaceAnimations | HandScaling,
            // Preset collection for using AnalyticIk/SimpleIk
            Preset_AllIk = Preset_Default | AnalyticIk,
            // Preset for minimum functional local avatar
            Preset_Minimal = Rendering | Animation,
            // Preset for common remote avatar usage
            Preset_Remote = Rendering | UseDefaultModel | UseDefaultAnimHierarchy,
        }

        public const ovrAvatar2EntityFeatures ovrAvatar2EntityFeatures_First = ovrAvatar2EntityFeatures.ReservedExtra;
        public const ovrAvatar2EntityFeatures ovrAvatar2EntityFeatures_Last = ovrAvatar2EntityFeatures.LegIk;
        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2EntityFilters
        {
            public ovrAvatar2EntityLODFlags lodFlags;
            public ovrAvatar2EntityManifestationFlags manifestationFlags;
            public ovrAvatar2EntityViewFlags viewFlags;
            public ovrAvatar2EntitySubMeshInclusionFlags subMeshInclusionFlags;
            public ovrAvatar2EntityQuality quality;
            [MarshalAs(UnmanagedType.U1)]
            public bool loadRigZipFromGlb;
            public fixed byte reserved[32];
            public ovrAvatar2EntitySubMeshVertexPreference subMeshVertexPreference;
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2EntityCreateInfo
        {
            public ovrAvatar2EntityFeatures features;
            public ovrAvatar2EntityFilters renderFilters;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Create(in ovrAvatar2EntityCreateInfo info, out ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Destroy(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetAvailableLodFlags(ovrAvatar2EntityId entityId, out UInt32 lodFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetLodFlags(ovrAvatar2EntityId entityId, out ovrAvatar2EntityLODFlags lodFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetLodFlags(ovrAvatar2EntityId entityId, ovrAvatar2EntityLODFlags lodflags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetAvailableManifestationFlags(ovrAvatar2EntityId entityId, out UInt32 manifestationFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetManifestationFlags(ovrAvatar2EntityId entityId, out ovrAvatar2EntityManifestationFlags manifestationflags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetManifestationFlags(ovrAvatar2EntityId entityId, ovrAvatar2EntityManifestationFlags manifestation);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetAvailableViewFlags(ovrAvatar2EntityId entityId, out UInt32 viewFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetViewFlags(ovrAvatar2EntityId entityId, out ovrAvatar2EntityViewFlags viewFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetViewFlags(ovrAvatar2EntityId entityId, ovrAvatar2EntityViewFlags viewflags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetSubMeshInclusionFlags(ovrAvatar2EntityId entityId, out ovrAvatar2EntitySubMeshInclusionFlags subMeshInclusionFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetSubMeshInclusionFlags(ovrAvatar2EntityId entityId, ovrAvatar2EntitySubMeshInclusionFlags subMeshInclusionFlags);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetQuality(ovrAvatar2EntityId entityId, out ovrAvatar2EntityQuality quality);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetQuality(ovrAvatar2EntityId entityId, ovrAvatar2EntityQuality quality);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetPose(ovrAvatar2EntityId entityId, out ovrAvatar2Pose posePtr, out ovrAvatar2HierarchyVersion hierarchyVersion);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_UpdatePose(ovrAvatar2EntityId entityId, in ovrAvatar2Pose posePtr);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetRoot(ovrAvatar2EntityId entityId, ovrAvatar2Transform root);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Entity_SetRoots(ovrAvatar2EntityId* entityIds, ovrAvatar2Transform* roots, UInt32 numEntities);
        [System.Serializable]
        public enum ovrAvatar2PointCorrespondenceQuerySpace : Int32
        {
            // point is defined in character space, relative to the Avatar's root transform
            ovrAvatar2PointCorrespondenceQuerySpace_DefaultPoseCharacterSpace = 0,
        }

        [System.Serializable]
        public enum ovrAvatar2PointCorrespondenceSurfaceSelectionMode : Int32
        {
            // select the intent spaces by proximity of input point to space(s)
            ovrAvatar2PointCorrespondenceSurfaceSelectionMode_PointProximityToBox = 0,
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2PointCorrespondenceSurfaceSelection
        {
            public ovrAvatar2PointCorrespondenceSurfaceSelectionMode mode;
            public float maxDist;
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2PointCorrespondenceQuery
        {
            public ovrAvatar2PointCorrespondenceQuerySpace pointSpace;
            public ovrAvatar2Transform pointTransform;
            public ovrAvatar2PointCorrespondenceSurfaceSelection surfaceSelection;
        }

        [System.Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2PointCorrespondenceQueryResult
        {
            public ovrAvatar2Transform transform;
            [MarshalAs(UnmanagedType.U1)]
            public bool valid;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Entity_ComputeImmediatePointCorrespondence(ovrAvatar2EntityId entityId, ovrAvatar2PointCorrespondenceQuery* queries, ovrAvatar2SizeType queryCount, ovrAvatar2PointCorrespondenceQueryResult* queryResults);
        public struct ovrAvatar2EntityLoadNetworkSettings
        {
            public UInt32 timeoutMS;
            public UInt32 lowSpeedTimeSeconds;
            public UInt32 lowSpeedLimitBytesPerSecond;
        }

        public struct ovrAvatar2EntityLoadSettings
        {
            public ovrAvatar2EntityFilters loadFilters;
            public ovrAvatar2EntityLoadNetworkSettings loadSpecificationNetworkSettings;
            public ovrAvatar2EntityLoadNetworkSettings loadAssetNetworkSettings;
            public UInt32 maxTextureMemoryBytes;
            public UInt32 numLodsWithMorphs;
            public byte reserved1;
            [MarshalAs(UnmanagedType.U1)]
            public bool prefetchOnly;
            [MarshalAs(UnmanagedType.U1)]
            public bool validateCache;
            public byte reserved2;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2EntityLoadSettings ovrAvatar2Entity_DefaultLoadSettings();
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2EntityLoadSettings ovrAvatar2Entity_MinimumLoadSettings();
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_LoadUri(ovrAvatar2EntityId entityId, string path, ovrAvatar2EntityLoadSettings loadSettings, out ovrAvatar2LoadRequestId requestId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_LoadMemory(ovrAvatar2EntityId entityId, IntPtr data, UInt32 dataSize, string name, ovrAvatar2EntityLoadSettings loadSettings, out ovrAvatar2LoadRequestId requestId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_LoadUser(ovrAvatar2EntityId entityId, UInt64 userId, ovrAvatar2EntityLoadSettings loadSettings, out ovrAvatar2LoadRequestId requestId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_UnloadDefaultModel(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_UnloadUri(ovrAvatar2EntityId entityId, string uri);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public unsafe static extern ovrAvatar2Result ovrAvatar2Entity_UnloadMemory(ovrAvatar2EntityId entityId, /*const*/ char* name);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_UnloadUser(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetCreationContext(ovrAvatar2EntityId entityId, ovrAvatar2StringView creationContext);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 ovrAvatar2Entity_GetNumLoadedAssets(ovrAvatar2EntityId entityId);
        [System.Serializable]
        public enum ovrAvatar2EntityAssetType : Int32
        {
            SystemDefaultModel = 0,
            SystemOther = 1,
            Other = 2,
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ovrAvatar2Result ovrAvatar2Entity_GetLoadedAssetTypes(ovrAvatar2EntityId entityId, ovrAvatar2EntityAssetType* typesBuffer, UInt32 bufferSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetStatus(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SetActive(ovrAvatar2EntityId entityId, [MarshalAs(UnmanagedType.U1)] bool active);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetActive(ovrAvatar2EntityId entityId, [MarshalAs(UnmanagedType.U1)] out bool isActive);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Entity_GetActives(ovrAvatar2EntityId* entityIds, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] bool* isActives, uint numIds);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Entity_GetNodeName(ovrAvatar2EntityId entityId, ovrAvatar2NodeId nodeId, byte* nameBuffer, UInt32 nameBufferSize, out UInt32 nameLength);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ovrAvatar2Result ovrAvatar2Entity_QueryJointTypeNodes(ovrAvatar2EntityId entityId, /*const*/
        ovrAvatar2JointType* jointTypes, UInt32 jointTypeCount, ovrAvatar2NodeId* nodeIds);
    }
}
