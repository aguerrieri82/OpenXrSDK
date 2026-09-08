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

namespace Oculus.Avatar2
{
    public static partial class CAPI
    {
        public const string AvatarCapiLogScope = "OvrAvatarAPI_Avatar";
        public const string ScriptBinaryMismatchResolution = "update c-sharp scripts to match libovravatar2 version";
        public const uint AVATAR_UPDATE_UNINITIALIZED_FRAME_COUNT = 0;
        public const float DefaultAvatarColorRed = (30 / 255.0f);
        public const float DefaultAvatarColorGreen = (157 / 255.0f);
        public const float DefaultAvatarColorBlue = (255 / 255.0f);
        public const uint DefaultNetworkWorkerUpdateFrequency = 256;
        public enum ovrAvatar2LogLevel : Int32
        {
            Unknown = 0,
            Default = 1,
            Verbose = 2,
            Debug = 3,
            Info = 4,
            Warn = 5,
            Failure = 6,
            Error = 7,
            Fatal = 8,
            Silent = 9,
        };
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void LoggingDelegate(ovrAvatar2LogLevel prio, byte* msg, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public unsafe delegate void LoggingViewDelegate(ovrAvatar2LogLevel prio, Experimental.CAPI.ovrAvatar2StringView msgView, void* context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MemAllocDelegate(UInt64 byteCount, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MemFreeDelegate(IntPtr buffer, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProfileMarkerDelegate(string name, bool isAsync);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProfileCounterDelegate(string name, Int64 value);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ResourceDelegate(in ovrAvatar2Asset_Resource resource, IntPtr context);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RequestDelegate(ovrAvatar2RequestId requestId, ovrAvatar2Result status, IntPtr userContext);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate IntPtr FileOpenDelegate(IntPtr context, string filename);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        // TODO: Change return type, bool is an unreliable return type for an unmanaged function pointer
        public delegate bool FileReadDelegate(IntPtr context, IntPtr fileHandle, out IntPtr fileDataPtr, out UInt64 fileSize);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        // TODO: Change return type, bool is an unreliable return type for an unmanaged function pointer
        public delegate bool FileCloseDelegate(IntPtr context, IntPtr fileHandle);
        public enum ovrAvatar2Platform : Int32
        {
            Invalid = 0,
            PC = 1,
            Quest = 2,
            Quest2 = 3,
            QuestPro = 4,
            Quest3 = 5,
            First = PC,
            Last = Quest3,
            Count = (Last - First) + 1,
            Num = Last + 1,
        }

        [Flags]
        public enum ovrAvatar2InitializeFlags : Int32
        {
            // When set, ovrAvatar2_Shutdown() may return ovrAvatar2Result_MemoryLeak to indicate a
            // detected memory leak
            CheckMemoryLeaks = 1 << 0,
            UseDefaultImage = 1 << 1,
            // When set, skinningOrigin in ovrAvatar2PrimitiveRenderState is set with the skinning origin
            // and the skinning matrices root will be the skinning Origin
            EnableSkinningOrigin = 1 << 3,
            // When set, additional implicit operations will be performed in order to facilitate ease-of-use in tooling scenarios
            // For example, automatically allowing hot-reloading of animations and more relaxed event registration requirements
            ToolMode = 1 << 4,
            // When set, an Entity's default status is Pending instead of Success.
            // It will go to Success when the RigResolver is ready.
            // NOTE: This behavior will change in the future such that the current effect of
            // `EntityPendingUntilRigResolverReady` will become the default, at which point this bit will be repurposed for other usage
            EntityPendingUntilRigResolverReady = 1 << 5,
            // When set, the runtime rig system will run with a full detailed hierarchy which is lees performant
            // though allows more detailed animations of skeleton extensions. Mostly intended for non VR scenarios.
            Experimental_EnableHighDetailRig = 1 << 8,
            None = 0,
            All = CheckMemoryLeaks | UseDefaultImage | EnableSkinningOrigin | ToolMode | EntityPendingUntilRigResolverReady | Experimental_EnableHighDetailRig,
            First = CheckMemoryLeaks,
            Last = Experimental_EnableHighDetailRig,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ovrAvatar2InitializeInfo
        {
            public FBVersionNumber versionNumber;
            public string clientVersion;
            public ovrAvatar2Platform platform;
            public ovrAvatar2InitializeFlags flags;
            public ovrAvatar2LogLevel loggingLevel;
            public readonly LoggingDelegate loggingCallback;
            public IntPtr loggingContext;
            public MemAllocDelegate memAllocCallback;
            public MemFreeDelegate memFreeCallback;
            public IntPtr memoryContext;
            public RequestDelegate requestCallback;
            public FileOpenDelegate fileOpenCallback;
            public FileReadDelegate fileReadCallback;
            public FileCloseDelegate fileCloseCallback;
            public IntPtr fileReaderContext;
            public ResourceDelegate resourceLoadCallback;
            public IntPtr resourceLoadContext;
            public string fallbackPathToOvrAvatar2AssetsZip;
            public UInt32 shouldCreateWorkerThreads;
            public Int64 maxNetworkRequests;
            public Int64 maxNetworkSendBytesPerSecond;
            public Int64 maxNetworkReceiveBytesPerSecond;
            public ovrAvatar2Vector3f defaultModelColor;
            public ovrAvatar2Vector3f clientSpaceRightAxis;
            public ovrAvatar2Vector3f clientSpaceUpAxis;
            public ovrAvatar2Vector3f clientSpaceForwardAxis;
            public string clientName;
            public UInt32 networkWorkerUpdateFrequency;
            public UInt32 reserved1;
            public IntPtr reserved2;
            public IntPtr reserved3;
            public IntPtr reserved4;
            public IntPtr reserved5;
            public IntPtr reserved6;
            public LoggingViewDelegate loggingViewCallback;
            public IntPtr reserved7;
            public Int64 reserved8;
            public IntPtr reserved9;
            public IntPtr reserved10;
            public IntPtr reserved11;
            public string extraJsonPayload;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_Initialize(in ovrAvatar2InitializeInfo infoPtr);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_Shutdown();
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2_UpdateAccessToken(byte* token);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_UpdateNetworkSettings(Int64 maxNetworkSendBytesPerSecond, Int64 maxNetworkReceiveBytesPerSecond, Int64 maxNetworkRequests);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_Update(float deltaSeconds);
        public const float AVATAR_UPDATE_SMALL_STEP = 0.1f;
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_RunTask();
        public enum ovrAvatar2JobCategoryFlags : Int32
        {
            // Jobs that are needed as part of the current frame.
            // Examples include updating animations and applying
            // network updates.
            ovrAvatar2JobCategoryFlags_FrameUpdate = 1 << 0,
            // Jobs that are used for incremental refinement of
            // the scene, but that don't have a strict deadline.
            // Examples include updating the LOD manager.
            ovrAvatar2JobCategoryFlags_Incremental = 1 << 1,
            // Background CPU-intensive tasks that have no deadline,
            // like parsing and setting up loaded assets.
            ovrAvatar2JobCategoryFlags_BackgroundCPU = 1 << 2,
            // Background IO-bound tasks that will leave threads
            // stalled or blocked. Examples include loading
            // files off of disk.
            ovrAvatar2JobCategoryFlags_BackgroundIO = 1 << 3,
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        unsafe public static extern ovrAvatar2Result ovrAvatar2_RunJobs(ovrAvatar2JobCategoryFlags jobCategories, bool sleepIfNoJobs, bool* pStopNow, float maxSeconds);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_WakeJobRunners(bool bForceReturn);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_HasAvatar(UInt64 userId, out ovrAvatar2RequestId requestId, IntPtr userContext);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_HasAvatarChanged(ovrAvatar2EntityId entityId, out ovrAvatar2RequestId requestId, IntPtr userContext);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_GetRequestBool(ovrAvatar2RequestId requestId, [MarshalAs(UnmanagedType.U1)] out bool result);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_AddZipSourceFile(string filename);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_RemoveZipSource(string filename);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2MemoryStats
        {
            public UInt64 currBytesUsed;
            public UInt64 currAllocationCount;
            public UInt64 maxBytesUsed;
            public UInt64 maxAllocationCount;
            public UInt64 totalBytesUsed;
            public UInt64 totalAllocationCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2NetworkStats
        {
            public UInt64 downloadTotalBytes;
            public UInt64 downloadSpeed;
            public UInt64 totalRequests;
            public UInt64 activeRequests;
        }

        public const int TaskHistogramSize = 32;
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2TaskStats
        {
            public fixed UInt32 histogram[TaskHistogramSize];
            public UInt32 pending;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2_GetResultString(ovrAvatar2Result result, char* buffer, UInt32* size);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2_GetVersionString(byte* versionBuffer, UInt32 bufferSize);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_QueryMemoryStats(out ovrAvatar2MemoryStats stats, UInt32 statsStructSize, out UInt32 bytesUpdated);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern ovrAvatar2Result ovrAvatar2_QueryNetworkStats(out ovrAvatar2NetworkStats stats, UInt32 statsStructSize, out UInt32 bytesUpdated);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2_QueryTaskStats(out ovrAvatar2TaskStats stats, UInt32 statsStructSize, out UInt32 bytesUpdated);
    }
}
