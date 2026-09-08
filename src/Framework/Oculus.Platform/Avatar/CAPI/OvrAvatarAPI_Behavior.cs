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
        public enum ovrAvatar2GazeTargetType : Int32
        {
            AvatarHead,
            AvatarHand,
            Object,
            ObjectStatic,
            Count
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2GazeTarget
        {
            public ovrAvatar2Id id;
            public ovrAvatar2Vector3f worldPosition;
            public ovrAvatar2GazeTargetType type;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Behavior_CreateGazeTargets(IntPtr targets, int targetCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Behavior_DestroyGazeTargets(IntPtr targets, int targetCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern ovrAvatar2Result ovrAvatar2Behavior_UpdateGazeTargetPositions(CAPI.ovrAvatar2GazeTarget* targets, int targetCount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Behavior_GetGazePos(ovrAvatar2EntityId entityId, ref ovrAvatar2Vector3f outPos);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_SetCustomWristOffset(ovrAvatar2EntityId entityId, ovrAvatar2Side side, in ovrAvatar2Transform offset);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_SetCustomHandSkeleton(ovrAvatar2EntityId entityId, ovrAvatar2Side side, in ovrAvatar2TrackingBodySkeleton skeleton);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_SetCustomHandPose(ovrAvatar2EntityId entityId, ovrAvatar2Side side, in ovrAvatar2TrackingBodyPose pose);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_ClearCustomHandPose(ovrAvatar2EntityId entityId, ovrAvatar2Side side);
    }
}
