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
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace Oculus.Avatar2.Experimental
{
    using ovrAvatar2Result = Avatar2.CAPI.ovrAvatar2Result;
    using ovrAvatar2Space = Avatar2.CAPI.ovrAvatar2Space;
    using ovrAvatar2Transform = Avatar2.CAPI.ovrAvatar2Transform;
    using ovrAvatar2EntityId = Avatar2.CAPI.ovrAvatar2EntityId;
    using ovrAvatar2LogLevel = Avatar2.CAPI.ovrAvatar2LogLevel;
    using ovrAvatar2DataBuffer = Avatar2.CAPI.ovrAvatar2DataBuffer;
    using ovrAvatar2SizeType = UIntPtr;

    public static partial class CAPI
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe ref struct ovrAvatar2Prototype_Behavior_Pose
        {
            public ovrAvatar2Space space;
            public UInt32 transformCount;
            public ovrAvatar2Transform* transforms;
            public UInt32* transformsMask;
            public UInt32 floatCount;
            public float* floats;
            public UInt32* floatsMask;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public unsafe ref struct ovrAvatar2Prototype_Behavior_Hierarchy
        {
            public UInt32 jointCount;
            public byte** jointNames;
            public UInt32* jointParentIndices;
            public UInt32 floatCount;
            public byte** floatNames;
            public Avatar2.CAPI.ovrAvatar2Vector3f forwardDirection;
            public ovrAvatar2Prototype_Behavior_Pose defaultPose;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate bool ovrAvatar2Prototype_Behavior_PoseNodeCallback(ovrAvatar2EntityId entityId, ref ovrAvatar2Prototype_Behavior_Pose pose, in ovrAvatar2Prototype_Behavior_Hierarchy hierarchy, void* userContext);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Prototype_Behavior_RegisterPoseNodeFunc(ovrAvatar2StringView name, ovrAvatar2Prototype_Behavior_PoseNodeCallback callback, void* userContext);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Prototype_Behavior_UnregisterPoseNode(ovrAvatar2StringView name);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Prototype_Behavior_RegisterHierarchy(ovrAvatar2StringView name, in ovrAvatar2Prototype_Behavior_Hierarchy hierarchy, bool forceOverride);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Prototype_Behavior_UnregisterHierarchy(ovrAvatar2StringView name);
        public enum ovrAvatar2Prototype_BehaviorDebugQueryType : Int32
        {
            // Default, invalid value for query type. If valid type is not set, ovrAvatar2Prototype_Behavior_AddDebugQuery will fail.
            Invalid = 0,
            // Trace of all operations performed to construct the final output pose. Does not include full pose/mask data.
            PoseTrace = 1,
            // Trace of all currently active state machine states.
            StateTrace = 2,
        }

        public enum ovrAvatar2Prototype_BehaviorDebugQueryId : Int32
        {
            Invalid = 0,
        }

        public enum ovrAvatar2Prototype_BehaviorDebugQueryFlag : Int32
        {
            // whether the query should fire once and then be automatically removed
            FireOnce = 1 << 0,
            // whether to send debug output formatted for readability as a null-terminated string to logs
            // output is logged with Debug log level
            // output may be split into chunks according to max log length to avoid truncation
            LogOutput = 1 << 1,
            // whether to store debug output formatted for readability as a null-terminated string for retrieval with ovrAvatar2Prototype_Behavior_GetDebugQuery
            // not compatible with FireOnce
            StoreOutput = 1 << 2,
            // whether to call the provided callback with with debug output formatted for readability as a null-terminated string
            CallbackFormattedOutput = 1 << 3,
            // whether to call the provided callback with with binary-encoded debug output
            CallbackBinaryOutput = 1 << 4,
            // whether to send binary-encoded debug output via the debug server to any connected clients
            TransmitOutput = 1 << 5,
            None = 0,
            All = 0 | FireOnce | LogOutput | StoreOutput | CallbackFormattedOutput | CallbackBinaryOutput | TransmitOutput,
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void ovrAvatar2Prototype_Behavior_DebugQueryCallback(ovrAvatar2DataView queryOutput, void* userContext);
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2Prototype_BehaviorDebugQuery
        {
            public ovrAvatar2Prototype_BehaviorDebugQueryType type;
            public ovrAvatar2Prototype_BehaviorDebugQueryFlag flags;
            public ovrAvatar2LogLevel logLevel;
            public ovrAvatar2Prototype_Behavior_DebugQueryCallback callback;
            public void* callbackUserContext;
        }

        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Prototype_BehaviorDebugQuery ovrAvatar2Prototype_DefaultBehaviorDebugQuery();
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Prototype_Behavior_AddDebugQuery(ovrAvatar2EntityId entityId, in ovrAvatar2Prototype_BehaviorDebugQuery debugQuery, out ovrAvatar2Prototype_BehaviorDebugQueryId outDebugQueryId);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Prototype_Behavior_RemoveDebugQuery(ovrAvatar2EntityId entityId, ovrAvatar2Prototype_BehaviorDebugQueryId debugQueryId);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Prototype_Behavior_GetDebugQueryOutputSize(ovrAvatar2EntityId entityId, ovrAvatar2Prototype_BehaviorDebugQueryId debugQueryId, out ovrAvatar2SizeType queryOutputSize);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Prototype_Behavior_GetDebugQueryOutput(ovrAvatar2EntityId entityId, ovrAvatar2Prototype_BehaviorDebugQueryId debugQueryId, out ovrAvatar2DataBuffer queryOutput);
    }
}
