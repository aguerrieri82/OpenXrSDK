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
        public const string StreamingCapiLogScope = "OvrAvatarAPI_Streaming";
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2StreamingPlaybackState
        {
            public UInt32 numSamples;
            public float interpolationBlendWeight;
            public UInt64 oldestSampleTime;
            public UInt64 latestSampleTime;
            public UInt64 remoteTime;
            public UInt64 localTime;
            public UInt64 recordingPlaybackTime;
            [MarshalAs(UnmanagedType.U1)]
            public bool poseValid;
            public UInt64 playbackTime;
        }

        public enum ovrAvatar2StreamLOD : Int32
        {
            Full, // Full avatar state with lossless compression
            High, // Full avatar state with lossy compression
            Medium, // Partial avatar state with lossy compression
            Low, // Minimal avatar state with lossy compression
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordStart(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordStop(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_RecordSnapshot(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_SerializeRecording(ovrAvatar2EntityId entityId, ovrAvatar2StreamLOD lod, byte* destinationPtr, ref UInt64 bytes);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_GetRecordingSize(ovrAvatar2EntityId entityId, ovrAvatar2StreamLOD lod, out UInt64 bytes);
        public enum DeserializationResult
        {
            UnknownError = -1,
            Success = 0,
            DeserializationPending = 1,
            PlaybackDisabled = 2,
            BufferTooSmall = 3,
            InvalidData = 4,
            SkeletonMismatch = 5,
            EntityNotReady = 6,
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_PlaybackStart(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_PlaybackStop(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_SetPlaybackTimeDelay(ovrAvatar2EntityId entityId, float delaySeconds);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_SetAutoPlaybackTimeDelay(ovrAvatar2EntityId entityId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern CAPI.ovrAvatar2Result ovrAvatar2Streaming_GetPlaybackState(ovrAvatar2EntityId entityId, out ovrAvatar2StreamingPlaybackState playbackState);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe CAPI.ovrAvatar2Result ovrAvatar2Streaming_DeserializeRecording(ovrAvatar2EntityId entityId, byte* sourceBuffer, UInt64 bytes);
    }
}
