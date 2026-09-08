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
using Oculus.Avatar2.Experimental;
using static Oculus.Avatar2.Experimental.CAPI;

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        public enum ovrAvatar2Graph : Int32
        {
            Invalid = 0,
            Meta = 1,
            Oculus = 2,
            First = Meta,
            Last = Oculus,
            Count = (Last - First) + 1,
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_GraphHasAvatar(UInt64 userID, ovrAvatar2Graph graph, out ovrAvatar2RequestId requestId, IntPtr userContext);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Entity_Prototype_LoadUserWithConfigsFromGraph(ovrAvatar2EntityId entityId, UInt64 userID, ovrAvatar2StringView configIDs, ovrAvatar2Graph graph, ovrAvatar2EntityLoadSettings loadSettings, UInt32 settingsStructSize, out ovrAvatar2LoadRequestId loadRequestId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Entity_LoadUserFromGraph(ovrAvatar2EntityId entityId, UInt64 userID, ovrAvatar2Graph graph, ovrAvatar2EntityLoadSettings loadSettings, out ovrAvatar2LoadRequestId loadRequestId);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2_UpdateAccessTokenForGraph(byte* token, ovrAvatar2Graph graph);
    }
}
