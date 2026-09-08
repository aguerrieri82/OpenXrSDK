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

    public static partial class CAPI
    {
        public const string eventsInternalLogScope = "eventsInternalCAPI";
        public enum ovrAvatar2EventId : UInt64
        {
            Invalid = 0,
        }

        public enum ovrAvatar2EventPayloadTypeId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2ExperimentalEventPayloadTypeId : Int32
        {
            Invalid = 0,
            Vector2f = 1,
            Vector3f = 2,
            Vector4f = 3,
            Quatf = 4,
            Transform = 5,
            Matrix4f = 6,
            String = 7,
            Count = 8
        }

        public enum ovrAvatar2EventPayloadDomainId : Int32
        {
            Invalid = 0,
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ovrAvatar2EventPayloadId
        {
            public readonly ovrAvatar2EventPayloadTypeId typeId;
            public readonly ovrAvatar2EventPayloadDomainId domainId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ovrAvatar2EventDefinition
        {
            public readonly ovrAvatar2EventId eventId;
            public readonly ovrAvatar2EventPayloadId payloadId;
        }

        public enum ovrAvatar2PrimitiveTypeId : Int32
        {
            Void = 0, // This almost always indicates an error
            Bit = 1,
            Bool = 2,
            Char = 3,
            Int8 = 4,
            Uint8 = 5,
            Int16 = 6,
            Uint16 = 7,
            Int32 = 8,
            Uint32 = 9,
            Int64 = 10,
            Uint64 = 11,
            Float = 12,
            Double = 13,
            Count = 14,
        }

        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_PayloadIdForPrimitive(ovrAvatar2PrimitiveTypeId primitiveId, out ovrAvatar2EventPayloadId outPayloadId);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_GetEventId(in ovrAvatar2StringView eventNameView, out ovrAvatar2EventId eventId);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_GetName(ovrAvatar2EventId eventId, ref ovrAvatar2StringBuffer name);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_GetPayloadTypeName(ovrAvatar2EventPayloadId payloadId, ref ovrAvatar2StringBuffer name);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_RegisterEventDefinition(in ovrAvatar2EventDefinition eventDefinition);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool ovrAvatar2Event_SynchronousEventCallbackWithPayload(IntPtr owner, in ovrAvatar2EventDefinition eventDef, in ovrAvatar2DataView payload);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_Experimental_SubscribeToEventWithPayload(in ovrAvatar2EventDefinition eventDefinition, IntPtr owner, ovrAvatar2Event_SynchronousEventCallbackWithPayload callback);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Event_Experimental_UnsubscribeFromEvent(ovrAvatar2EventId eventId, IntPtr owner);
    }
}
