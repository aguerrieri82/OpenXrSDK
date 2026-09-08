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

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_Prototype_SetInputControlContext")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetInputControlContext(ovrAvatar2EntityId entityId, in ovrAvatar2InputControlContext context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_Prototype_SetInputControlContext")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetInputControlContextNative(ovrAvatar2EntityId entityId, in ovrAvatar2InputControlContextNative context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetInputTrackingContext(ovrAvatar2EntityId entityId, in ovrAvatar2InputTrackingContext context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_SetInputTrackingContext")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetInputTrackingContextNative(ovrAvatar2EntityId entityId, in ovrAvatar2InputTrackingContextNative context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetHandTrackingContext(ovrAvatar2EntityId entityId, in ovrAvatar2HandTrackingDataContext context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_SetHandTrackingContext")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetHandTrackingContextNative(ovrAvatar2EntityId entityId, in ovrAvatar2HandTrackingDataContextNative context);
    }
}
