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

namespace Oculus.Avatar2.Experimental
{
    using ovrAvatar2Result = Avatar2.CAPI.ovrAvatar2Result;
    using ovrAvatar2EntityId = Avatar2.CAPI.ovrAvatar2EntityId;

    public static partial class CAPI
    {
        public const string entityInternalLogScope = "entityInternalCAPI";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool ovrAvatar2Entity_SynchronousEventCallback(IntPtr owner, ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool ovrAvatar2Entity_SynchronousEventCallbackWithPayload(IntPtr owner, ovrAvatar2EntityId entityId, in ovrAvatar2EventDefinition eventDef, in ovrAvatar2DataView payload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SendEvent(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_SendEventWithPayload(ovrAvatar2EntityId entityId, in ovrAvatar2EventDefinition eventDef, in ovrAvatar2DataView payload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithStringPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, string stringPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithFloatPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, float floatPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithBoolPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, bool boolPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithVec2fPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Vector2f vec2fPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithVec3fPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Vector3f vec3fPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithVec4fPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Vector4f vec4fPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithQuatfPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Quatf quatPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithPositionPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Vector3f positionPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithOrientationPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Quatf orientationPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithScalePayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Vector3f scalePayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SendEventWithTransformPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, Avatar2.CAPI.ovrAvatar2Transform transformPayload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SubscribeToEvent(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, ovrAvatar2Entity_SynchronousEventCallback callback, IntPtr owner);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_SubscribeToEventWithPayload(ovrAvatar2EntityId entityId, ovrAvatar2EventId eventId, ovrAvatar2Entity_SynchronousEventCallbackWithPayload callback, IntPtr owner);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_Experimental_UnsubscribeAllEvents(ovrAvatar2EntityId entityId, IntPtr owner);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatarXEntity_GetEventDefinitions(ovrAvatar2EntityId entityId, ovrAvatar2EventDefinition* definitions, uint definitionsCapacity, uint* totalDefinitions);
        public enum ovrAvatar2AbstractPoseJoints : Int32
        {
            ovrAvatar2AbstractPoseJoints_HandGripLeft = 0,
            ovrAvatar2AbstractPoseJoints_HandGripRight = 1,
            ovrAvatar2AbstractPoseJoints_Count = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2AbstractPose
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)Experimental.CAPI.ovrAvatar2AbstractPoseJoints.ovrAvatar2AbstractPoseJoints_Count)]
            public Avatar2.CAPI.ovrAvatar2Transform[] transforms;
        }

        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_GetAbstractPose(ovrAvatar2EntityId entityId, out ovrAvatar2AbstractPose outPose);
    }
}

namespace Oculus.Avatar2
{
    using EXPERIMENTAL_CAPI = Oculus.Avatar2.Experimental.CAPI;
}
