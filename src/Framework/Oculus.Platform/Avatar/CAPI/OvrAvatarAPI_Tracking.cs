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
        [Flags]
        public enum ovrAvatar2Button : Int32
        {
            // X/A pressed
            One = 0x0001,
            // Y/B pressed
            Two = 0x0002,
            // Select/Oculus button pressed
            Three = 0x0004,
            // Joystick button pressed
            Joystick = 0x0008,
        }

        [Flags]
        public enum ovrAvatar2Touch : Int32
        {
            // Capacitive touch for X/A button
            One = 0x0001,
            // Capacitive touch for Y/B button
            Two = 0x0002,
            // Capacitive touch for thumbstick
            Joystick = 0x0004,
            // Capacitive touch for thumb rest
            ThumbRest = 0x0008,
            // Capacitive touch for index trigger
            Index = 0x0010,
            // Index finger is pointing
            Pointing = 0x0040,
            // Thumb is up
            ThumbUp = 0x0080,
        }

        public enum ovrAvatar2ControllerType : Int32
        {
            // Invalid or unknown controller
            Invalid = -1,
            //  Oculus Rift controller
            Rift = 0,
            // Oculus Touch controller
            Touch = 1,
            // Oculus Quest 2 controller
            Quest2 = 2,
            // Meta Quest Pro controller
            QuestPro = 3,
            // Meta Quest 3 controller
            Quest3 = 4,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2ControllerState
        {
            public ovrAvatar2Button buttonMask;
            public ovrAvatar2Touch touchMask;
            public float joystickX;
            public float joystickY;
            public float indexTrigger;
            public float handTrigger;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2InputControlState
        {
            public ovrAvatar2ControllerType type;
            public ovrAvatar2ControllerState leftControllerState;
            public ovrAvatar2ControllerState rightControllerState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2InputTrackingState
        {
            [MarshalAs(UnmanagedType.U1)]
            public bool headsetActive;
            [MarshalAs(UnmanagedType.U1)]
            public bool leftControllerActive;
            [MarshalAs(UnmanagedType.U1)]
            public bool rightControllerActive;
            [MarshalAs(UnmanagedType.U1)]
            public bool leftControllerVisible;
            [MarshalAs(UnmanagedType.U1)]
            public bool rightControllerVisible;
            public ovrAvatar2Transform headset;
            public ovrAvatar2Transform leftController;
            public ovrAvatar2Transform rightController;
        }

        public enum ovrAvatar2TrackingBodyModality : Int32
        {
            // Avatar modality unknown.
            Unknown = 0,
            // TODO: verify this is correct
            //  User is in a seated position.
            Sitting = 1,
            // TODO: verify this is correct
            //  User is in a standing position.
            Standing = 2,
        };
        public enum ovrAvatar2TrackingConfidence : Int32
        {
            // Low tracking confidence level
            Low = 0,
            // High tracking confidence level
            High = 0x3f800000,
        };
        public enum ovrAvatar2Space : Int32
        {
            // Local coordinates, with respect to parent joint
            Local = 0,
            // Object coordinates, with respect to the avatar entity
            Object = 1,
            // Coordinate space not known
            Unknown = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2Bone
        {
            public ovrAvatar2JointType boneId;
            public Int16 parentBoneIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe ref struct ovrAvatar2TrackingBodyPose
        {
            public readonly UInt32 numBones;
            public ovrAvatar2Space space;
            public readonly ovrAvatar2Transform* bones;
            public readonly float leftHandScale;
            public readonly float rightHandScale;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe ref struct ovrAvatar2TrackingBodySkeleton
        {
            public readonly UInt32 numBones;
            public ovrAvatar2Vector3f forwardDir;
            public readonly ovrAvatar2Bone* bones;
            public ovrAvatar2TrackingBodyPose referencePose;
        }

        public enum ovrAvatar2HandInputType : Int32
        {
            // Controller used to get hand position and orientation
            Controller = 0,
            // Headset sensors are used to get hand position and orientation
            Tracking = 1,
            // Custom hand tracking implementation
            Custom = 2,
            // Hand tracking input type unknown
            Unknown = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public ref struct ovrAvatar2TrackingBodyState
        {
            public ovrAvatar2InputTrackingState inputTrackingState;
            public ovrAvatar2InputControlState inputControlState;
            public ovrAvatar2HandInputType leftHandInputType;
            public ovrAvatar2HandInputType rightHandInputType;
            public Int32 skeletonVersion;
            public Int32 numBones;
            public ovrAvatar2TrackingBodyModality bodyModality;
            public float leftHandScale;
            public float rightHandScale;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool BodyStateCallback(out ovrAvatar2TrackingBodyState bodyState, IntPtr userContext);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool BodySkeletonCallback(ref ovrAvatar2TrackingBodySkeleton skeleton, IntPtr userContext);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool BodyPoseCallback(ref ovrAvatar2TrackingBodyPose pose, IntPtr userContext);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2TrackingDataContext
        {
            public IntPtr context;
            public BodyStateCallback bodyStateCallback;
            public BodySkeletonCallback bodySkeletonCallback;
            public BodyPoseCallback bodyPoseCallback;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2TrackingDataContextNative
        {
            public IntPtr context;
            public IntPtr bodyStateCallback;
            public IntPtr bodySkeletonCallback;
            public IntPtr bodyPoseCallback;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_SetBodyTrackingContext(ovrAvatar2EntityId entityId, in ovrAvatar2TrackingDataContext context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Tracking_SetBodyTrackingContext")]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_SetBodyTrackingContextNative(ovrAvatar2EntityId entityId, in ovrAvatar2TrackingDataContextNative context);
        public enum ovrAvatar2Viseme : Int32
        {
            // Silent viseme
            sil = 0,
            // PP viseme (corresponds to p,b,m phonemes in worlds like \a put , \a bat, \a mat)
            PP = 1,
            // FF viseme (corrseponds to f,v phonemes in the worlds like \a fat, \a vat)
            FF = 2,
            // TH viseme (corresponds to th phoneme in words like \a think, \a that)
            TH = 3,
            // DD viseme (corresponds to t,d phonemes in words like \a tip or \a doll)
            DD = 4,
            // kk viseme (corresponds to k,g phonemes in words like \a call or \a gas)
            kk = 5,
            // CH viseme (corresponds to tS,dZ,S phonemes in words like \a chair, \a join, \a she)
            CH = 6,
            // SS viseme (corresponds to s,z phonemes in words like \a sir or \a zeal)
            SS = 7,
            // nn viseme (corresponds to n,l phonemes in worlds like \a lot or \a not)
            nn = 8,
            // RR viseme (corresponds to r phoneme in worlds like \a red)
            RR = 9,
            // aa viseme (corresponds to A: phoneme in worlds like \a car)
            aa = 10,
            // E viseme (corresponds to e phoneme in worlds like \a bed)
            E = 11,
            // I viseme (corresponds to ih phoneme in worlds like \a tip)
            ih = 12,
            // O viseme (corresponds to oh phoneme in worlds like \a toe)
            oh = 13,
            // U viseme (corresponds to ou phoneme in worlds like \a book)
            ou = 14,
            // Total number of visemes
            Count = 15
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ovrAvatar2LipSyncState
        {
            public fixed float visemes[(int)ovrAvatar2Viseme.Count];
            public float laughterScore;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool LipSyncCallback(out ovrAvatar2LipSyncState lipSyncState, IntPtr userContext);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LipSyncContext
        {
            public IntPtr context;
            public LipSyncCallback lipSyncCallback;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2LipSyncContextNative
        {
            public IntPtr context;
            public IntPtr lipSyncCallback;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_SetLipSyncContext(ovrAvatar2EntityId entityId, in ovrAvatar2LipSyncContext context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Tracking_SetLipSyncContext")]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_SetLipSyncContextNative(ovrAvatar2EntityId entityId, in ovrAvatar2LipSyncContextNative context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_GetVisemes(ovrAvatar2EntityId entityId, Int32 numVisemeValues, IntPtr visemeValues);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_GetPose(ovrAvatar2EntityId entityId, out ovrAvatar2Pose outPose);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Tracking_GetPoseValid(ovrAvatar2EntityId entityId, [MarshalAs(UnmanagedType.U1)] out bool isValid);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern unsafe ovrAvatar2Result ovrAvatar2Tracking_GetNameAtIndex(ovrAvatar2EntityId entityId, int index, byte* nameBuffer, UInt32 bufferSize);
        public enum ovrAvatar2FaceExpression : Int32
        {
            BrowLowererL = 0,
            BrowLowererR = 1,
            CheekPuffL = 2,
            CheekPuffR = 3,
            CheekRaiserL = 4,
            CheekRaiserR = 5,
            CheekSuckL = 6,
            CheekSuckR = 7,
            ChinRaiserL = 8,
            ChinRaiserR = 9,
            DimplerL = 10,
            DimplerR = 11,
            EyesClosedL = 12,
            EyesClosedR = 13,
            EyesLookDownL = 14,
            EyesLookDownR = 15,
            EyesLookLeftL = 16,
            EyesLookLeftR = 17,
            EyesLookRightL = 18,
            EyesLookRightR = 19,
            EyesLookUpL = 20,
            EyesLookUpR = 21,
            InnerBrowRaiserL = 22,
            InnerBrowRaiserR = 23,
            JawDrop = 24,
            JawSidewaysLeft = 25,
            JawSidewaysRight = 26,
            JawThrust = 27,
            LidTightenerL = 28,
            LidTightenerR = 29,
            LipCornerDepressorL = 30,
            LipCornerDepressorR = 31,
            LipCornerPullerL = 32,
            LipCornerPullerR = 33,
            LipFunnelerLB = 34,
            LipFunnelerLT = 35,
            LipFunnelerRB = 36,
            LipFunnelerRT = 37,
            LipPressorL = 38,
            LipPressorR = 39,
            LipPuckerL = 40,
            LipPuckerR = 41,
            LipStretcherL = 42,
            LipStretcherR = 43,
            LipSuckLB = 44,
            LipSuckLT = 45,
            LipSuckRB = 46,
            LipSuckRT = 47,
            LipTightenerL = 48,
            LipTightenerR = 49,
            LipsTowardLB = 50,
            LipsTowardLT = 51,
            LipsTowardRB = 52,
            LipsTowardRT = 53,
            LowerLipDepressorL = 54,
            LowerLipDepressorR = 55,
            MouthLeft = 56,
            MouthRight = 57,
            NasiolabialFurrowL = 58,
            NasiolabialFurrowR = 59,
            NoseWrinklerL = 60,
            NoseWrinklerR = 61,
            NostrilCompressorL = 62,
            NostrilCompressorR = 63,
            NostrilDilatorL = 64,
            NostrilDilatorR = 65,
            OuterBrowRaiserL = 66,
            OuterBrowRaiserR = 67,
            UpperLidRaiserL = 68,
            UpperLidRaiserR = 69,
            UpperLipRaiserL = 70,
            UpperLipRaiserR = 71,
            Count = 72
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2FacePose
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)ovrAvatar2FaceExpression.Count)]
            public float[] expressionWeights;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)ovrAvatar2FaceExpression.Count)]
            public float[] expressionConfidence;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool FacePoseCallback(out ovrAvatar2FacePose facePose, IntPtr userContext);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2FacePoseProvider
        {
            public IntPtr provider;
            public FacePoseCallback facePoseCallback;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2FacePoseProviderNative
        {
            public IntPtr provider;
            public IntPtr facePoseCallback;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetFacePoseProvider(ovrAvatar2EntityId entityId, in ovrAvatar2FacePoseProvider provider);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_SetFacePoseProvider")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetFacePoseProviderNative(ovrAvatar2EntityId entityId, in ovrAvatar2FacePoseProviderNative provider);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2EyePose
        {
            public ovrAvatar2Quatf orientation;
            public ovrAvatar2Vector3f position;
            [MarshalAs(UnmanagedType.U1)]
            public bool isValid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2EyesPose
        {
            public ovrAvatar2EyePose leftEye;
            public ovrAvatar2EyePose rightEye;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public delegate bool EyePoseCallback(out ovrAvatar2EyesPose eyePose, IntPtr userContext);
        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2EyePoseProvider
        {
            public IntPtr provider;
            public EyePoseCallback eyePoseCallback;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ovrAvatar2EyePoseProviderNative
        {
            public IntPtr provider;
            public IntPtr eyePoseCallback;
        }

        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl)]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetEyePoseProvider(ovrAvatar2EntityId entityId, in ovrAvatar2EyePoseProvider context);
        [DllImport(LibFile, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrAvatar2Input_SetEyePoseProvider")]
        public static extern ovrAvatar2Result ovrAvatar2Input_SetEyePoseProviderNative(ovrAvatar2EntityId entityId, in ovrAvatar2EyePoseProviderNative context);
    }
}
