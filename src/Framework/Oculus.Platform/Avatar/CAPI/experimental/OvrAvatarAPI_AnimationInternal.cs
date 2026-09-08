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
    using EntityPtr = IntPtr;
    /* Pointer to pinned float[] */
    using FloatArrayPtr = IntPtr;
    using MixerLayerPtr = IntPtr;
    /* Pointer to pinned string[], [In] string[] is used instead */
    // using StringArrayPtr = IntPtr;
    using OvrAnimClipPtr = IntPtr;
    using OvrAnimHierarchyPtr = IntPtr;
    /* Pointer to pinned ovrAvatar2AnimationParameterId[]*/
    using ParameterIdArrayPtr = IntPtr;
    using ovrAvatar2Id = Avatar2.CAPI.ovrAvatar2Id;
    using ovrAvatar2Result = Avatar2.CAPI.ovrAvatar2Result;
    using ovrAvatar2EntityId = Avatar2.CAPI.ovrAvatar2EntityId;

    public static partial class CAPI
    {
        public enum ovrAvatar2Mood : Int32
        {
            Invalid = -1,
            Neutral = 0,
            Like,
            VeryLike,
            Happy,
            Confused,
            VeryConfused,
            Dislike,
            VeryDislike,
            Unhappy,
            Count
        }

        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Animation_SetMood(ovrAvatar2EntityId entityId, ovrAvatar2Mood desiredMood);
        [DllImport(Avatar2.CAPI.LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Animation_GetMood(ovrAvatar2EntityId entityId, out ovrAvatar2Mood currentMood);
    }
}
