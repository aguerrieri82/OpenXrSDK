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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        public enum ovrAvatar2EntityId : Int32
        {
            Invalid = 0
        }

        public enum ovrAvatar2RequestId : Int32
        {
            Invalid = 0
        }

        public enum ovrAvatar2Id : Int32
        {
            Invalid = 0
        }

        public enum ovrAvatar2VertexBufferId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2MorphTargetBufferId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2CompactSkinningDataId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2NodeId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2LoadRequestId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2HierarchyVersion : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2EntityRenderStateVersion : Int32
        {
            Invalid = 0,
        }

        [Flags]
        [System.Serializable]
        public enum ovrAvatar2EntityLODFlags : Int32
        {
            // level of detail 0 (highest fidelity)
            LOD_0 = 1 << 0,
            // level of detail 1
            LOD_1 = 1 << 1,
            // level of detail 2
            LOD_2 = 1 << 2,
            // level of detail 3
            LOD_3 = 1 << 3,
            // level of detail 4 (lowest level)
            LOD_4 = 1 << 4,
            // All levels of detail
            All = LOD_0 | LOD_1 | LOD_2 | LOD_3 | LOD_4,
        }

        public const uint ovrAvatar2EntityLODFlagsCount = 5;
        [Flags]
        [System.Serializable]
        public enum ovrAvatar2EntityManifestationFlags : Int32
        {
            // No avatar parts manifested
            None = 0,
            // All body parts
            Full = 1 << 0,
            // Upper body only
            Half = 1 << 1,
            // Head and hands only
            HeadHands = 1 << 2,
            // Head only
            Head = 1 << 3,
            // Hands only
            Hands = 1 << 4,
            //  All manifestations requested.
            All = Full | Half | HeadHands | Head | Hands,
        }

        [Flags]
        [System.Serializable]
        public enum ovrAvatar2EntityViewFlags : Int32
        {
            None = 0,
            // First person view
            FirstPerson = 1 << 0,
            // Third person view
            ThirdPerson = 1 << 1,
            // All views
            All = FirstPerson | ThirdPerson
        }

        [System.Serializable]
        public enum ovrAvatar2EntitySubMeshVertexPreference : Int32
        {
            // Will automatically select the best value shipped with this version of SDK.
            Default = 0,
            // The original index of the sub-mesh in this primitive's combined collection.
            OriginalIndex = 1,
            // Sub-mesh inclusion flags represent a grouping of sub-meshes based on physical qualities.
            InclusionFlag = 2,
            // Enumeration of the type of material, where each material has a pre-determined shader sub-technique.
            MaterialType = 3,
        }

        [Flags]
        [System.Serializable]
        public enum ovrAvatar2EntitySubMeshInclusionFlags : Int32
        {
            None = 0,
            // Outfit only
            Outfit = 1 << 0,
            // Body only
            Body = 1 << 1,
            // Head only
            Head = 1 << 2,
            // Hair only
            Hair = 1 << 3,
            // Eyebrow only
            Eyebrow = 1 << 4,
            // L Eye only
            L_Eye = 1 << 5,
            // R Eye only
            R_Eye = 1 << 6,
            // Lashes only
            Lashes = 1 << 7,
            // Facial hair only
            FacialHair = 1 << 8,
            // Headwear only
            Headwear = 1 << 9,
            // Earrings only
            Earrings = 1 << 10,
            // Mouth only
            Mouth = 1 << 11,
            // IMPORTANT: SubMesh_All_Exclusive (below) needs to be updated as new enumerations are added
            //  Works both as a test and also might be useful in some real applications.
            BothEyes = L_Eye | R_Eye,
            //  All manifestations requested. To accomodate the Unity IDE and
            //  prefabs, his -1 value should very intentionally keep all bits filled.
            //  As flags are added, they'll be part of this value without entity updates.
            All = -1,
        }

        public const Int32 SubMesh_All_Exclusive = -(1 - ((Int32)ovrAvatar2EntitySubMeshInclusionFlags.Mouth));
        [System.Serializable]
        public enum ovrAvatar2EntityQuality : Int32
        {
            // Default quality with normal maps and hair maps.
            Standard = 0,
            // Lower quality but lighter-weight. No normal maps. Metallic-roughness and skin shading still on.
            Light = 1,
            // Extremely light. No textures, only vertex colors. LODs 2 and 4 only.
            Ultralight = 2
        }

        [Flags]
        public enum ovrAvatar2EntityQualityFlags : Int32
        {
            None = 0,
            Standard = 1 << 0,
            Light = 1 << 1,
            Ultralight = 1 << 2,
            All = Standard | Light | Ultralight
        }

        public enum ovrAvatar2DataFormat : Int32
        {
            Invalid = 0,
            U8 = 1,
            //< Unsigned 8 bit integer
            U16 = 2,
            //< Unsigned 16 bit integer
            U32 = 3,
            //< Unsigned 32 bit integer
            S8 = 4,
            //< Signed 8 bit integer
            S16 = 5,
            //< Signed 16 bit integer
            S32 = 6,
            //< Signed 32 bit integer
            F16 = 7,
            //< 16 bit floating point number
            F32 = 8,
            //< 32 bit floating point number
            Unorm8 = 9,
            //< 8 bit unsigned normalized number
            Unorm10_10_10_2 = 10,
            // < 4 unsigned normalized components (10/10/10/2 bits
            // )
            Unorm16 = 11,
            //< 16 bit unsigned normalized number
            Snorm8 = 12,
            //< 8 bit signed normalized number
            Snorm10_10_10_2 = 13,
            // < 4 signed normalized components (10/10/10/2 bits
            // )
            Snorm16 = 14, //< 16 bit signed normalized number
        }

        public enum ovrAvatar2CompactMeshAttributes : Int32
        {
            //< Usually an error, for completeness.
            None = 0,
            //< Vertex Colors and material type
            Colors = 1 << 0,
            //< Will be present if the primitive has textures.
            TexCoord0 = 1 << 1,
            //< Vertex Colors ORMT/OFSB
            Properties = 1 << 2,
            //< Curvature information for skin
            Curvature = 1 << 3,
            All = TexCoord0 | Colors | Properties | Curvature,
        }

        public const int ovrAvatar2CompactMeshAttributesCount = 4;
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Vector2f
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ovrAvatar2Vector2u
        {
            public readonly UInt32 x;
            public readonly UInt32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Vector3f
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Vector4f
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Vector4ub
        {
            public Byte x;
            public Byte y;
            public Byte z;
            public Byte w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Vector4us
        {
            public UInt16 x;
            public UInt16 y;
            public UInt16 z;
            public UInt16 w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Quatf
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Transform
        {
            public ovrAvatar2Vector3f position;
            public ovrAvatar2Quatf orientation;
            public ovrAvatar2Vector3f scale;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Matrix4f
        {
            public float m00, m10, m20, m30;
            public float m01, m11, m21, m31;
            public float m02, m12, m22, m32;
            public float m03, m13, m23, m33;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly unsafe struct ovrAvatar2Pose
        {
            public readonly UInt32 jointCount;
            public readonly ovrAvatar2Transform* localTransforms;
            public readonly ovrAvatar2Transform* objectTransforms;
            public readonly Int32* parents;
            public readonly ovrAvatar2NodeId* nodeIds;
        }

        public enum ovrAvatar2Side : Int32
        {
            Left = 0,
            Right = 1,
            Count = 2
        }

        public enum ovrAvatar2Result : Int32
        {
            Success = 0,
            Unknown = 1,
            OutOfMemory = 2,
            NotInitialized = 3,
            AlreadyInitialized = 4,
            BadParameter = 5,
            Unsupported = 6,
            NotFound = 7,
            AlreadyExists = 8,
            IndexOutOfRange = 9,
            InvalidEntity = 10,
            Reserved_11 = 11, // Formerly InvalidThread, but the native side is neutral about this now
            BufferTooSmall = 12,
            DataNotAvailable = 13,
            InvalidData = 14,
            SkeletonMismatch = 15,
            LibraryLoadFailed = 16,
            Pending = 17,
            MissingAccessToken = 18,
            MemoryLeak = 19,
            RequestCallbackNotSet = 20,
            UnmatchedLoadFilters = 21,
            DeserializationPending = 22,
            StaticJointTypeFallback = 23,
            UnableToConnectToDevTools = 24,
            RequestCancelled = 25,
            BufferLargerThanExpected = 26,
            BufferMisaligned = 27,
            TypeMismatch = 28,
            Count,
        }

        public enum ovrAvatar2JointType : Int32
        {
            Invalid = -1,
            Root = 0,
            Hips = 1,
            LeftLegUpper = 2,
            LeftLegLower = 3,
            LeftFootAnkle = 4,
            LeftFootBall = 5,
            RightLegUpper = 6,
            RightLegLower = 7,
            RightFootAnkle = 8,
            RightFootBall = 9,
            SpineLower = 10,
            SpineMiddle = 11,
            SpineUpper = 12,
            Chest = 13,
            Neck = 14,
            Head = 15,
            LeftShoulder = 16,
            LeftArmUpper = 17,
            LeftArmLower = 18,
            LeftHandWrist = 19,
            RightShoulder = 20,
            RightArmUpper = 21,
            RightArmLower = 22,
            RightHandWrist = 23,
            LeftHandThumbTrapezium = 24,
            LeftHandThumbMeta = 25,
            LeftHandThumbProximal = 26,
            LeftHandThumbDistal = 27,
            LeftHandIndexMeta = 28,
            LeftHandIndexProximal = 29,
            LeftHandIndexIntermediate = 30,
            LeftHandIndexDistal = 31,
            LeftHandMiddleMeta = 32,
            LeftHandMiddleProximal = 33,
            LeftHandMiddleIntermediate = 34,
            LeftHandMiddleDistal = 35,
            LeftHandRingMeta = 36,
            LeftHandRingProximal = 37,
            LeftHandRingIntermediate = 38,
            LeftHandRingDistal = 39,
            LeftHandPinkyMeta = 40,
            LeftHandPinkyProximal = 41,
            LeftHandPinkyIntermediate = 42,
            LeftHandPinkyDistal = 43,
            RightHandThumbTrapezium = 44,
            RightHandThumbMeta = 45,
            RightHandThumbProximal = 46,
            RightHandThumbDistal = 47,
            RightHandIndexMeta = 48,
            RightHandIndexProximal = 49,
            RightHandIndexIntermediate = 50,
            RightHandIndexDistal = 51,
            RightHandMiddleMeta = 52,
            RightHandMiddleProximal = 53,
            RightHandMiddleIntermediate = 54,
            RightHandMiddleDistal = 55,
            RightHandRingMeta = 56,
            RightHandRingProximal = 57,
            RightHandRingIntermediate = 58,
            RightHandRingDistal = 59,
            RightHandPinkyMeta = 60,
            RightHandPinkyProximal = 61,
            RightHandPinkyIntermediate = 62,
            RightHandPinkyDistal = 63,
            Count
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2DataBlock
        {
            public byte* data;
            public UInt64 size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2DataSpan
        {
            public byte* data;
            public UInt64 size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2DataBuffer
        {
            ovrAvatar2DataSpan data;
            public UInt64 bytesWritten;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ovrAvatar2BufferMetaData
        {
            public readonly ovrAvatar2DataFormat dataFormat;
            public readonly UInt32 dataSizeBytes;
            public readonly UInt32 strideBytes;
            public readonly UInt32 count;
        }
    }
}
