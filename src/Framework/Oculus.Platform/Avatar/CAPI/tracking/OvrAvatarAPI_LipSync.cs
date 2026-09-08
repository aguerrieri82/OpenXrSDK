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
        public enum ovrAvatar2LipSyncMode : Int32
        {
            Original = 0,
            Enhanced = 1,
            EnhancedWithLaughter = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LipSyncProviderConfig
        {
            public ovrAvatar2LipSyncMode mode;
            public UInt32 audioSampleRate;
            public UInt32 audioBufferSize;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_CreateProvider(ref ovrAvatar2LipSyncProviderConfig config, ref IntPtr lipsyncProvider);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_ReconfigureProvider(IntPtr lipsyncProvider, ref ovrAvatar2LipSyncProviderConfig config);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_DestroyProvider(IntPtr lipsyncProvider);
        public enum ovrAvatar2AudioDataFormat : Int32
        {
            S16_Mono, // Signed 16-bit integer mono audio stream
            S16_Stereo, // Signed 16-bit integer stereo audio stream
            F32_Mono, // Signed 32-bit float mono data type
            F32_Stereo, // Signed 32-bit float stereo data type
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_FeedAudio(IntPtr lipsyncProvider, ovrAvatar2AudioDataFormat format, IntPtr data, UInt32 numSamples);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_SetLaughter(IntPtr lipsyncProvider, Int32 amount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_SetSmoothing(IntPtr lipsyncProvider, Int32 smoothing);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_EnableViseme(IntPtr lipsyncProvider, ovrAvatar2Viseme viseme);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_DisableViseme(IntPtr lipsyncProvider, ovrAvatar2Viseme viseme);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_SetViseme(IntPtr lipsyncProvider, ovrAvatar2Viseme viseme, Int32 amount);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_InitializeContext(IntPtr lipsyncProvider, ref ovrAvatar2LipSyncContext lipSyncContext);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2LipSync_InitializeContext")]
        public static extern ovrAvatar2Result ovrAvatar2LipSync_InitializeContextNative(IntPtr lipsyncProvider, out ovrAvatar2LipSyncContextNative lipSyncContext);
    }
}
