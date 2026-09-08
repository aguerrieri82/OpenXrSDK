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
        public struct ovrAvatar2LODRegistration
        {
            public Int32 avatarId;
            public Int32[] lodWeights;
            public Int32 lodThreshold;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODRegistrationNative
        {
            public Int32 avatarId;
            public IntPtr lodWeights;
            public Int32 lodWeightCount;
            public Int32 lodThreshold;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODUpdate
        {
            public Int32 avatarId;
            [MarshalAs(UnmanagedType.U1)]
            public bool isPlayer;
            [MarshalAs(UnmanagedType.U1)]
            public bool isCulled;
            public Int32 importanceScore;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODInput
        {
            public Int32 avatarId;
            public Int32 minLod;
            public Int32 maxLod;
            public bool toProcess;
            public bool isCulled;
            public bool isPlayer;
            public ovrAvatar2Vector3f pos;
            public float scale;
            public Int32 triangleData0;
            public Int32 triangleData1;
            public Int32 triangleData2;
            public Int32 triangleData3;
            public Int32 triangleData4;
            public Int32 prevLOD;
            public Int32 assignedLOD;
            public Int32 wantedLOD;
            public float fracLOD;
            public Int32 lodImportance;
            public bool LODToggled;
            public bool cullToggled;
            public float importance;
            public float distance;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODCamera
        {
            public float twoOverFov;
            public float height;
            public ovrAvatar2Vector3f position;
            public ovrAvatar2Vector3f forward;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODParameters
        {
            public float dynamicLodWantedLogScale;
            public float updateImportanceUpdatePower;
            public float updateImportanceUpdateMult;
            public Int32 lodCountPerFrame;
            public Int32 geometryTriLimit;
            public Int32 numDynamicLevels;
            public Int32 maxActiveAvatars;
            public Int32 maxVerticesToSkin;
            public ovrAvatar2Vector4f cullingPlane0;
            public ovrAvatar2Vector4f cullingPlane1;
            public ovrAvatar2Vector4f cullingPlane2;
            public ovrAvatar2Vector4f cullingPlane3;
            public ovrAvatar2Vector4f cullingPlane4;
            public ovrAvatar2Vector4f cullingPlane5;
            public bool performCulling;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODStats
        {
            public Int32 numTrisRequested;
            public Int32 numTrisFitted;
            unsafe public fixed Int32 NumLODsRequested[5];
            unsafe public fixed Int32 NumLODsFitted[5];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LODResult
        {
            public Int32 avatarId;
            public Int32 assignedLOD;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LOD_UnregisterAvatar(Int32 id);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LOD_AvatarRegistered(Int32 id);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2LOD_RegisterAvatar")]
        public static extern ovrAvatar2Result ovrAvatar2LOD_RegisterAvatarNative(IntPtr recrord);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrAvatar2LOD_SetDistribution(Int32 maxWeightValue, float exponent);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2LOD_GenerateDistribution")]
        unsafe public static extern ovrAvatar2Result ovrAvatar2LOD_GenerateDistributionNative(Int32* weightDistribution, Int32 distributionCount, ovrAvatar2LODUpdate* lodUpdates, ovrAvatar2LODResult* lodResults, Int32 avatarCount, Int32* totalAssignedWeight);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2LOD_GenerateLodLevels")]
        unsafe public static extern ovrAvatar2Result ovrAvatar2LOD_GenerateLodLevelsNative(ovrAvatar2LODParameters* lodParams, ovrAvatar2LODCamera* cameras, Int32 cameraCount, ovrAvatar2LODInput* lodInputs, Int32 avatarCount, out ovrAvatar2LODStats outStats);
    }
}
