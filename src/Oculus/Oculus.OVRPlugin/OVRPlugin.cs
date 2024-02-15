#pragma warning disable CS8618
#pragma warning disable CS8603
#pragma warning disable CS8765

using Oculus;
using System.Runtime.InteropServices;


public static partial class OVRPlugin
{
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
    public const bool isSupportedPlatform = false;
#else
    public const bool isSupportedPlatform = true;
#endif

#if OVRPLUGIN_UNSUPPORTED_PLATFORM
    public static readonly System.Version wrapperVersion = _versionZero;
#else
    public static readonly System.Version wrapperVersion = OVRP_1_92_0.version;
#endif

#if !OVRPLUGIN_UNSUPPORTED_PLATFORM
    private static System.Version _version;
#endif
    public static System.Version version
    {
        get
        {
#if OVRPLUGIN_EDITOR_MOCK_ENABLED
            return wrapperVersion;
#elif OVRPLUGIN_UNSUPPORTED_PLATFORM
            Debug.LogWarning("Platform is not currently supported by OVRPlugin");
            return _versionZero;
#else
            if (_version == null)
            {
                try
                {
                    string pluginVersion = OVRP_1_1_0.ovrp_GetVersion();

                    if (pluginVersion != null)
                    {
                        // Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
                        pluginVersion = pluginVersion.Split('-')[0];
                        _version = new System.Version(pluginVersion);
                    }
                    else
                    {
                        _version = _versionZero;
                    }
                }
                catch
                {
                    _version = _versionZero;
                }

                // Unity 5.1.1f3-p3 have OVRPlugin version "0.5.0", which isn't accurate.
                if (_version == OVRP_0_5_0.version)
                    _version = OVRP_0_1_0.version;

                if (_version > _versionZero && _version < OVRP_1_3_0.version)
                    throw new PlatformNotSupportedException(
                        "Oculus Utilities version " + wrapperVersion + " is too new for OVRPlugin version " +
                        _version.ToString() + ". Update to the latest version of Unity.");
            }

            return _version;
#endif
        }
    }

#if !OVRPLUGIN_UNSUPPORTED_PLATFORM
    private static System.Version _nativeSDKVersion;
#endif
    public static System.Version nativeSDKVersion
    {
        get
        {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
            return _versionZero;
#else
            if (_nativeSDKVersion == null)
            {
                try
                {
                    string sdkVersion = string.Empty;

                    if (version >= OVRP_1_1_0.version)
                        sdkVersion = OVRP_1_1_0.ovrp_GetNativeSDKVersion();
                    else
                        sdkVersion = _versionZero.ToString();

                    if (sdkVersion != null)
                    {
                        // Truncate unsupported trailing version info for System.Version. Original string is returned if not present.
                        sdkVersion = sdkVersion.Split('-')[0];
                        _nativeSDKVersion = new System.Version(sdkVersion);
                    }
                    else
                    {
                        _nativeSDKVersion = _versionZero;
                    }
                }
                catch
                {
                    _nativeSDKVersion = _versionZero;
                }
            }

            return _nativeSDKVersion;
#endif
        }
    }

    public static class Qpl
    {
        public enum ResultType : short
        {
            Success = 2,
            Fail = 3,
            Cancel = 4
        }
    }

    public static class Media
    {
        public enum MrcActivationMode
        {
            Automatic = 0,
            Disabled = 1,
            EnumSize = 0x7fffffff
        }

        public enum PlatformCameraMode
        {
            Disabled = -1,
            Initialized = 0,
            UserControlled = 1,
            SmartNavigated = 2,
            StabilizedPoV = 3,
            RemoteDroneControlled = 4,
            RemoteSpatialMapped = 5,
            SpectatorMode = 6,
            MobileMRC = 7,
            EnumSize = 0x7fffffff
        }

        public enum InputVideoBufferType
        {
            Memory = 0,
            TextureHandle = 1,
            EnumSize = 0x7fffffff
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4
    {
        public float m00;

        public float m10;

        public float m20;

        public float m30;

        public float m01;

        public float m11;

        public float m21;

        public float m31;

        public float m02;

        public float m12;

        public float m22;

        public float m32;

        public float m03;

        public float m13;

        public float m23;

        public float m33;

    }


    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceContainerInternal
    {
        public int uuidCapacityInput;
        public int uuidCountOutput;
        public IntPtr uuids;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceSemanticLabelInternal
    {
        public int byteCapacityInput;
        public int byteCountOutput;
        public IntPtr labels;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomLayout
    {
        public Guid floorUuid;
        public Guid ceilingUuid;
        public Guid[] wallUuids;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RoomLayoutInternal
    {
        public Guid floorUuid;
        public Guid ceilingUuid;
        public int wallUuidCapacityInput;
        public int wallUuidCountOutput;
        public IntPtr wallUuids;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PolygonalBoundary2DInternal
    {
        public int vertexCapacityInput;
        public int vertexCountOutput;
        public IntPtr vertices;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SceneCaptureRequestInternal
    {
        public int requestByteCount;

        [MarshalAs(UnmanagedType.LPStr)]
        public string request;
    }

    public struct PinnedArray<T> : IDisposable where T : unmanaged
    {
        GCHandle _handle;
        public PinnedArray(T[] array) => _handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        public void Dispose() => _handle.Free();
        public static implicit operator IntPtr(PinnedArray<T> pinnedArray) => pinnedArray._handle.AddrOfPinnedObject();
    }

    [StructLayout(LayoutKind.Sequential)]
    private class GUID
    {
        public int a;
        public short b;
        public short c;
        public byte d0;
        public byte d1;
        public byte d2;
        public byte d3;
        public byte d4;
        public byte d5;
        public byte d6;
        public byte d7;
    }

    public enum Bool
    {
        False = 0,
        True
    }

    public enum Result
    {
        /// Success
        Success = 0,
        Success_EventUnavailable = 1,
        Success_Pending = 2,

        /// Failure
        Failure = -1000,
        Failure_InvalidParameter = -1001,
        Failure_NotInitialized = -1002,
        Failure_InvalidOperation = -1003,
        Failure_Unsupported = -1004,
        Failure_NotYetImplemented = -1005,
        Failure_OperationFailed = -1006,
        Failure_InsufficientSize = -1007,
        Failure_DataIsInvalid = -1008,
        Failure_DeprecatedOperation = -1009,
        Failure_ErrorLimitReached = -1010,
        Failure_ErrorInitializationFailed = -1011,
        Failure_RuntimeUnavailable = -1012,
        Failure_HandleInvalid = -1013,

        /// Space error cases
        Failure_SpaceCloudStorageDisabled = -2000,
        Failure_SpaceMappingInsufficient = -2001,
        Failure_SpaceLocalizationFailed = -2002,
        Failure_SpaceNetworkTimeout = -2003,
        Failure_SpaceNetworkRequestFailed = -2004,


    }

    public static bool IsSuccess(this Result result) => result >= 0;

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Error = 2
    }

    public delegate void LogCallback2DelegateType(LogLevel logLevel, IntPtr message, int size);

    public static void SetLogCallback2(LogCallback2DelegateType logCallback)
    {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM
        // do nothing
#else
        if (version >= OVRP_1_70_0.version)
        {
            Result result = OVRP_1_70_0.ovrp_SetLogCallback2(logCallback);
            if (result != Result.Success)
            {
                Debug.LogWarning("OVRPlugin.SetLogCallback2() failed");
            }
        }
#endif
    }


    public enum CameraStatus
    {
        CameraStatus_None,
        CameraStatus_Connected,
        CameraStatus_Calibrating,
        CameraStatus_CalibrationFailed,
        CameraStatus_Calibrated,
        CameraStatus_ThirdPerson,
        CameraStatus_EnumSize = 0x7fffffff
    }

    public enum CameraAnchorType
    {
        CameraAnchorType_PreDefined = 0,
        CameraAnchorType_Custom = 1,
        CameraAnchorType_Count,
        CameraAnchorType_EnumSize = 0x7fffffff
    }

    public enum XrApi
    {
        Unknown = 0,
        CAPI = 1,
        VRAPI = 2,
        OpenXR = 3,
        EnumSize = 0x7fffffff
    }

    public enum Eye
    {
        None = -1,
        Left = 0,
        Right = 1,
        Count = 2
    }


    public enum Tracker
    {
        None = -1,
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Count,
    }

    public enum Node
    {
        None = -1,
        EyeLeft = 0,
        EyeRight = 1,
        EyeCenter = 2,
        HandLeft = 3,
        HandRight = 4,
        TrackerZero = 5,
        TrackerOne = 6,
        TrackerTwo = 7,
        TrackerThree = 8,
        Head = 9,
        DeviceObjectZero = 10,
        TrackedKeyboard = 11,
        ControllerLeft = 12,
        ControllerRight = 13,
        Count,
    }

    public enum Controller
    {
        None = 0,
        LTouch = 0x00000001,
        RTouch = 0x00000002,
        Touch = LTouch | RTouch,
        Remote = 0x00000004,
        Gamepad = 0x00000010,
        LHand = 0x00000020,
        RHand = 0x00000040,
        Hands = LHand | RHand,
        Active = unchecked((int)0x80000000),
        All = ~None,
    }

    public enum InteractionProfile
    {
        None = 0,
        Touch = 1,
        TouchPro = 2,
        TouchPlus = 4,
    }

    public enum Handedness
    {
        Unsupported = 0,
        LeftHanded = 1,
        RightHanded = 2,
    }

    public enum TrackingOrigin
    {
        EyeLevel = 0,
        FloorLevel = 1,
        Stage = 2,
        View = 4,
        Count,
    }

    public enum RecenterFlags
    {
        Default = 0,
        IgnoreAll = unchecked((int)0x80000000),
        Count,
    }

    public enum BatteryStatus
    {
        Charging = 0,
        Discharging,
        Full,
        NotCharging,
        Unknown,
    }

    public enum EyeTextureFormat
    {
        Default = 0,
        R8G8B8A8_sRGB = 0,
        R8G8B8A8 = 1,
        R16G16B16A16_FP = 2,
        R11G11B10_FP = 3,
        B8G8R8A8_sRGB = 4,
        B8G8R8A8 = 5,
        R5G6B5 = 11,
        EnumSize = 0x7fffffff
    }

    public enum PlatformUI
    {
        None = -1,
        ConfirmQuit = 1,
        GlobalMenuTutorial, // Deprecated
    }

    public enum SystemRegion
    {
        Unspecified = 0,
        Japan,
        China,
    }

    public enum SystemHeadset
    {
        None = 0,

        // Standalone headsets
        Oculus_Quest = 8,
        Oculus_Quest_2 = 9,
        Meta_Quest_Pro = 10,
        Meta_Quest_3 = 11,
        Placeholder_12,
        Placeholder_13,
        Placeholder_14,

        // PC headsets
        Rift_DK1 = 0x1000,
        Rift_DK2,
        Rift_CV1,
        Rift_CB,
        Rift_S,
        Oculus_Link_Quest,
        Oculus_Link_Quest_2,
        Meta_Link_Quest_Pro,
        Meta_Link_Quest_3,
        PC_Placeholder_4105,
        PC_Placeholder_4106,
        PC_Placeholder_4107
    }

    public enum OverlayShape
    {
        Quad = 0,
        Cylinder = 1,
        Cubemap = 2,
        OffcenterCubemap = 4,
        Equirect = 5,
        ReconstructionPassthrough = 7,
        SurfaceProjectedPassthrough = 8,
        Fisheye = 9,
        KeyboardHandsPassthrough = 10,
        KeyboardMaskedHandsPassthrough = 11,
    }

    public enum LayerSuperSamplingType
    {
        None = 0,
        Normal = 1 << 12,
        Quality = 1 << 8,
    }

    public enum LayerSharpenType
    {
        None = 0,
        Normal = 1 << 13,
        Quality = 1 << 16,
    }

    public static bool IsPassthroughShape(OverlayShape shape)
    {
        return shape == OverlayShape.ReconstructionPassthrough
               || shape == OverlayShape.KeyboardHandsPassthrough
               || shape == OverlayShape.KeyboardMaskedHandsPassthrough
               || shape == OverlayShape.SurfaceProjectedPassthrough;
    }

    public enum Step
    {
        Render = -1,
        Physics = 0, // will be deprecated when using OpenXR
    }

    public enum CameraDevice
    {
        None = 0,
        WebCamera0 = 100,
        WebCamera1 = 101,
        ZEDCamera = 300,
    }

    public enum CameraDeviceDepthSensingMode
    {
        Standard = 0,
        Fill = 1,
    }

    public enum CameraDeviceDepthQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    public enum FoveatedRenderingLevel
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        HighTop = 4,
        EnumSize = 0x7FFFFFFF
    }

    [Obsolete("Please use FoveatedRenderingLevel instead", false)]
    public enum FixedFoveatedRenderingLevel
    {
        Off = 0,
        Low = FoveatedRenderingLevel.Low,
        Medium = FoveatedRenderingLevel.Medium,
        High = FoveatedRenderingLevel.High,

        // High foveation setting with more detail toward the bottom of the view and more foveation near the top (Same as High on Oculus Go)
        HighTop = FoveatedRenderingLevel.HighTop,
        EnumSize = 0x7FFFFFFF
    }

    [Obsolete("Please use FixedFoveatedRenderingLevel instead", false)]
    public enum TiledMultiResLevel
    {
        Off = 0,
        LMSLow = FixedFoveatedRenderingLevel.Low,
        LMSMedium = FixedFoveatedRenderingLevel.Medium,
        LMSHigh = FixedFoveatedRenderingLevel.High,

        // High foveation setting with more detail toward the bottom of the view and more foveation near the top (Same as High on Oculus Go)
        LMSHighTop = FixedFoveatedRenderingLevel.HighTop,
        EnumSize = 0x7FFFFFFF
    }

    public static int MAX_CPU_CORES = 8;

    public enum PerfMetrics
    {
        App_CpuTime_Float = 0,
        App_GpuTime_Float = 1,

        Compositor_CpuTime_Float = 3,
        Compositor_GpuTime_Float = 4,
        Compositor_DroppedFrameCount_Int = 5,

        System_GpuUtilPercentage_Float = 7,
        System_CpuUtilAveragePercentage_Float = 8,
        System_CpuUtilWorstPercentage_Float = 9,

        // Added 1.32.0
        Device_CpuClockFrequencyInMHz_Float = 10, // Deprecated 1.68.0
        Device_GpuClockFrequencyInMHz_Float = 11, // Deprecated 1.68.0
        Device_CpuClockLevel_Int = 12, // Deprecated 1.68.0
        Device_GpuClockLevel_Int = 13, // Deprecated 1.68.0

        Compositor_SpaceWarp_Mode_Int = 14,

        Device_CpuCore0UtilPercentage_Float = 32,
        Device_CpuCore1UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 1,
        Device_CpuCore2UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 2,
        Device_CpuCore3UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 3,
        Device_CpuCore4UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 4,
        Device_CpuCore5UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 5,
        Device_CpuCore6UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 6,
        Device_CpuCore7UtilPercentage_Float = Device_CpuCore0UtilPercentage_Float + 7,
        // Enum value 32~63 are reserved for CPU Cores' utilization (assuming at most 32 cores).

        Count,
        EnumSize = 0x7FFFFFFF
    }

    public enum ProcessorPerformanceLevel
    {
        PowerSavings = 0,
        SustainedLow = 1,
        SustainedHigh = 2,
        Boost = 3,
        EnumSize = 0x7FFFFFFF
    }

    public enum FeatureType
    {
        HandTracking = 0,
        KeyboardTracking = 1,
        EyeTracking = 2,
        FaceTracking = 3,
        BodyTracking = 4,
        Passthrough = 5,
        GazeBasedFoveatedRendering = 6,
        Count,
        EnumSize = 0x7FFFFFFF
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleMeshInternal
    {
        public int vertexCapacityInput;
        public int vertexCountOutput;
        public IntPtr vertices;
        public int indexCapacityInput;
        public int indexCountOutput;
        public IntPtr indices;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct CameraDeviceIntrinsicsParameters
    {
        // Focal length in pixels along x axis.
        readonly float fx;

        // Focal length in pixels along y axis.
        readonly float fy;

        // Optical center along x axis, defined in pixels (usually close to width/2).
        readonly float cx;

        // Optical center along y axis, defined in pixels (usually close to height/2).
        readonly float cy;

        // Distortion factor : [ k1, k2, p1, p2, k3 ]. Radial (k1,k2,k3) and Tangential (p1,p2) distortion.
        readonly double disto0;

        readonly double disto1;
        readonly double disto2;
        readonly double disto3;
        readonly double disto4;

        // Vertical field of view after stereo rectification, in degrees.
        readonly float v_fov;

        // Horizontal field of view after stereo rectification, in degrees.
        readonly float h_fov;

        // Diagonal field of view after stereo rectification, in degrees.
        readonly float d_fov;

        // Resolution width
        readonly int w;

        // Resolution height
        readonly int h;
    }

    private const int OverlayShapeFlagShift = 4;

    private enum OverlayFlag
    {
        None = unchecked((int)0x00000000),
        OnTop = unchecked((int)0x00000001),
        HeadLocked = unchecked((int)0x00000002),
        NoDepth = unchecked((int)0x00000004),
        ExpensiveSuperSample = unchecked((int)0x00000008),
        EfficientSuperSample = unchecked((int)0x00000010),
        EfficientSharpen = unchecked((int)0x00000020),
        BicubicFiltering = unchecked((int)0x00000040),
        ExpensiveSharpen = unchecked((int)0x00000080),
        SecureContent = unchecked((int)0x00000100),

        // Using the 5-8 bits for shapes, total 16 potential shapes can be supported 0x000000[0]0 ->  0x000000[F]0
        ShapeFlag_Quad = unchecked((int)OverlayShape.Quad << OverlayShapeFlagShift),
        ShapeFlag_Cylinder = unchecked((int)OverlayShape.Cylinder << OverlayShapeFlagShift),
        ShapeFlag_Cubemap = unchecked((int)OverlayShape.Cubemap << OverlayShapeFlagShift),
        ShapeFlag_OffcenterCubemap = unchecked((int)OverlayShape.OffcenterCubemap << OverlayShapeFlagShift),
        ShapeFlagRangeMask = unchecked((int)0xF << OverlayShapeFlagShift),

        Hidden = unchecked((int)0x000000200),

        AutoFiltering = unchecked((int)0x00000400),
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2f
    {
        public float x;
        public float y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3f
    {
        public float x;
        public float y;
        public float z;
        public static readonly Vector3f zero = new Vector3f { x = 0.0f, y = 0.0f, z = 0.0f };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}", x, y, z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4f
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public static readonly Vector4f zero = new Vector4f { x = 0.0f, y = 0.0f, z = 0.0f, w = 0.0f };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", x, y, z, w);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4s
    {
        public short x;
        public short y;
        public short z;
        public short w;
        public static readonly Vector4s zero = new Vector4s { x = 0, y = 0, z = 0, w = 0 };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", x, y, z, w);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Quatf
    {
        public float x;
        public float y;
        public float z;
        public float w;
        public static readonly Quatf identity = new Quatf { x = 0.0f, y = 0.0f, z = 0.0f, w = 1.0f };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", x, y, z, w);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Posef
    {
        public Quatf Orientation;
        public Vector3f Position;
        public static readonly Posef identity = new Posef { Orientation = Quatf.identity, Position = Vector3f.zero };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Position ({0}), Orientation({1})",
                Position, Orientation);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TextureRectMatrixf
    {
        public Rectf leftRect;
        public Rectf rightRect;
        public Vector4f leftScaleBias;
        public Vector4f rightScaleBias;

        public static readonly TextureRectMatrixf zero = new TextureRectMatrixf
        {
            leftRect = new Rectf { Size = new Sizef { w = 1, h = 1 } },
            rightRect = new Rectf { Size = new Sizef { w = 1, h = 1 } },
            leftScaleBias = new Vector4f { x = 1, y = 1 },
            rightScaleBias = new Vector4f { x = 1, y = 1 },
        };

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Rect Left ({0}), Rect Right({1}), Scale Bias Left ({2}), Scale Bias Right({3})", leftRect, rightRect,
                leftScaleBias, rightScaleBias);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PoseStatef
    {
        public Posef Pose;
        public Vector3f Velocity;
        [System.Obsolete("Deprecated. Acceleration is not supported in OpenXR", false)]
        public Vector3f Acceleration;
        public Vector3f AngularVelocity;
        [System.Obsolete("Deprecated. Acceleration is not supported in OpenXR", false)]
        public Vector3f AngularAcceleration;
        public double Time;

        public static readonly PoseStatef identity = new PoseStatef
        {
            Pose = Posef.identity,
            Velocity = Vector3f.zero,
            AngularVelocity = Vector3f.zero,
#pragma warning disable CS0618 // Type or member is obsolete
            Acceleration = Vector3f.zero,
            AngularAcceleration = Vector3f.zero
#pragma warning restore CS0618 // Type or member is obsolete
        };
    }

    public enum HapticsLocation
    {
        None = 0x00,
        Hand = 0x01,
        Thumb = 0x02,
        Index = 0x04,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ControllerState6
    {
        public uint ConnectedControllers;
        public uint Buttons;
        public uint Touches;
        public uint NearTouches;
        public float LIndexTrigger;
        public float RIndexTrigger;
        public float LHandTrigger;
        public float RHandTrigger;
        public Vector2f LThumbstick;
        public Vector2f RThumbstick;
        public Vector2f LTouchpad;
        public Vector2f RTouchpad;
        public byte LBatteryPercentRemaining;
        public byte RBatteryPercentRemaining;
        public byte LRecenterCount;
        public byte RRecenterCount;
        public float LThumbRestForce;
        public float RThumbRestForce;
        public float LStylusForce;
        public float RStylusForce;
        public float LIndexTriggerCurl;
        public float RIndexTriggerCurl;
        public float LIndexTriggerSlide;
        public float RIndexTriggerSlide;
        public float LIndexTriggerForce;
        public float RIndexTriggerForce;

        public ControllerState6(ControllerState5 cs)
        {
            ConnectedControllers = cs.ConnectedControllers;
            Buttons = cs.Buttons;
            Touches = cs.Touches;
            NearTouches = cs.NearTouches;
            LIndexTrigger = cs.LIndexTrigger;
            RIndexTrigger = cs.RIndexTrigger;
            LHandTrigger = cs.LHandTrigger;
            RHandTrigger = cs.RHandTrigger;
            LThumbstick = cs.LThumbstick;
            RThumbstick = cs.RThumbstick;
            LTouchpad = cs.LTouchpad;
            RTouchpad = cs.RTouchpad;
            LBatteryPercentRemaining = cs.LBatteryPercentRemaining;
            RBatteryPercentRemaining = cs.RBatteryPercentRemaining;
            LRecenterCount = cs.LRecenterCount;
            RRecenterCount = cs.RRecenterCount;
            LThumbRestForce = cs.LThumbRestForce;
            RThumbRestForce = cs.RThumbRestForce;
            LStylusForce = cs.LStylusForce;
            RStylusForce = cs.RStylusForce;
            LIndexTriggerCurl = cs.LIndexTriggerCurl;
            RIndexTriggerCurl = cs.RIndexTriggerCurl;
            LIndexTriggerSlide = cs.LIndexTriggerSlide;
            RIndexTriggerSlide = cs.RIndexTriggerSlide;
            LIndexTriggerForce = 0.0f;
            RIndexTriggerForce = 0.0f;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ControllerState5
    {
        public uint ConnectedControllers;
        public uint Buttons;
        public uint Touches;
        public uint NearTouches;
        public float LIndexTrigger;
        public float RIndexTrigger;
        public float LHandTrigger;
        public float RHandTrigger;
        public Vector2f LThumbstick;
        public Vector2f RThumbstick;
        public Vector2f LTouchpad;
        public Vector2f RTouchpad;
        public byte LBatteryPercentRemaining;
        public byte RBatteryPercentRemaining;
        public byte LRecenterCount;
        public byte RRecenterCount;
        public float LThumbRestForce;
        public float RThumbRestForce;
        public float LStylusForce;
        public float RStylusForce;
        public float LIndexTriggerCurl;
        public float RIndexTriggerCurl;
        public float LIndexTriggerSlide;
        public float RIndexTriggerSlide;

        public ControllerState5(ControllerState4 cs)
        {
            ConnectedControllers = cs.ConnectedControllers;
            Buttons = cs.Buttons;
            Touches = cs.Touches;
            NearTouches = cs.NearTouches;
            LIndexTrigger = cs.LIndexTrigger;
            RIndexTrigger = cs.RIndexTrigger;
            LHandTrigger = cs.LHandTrigger;
            RHandTrigger = cs.RHandTrigger;
            LThumbstick = cs.LThumbstick;
            RThumbstick = cs.RThumbstick;
            LTouchpad = cs.LTouchpad;
            RTouchpad = cs.RTouchpad;
            LBatteryPercentRemaining = cs.LBatteryPercentRemaining;
            RBatteryPercentRemaining = cs.RBatteryPercentRemaining;
            LRecenterCount = cs.LRecenterCount;
            RRecenterCount = cs.RRecenterCount;
            LThumbRestForce = 0.0f;
            RThumbRestForce = 0.0f;
            LStylusForce = 0.0f;
            RStylusForce = 0.0f;
            LIndexTriggerCurl = 0.0f;
            RIndexTriggerCurl = 0.0f;
            LIndexTriggerSlide = 0.0f;
            RIndexTriggerSlide = 0.0f;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ControllerState4
    {
        public uint ConnectedControllers;
        public uint Buttons;
        public uint Touches;
        public uint NearTouches;
        public float LIndexTrigger;
        public float RIndexTrigger;
        public float LHandTrigger;
        public float RHandTrigger;
        public Vector2f LThumbstick;
        public Vector2f RThumbstick;
        public Vector2f LTouchpad;
        public Vector2f RTouchpad;
        public byte LBatteryPercentRemaining;
        public byte RBatteryPercentRemaining;
        public byte LRecenterCount;
        public byte RRecenterCount;
        public byte Reserved_27;
        public byte Reserved_26;
        public byte Reserved_25;
        public byte Reserved_24;
        public byte Reserved_23;
        public byte Reserved_22;
        public byte Reserved_21;
        public byte Reserved_20;
        public byte Reserved_19;
        public byte Reserved_18;
        public byte Reserved_17;
        public byte Reserved_16;
        public byte Reserved_15;
        public byte Reserved_14;
        public byte Reserved_13;
        public byte Reserved_12;
        public byte Reserved_11;
        public byte Reserved_10;
        public byte Reserved_09;
        public byte Reserved_08;
        public byte Reserved_07;
        public byte Reserved_06;
        public byte Reserved_05;
        public byte Reserved_04;
        public byte Reserved_03;
        public byte Reserved_02;
        public byte Reserved_01;
        public byte Reserved_00;

        public ControllerState4(ControllerState2 cs)
        {
            ConnectedControllers = cs.ConnectedControllers;
            Buttons = cs.Buttons;
            Touches = cs.Touches;
            NearTouches = cs.NearTouches;
            LIndexTrigger = cs.LIndexTrigger;
            RIndexTrigger = cs.RIndexTrigger;
            LHandTrigger = cs.LHandTrigger;
            RHandTrigger = cs.RHandTrigger;
            LThumbstick = cs.LThumbstick;
            RThumbstick = cs.RThumbstick;
            LTouchpad = cs.LTouchpad;
            RTouchpad = cs.RTouchpad;
            LBatteryPercentRemaining = 0;
            RBatteryPercentRemaining = 0;
            LRecenterCount = 0;
            RRecenterCount = 0;
            Reserved_27 = 0;
            Reserved_26 = 0;
            Reserved_25 = 0;
            Reserved_24 = 0;
            Reserved_23 = 0;
            Reserved_22 = 0;
            Reserved_21 = 0;
            Reserved_20 = 0;
            Reserved_19 = 0;
            Reserved_18 = 0;
            Reserved_17 = 0;
            Reserved_16 = 0;
            Reserved_15 = 0;
            Reserved_14 = 0;
            Reserved_13 = 0;
            Reserved_12 = 0;
            Reserved_11 = 0;
            Reserved_10 = 0;
            Reserved_09 = 0;
            Reserved_08 = 0;
            Reserved_07 = 0;
            Reserved_06 = 0;
            Reserved_05 = 0;
            Reserved_04 = 0;
            Reserved_03 = 0;
            Reserved_02 = 0;
            Reserved_01 = 0;
            Reserved_00 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ControllerState2
    {
        public uint ConnectedControllers;
        public uint Buttons;
        public uint Touches;
        public uint NearTouches;
        public float LIndexTrigger;
        public float RIndexTrigger;
        public float LHandTrigger;
        public float RHandTrigger;
        public Vector2f LThumbstick;
        public Vector2f RThumbstick;
        public Vector2f LTouchpad;
        public Vector2f RTouchpad;

        public ControllerState2(ControllerState cs)
        {
            ConnectedControllers = cs.ConnectedControllers;
            Buttons = cs.Buttons;
            Touches = cs.Touches;
            NearTouches = cs.NearTouches;
            LIndexTrigger = cs.LIndexTrigger;
            RIndexTrigger = cs.RIndexTrigger;
            LHandTrigger = cs.LHandTrigger;
            RHandTrigger = cs.RHandTrigger;
            LThumbstick = cs.LThumbstick;
            RThumbstick = cs.RThumbstick;
            LTouchpad = new Vector2f() { x = 0.0f, y = 0.0f };
            RTouchpad = new Vector2f() { x = 0.0f, y = 0.0f };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ControllerState
    {
        public uint ConnectedControllers;
        public uint Buttons;
        public uint Touches;
        public uint NearTouches;
        public float LIndexTrigger;
        public float RIndexTrigger;
        public float LHandTrigger;
        public float RHandTrigger;
        public Vector2f LThumbstick;
        public Vector2f RThumbstick;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct HapticsBuffer
    {
        public IntPtr Samples;
        public int SamplesCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HapticsState
    {
        public int SamplesAvailable;
        public int SamplesQueued;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HapticsDesc
    {
        public int SampleRateHz;
        public int SampleSizeInBytes;
        public int MinimumSafeSamplesQueued;
        public int MinimumBufferSamplesCount;
        public int OptimalBufferSamplesCount;
        public int MaximumBufferSamplesCount;
    }

    [Flags]
    public enum PassthroughPreferenceFields
    {
        Flags = 1 << 0
    }

    [Flags]
    public enum PassthroughPreferenceFlags : long
    {
        DefaultToActive = 1 << 0
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PassthroughPreferences
    {
        public PassthroughPreferenceFields Fields;
        public PassthroughPreferenceFlags Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HapticsAmplitudeEnvelopeVibration
    {
        public float Duration;
        public UInt32 AmplitudeCount;
        public IntPtr Amplitudes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HapticsPcmVibration
    {
        public UInt32 BufferSize;
        public IntPtr Buffer;
        public float SampleRateHz;
        public Bool Append;
        public IntPtr SamplesConsumed;
    }

    public enum HapticsConstants
    {
        MaxSamples = 4000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AppPerfFrameStats
    {
        public int HmdVsyncIndex;
        public int AppFrameIndex;
        public int AppDroppedFrameCount;
        public float AppMotionToPhotonLatency;
        public float AppQueueAheadTime;
        public float AppCpuElapsedTime;
        public float AppGpuElapsedTime;
        public int CompositorFrameIndex;
        public int CompositorDroppedFrameCount;
        public float CompositorLatency;
        public float CompositorCpuElapsedTime;
        public float CompositorGpuElapsedTime;
        public float CompositorCpuStartToGpuEndElapsedTime;
        public float CompositorGpuEndToVsyncElapsedTime;
    }

    public const int AppPerfFrameStatsMaxCount = 5;

    [StructLayout(LayoutKind.Sequential)]
    public struct AppPerfStats
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = AppPerfFrameStatsMaxCount)]
        public AppPerfFrameStats[] FrameStats;

        public int FrameStatsCount;
        public Bool AnyFrameStatsDropped;
        public float AdaptiveGpuPerformanceScale;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Sizei : IEquatable<Sizei>
    {
        public int w;
        public int h;

        public static readonly Sizei zero = new Sizei { w = 0, h = 0 };

        public bool Equals(Sizei other)
        {
            return w == other.w && h == other.h;
        }

        public override bool Equals(object obj)
        {
            return obj is Sizei other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (w * 397) ^ h;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Sizef
    {
        public float w;
        public float h;

        public static readonly Sizef zero = new Sizef { w = 0, h = 0 };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Size3f
    {
        public float w;
        public float h;
        public float d;

        public static readonly Size3f zero = new Size3f { w = 0, h = 0, d = 0 };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2i
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Recti
    {
        public Vector2i Pos;
        public Sizei Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RectiPair
    {
        public Recti Rect0;
        public Recti Rect1;

        public Recti this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Rect0;
                    case 1: return Rect1;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        Rect0 = value;
                        return;
                    case 1:
                        Rect1 = value;
                        return;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rectf
    {
        public Vector2f Pos;
        public Sizef Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RectfPair
    {
        public Rectf Rect0;
        public Rectf Rect1;

        public Rectf this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Rect0;
                    case 1: return Rect1;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        Rect0 = value;
                        return;
                    case 1:
                        Rect1 = value;
                        return;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Boundsf
    {
        public Vector3f Pos;
        public Size3f Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Frustumf
    {
        public float zNear;
        public float zFar;
        public float fovX;
        public float fovY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Frustumf2
    {
        public float zNear;
        public float zFar;
        public Fovf Fov;
    }

    public enum BoundaryType
    {
        [System.Obsolete("Deprecated. This enum value will not be supported in OpenXR", false)]
        OuterBoundary = 0x0001,
        PlayArea = 0x0100,
    }

    [System.Obsolete("Deprecated. This struct will not be supported in OpenXR", false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundaryTestResult
    {
        public Bool IsTriggering;
        public float ClosestDistance;
        public Vector3f ClosestPoint;
        public Vector3f ClosestPointNormal;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoundaryGeometry
    {
        public BoundaryType BoundaryType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public Vector3f[] Points;

        public int PointsCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Colorf
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "R:{0:F3} G:{1:F3} B:{2:F3} A:{3:F3}", r, g, b, a);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Fovf
    {
        public float UpTan;
        public float DownTan;
        public float LeftTan;
        public float RightTan;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FovfPair
    {
        public Fovf Fov0;
        public Fovf Fov1;

        public Fovf this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return Fov0;
                    case 1: return Fov1;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        Fov0 = value;
                        return;
                    case 1:
                        Fov1 = value;
                        return;
                    default: throw new IndexOutOfRangeException($"{i} was not in range [0,2)");
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraIntrinsics
    {
        public Bool IsValid;
        public double LastChangedTimeSeconds;
        public Fovf FOVPort;
        public float VirtualNearPlaneDistanceMeters;
        public float VirtualFarPlaneDistanceMeters;
        public Sizei ImageSensorPixelResolution;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraExtrinsics
    {
        public Bool IsValid;
        public double LastChangedTimeSeconds;
        public CameraStatus CameraStatusData;
        public Node AttachedToNode;
        public Posef RelativePose;
    }

    public enum LayerLayout
    {
        Stereo = 0,
        Mono = 1,
        DoubleWide = 2,
        Array = 3,
        EnumSize = 0xF
    }

    public enum LayerFlags
    {
        Static = (1 << 0),
        LoadingScreen = (1 << 1),
        SymmetricFov = (1 << 2),
        TextureOriginAtBottomLeft = (1 << 3),
        ChromaticAberrationCorrection = (1 << 4),
        NoAllocation = (1 << 5),
        ProtectedContent = (1 << 6),
        AndroidSurfaceSwapChain = (1 << 7),
        BicubicFiltering = (1 << 14),
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LayerDesc
    {
        public OverlayShape Shape;
        public LayerLayout Layout;
        public Sizei TextureSize;
        public int MipLevels;
        public int SampleCount;
        public EyeTextureFormat Format;
        public int LayerFlags;

        //Eye FOV-only members.
        public FovfPair Fov;
        public RectfPair VisibleRect;
        public Sizei MaxViewportSize;
        public EyeTextureFormat DepthFormat;

        public EyeTextureFormat MotionVectorFormat;
        public EyeTextureFormat MotionVectorDepthFormat;
        public Sizei MotionVectorTextureSize;

        public override string ToString()
        {
            string delim = ", ";
            return Shape.ToString()
                   + delim + Layout.ToString()
                   + delim + TextureSize.w.ToString() + "x" + TextureSize.h.ToString()
                   + delim + MipLevels.ToString()
                   + delim + SampleCount.ToString()
                   + delim + Format.ToString()
                   + delim + LayerFlags.ToString();
        }
    }


    public enum BlendFactor
    {
        Zero = 0,
        One = 1,
        SrcAlpha = 2,
        OneMinusSrcAlpha = 3,
        DstAlpha = 4,
        OneMinusDstAlpha = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LayerSubmit
    {
        readonly int LayerId;
        readonly int TextureStage;

        RectiPair ViewportRect;

        Posef Pose;
        readonly int LayerSubmitFlags;
    }

    public enum TrackingConfidence
    {
        Low = 0,
        High = 0x3f800000,
    }

    public enum Hand
    {
        None = -1,
        HandLeft = 0,
        HandRight = 1,
    }

    [Flags]
    public enum HandStatus
    {
        HandTracked = (1 << 0), // if this is set the hand pose and bone rotations data is usable
        InputStateValid = (1 << 1), // if this is set the pointer pose and pinch data is usable
        SystemGestureInProgress = (1 << 6), // if this is set the hand is currently processing a system gesture
        DominantHand = (1 << 7), // if this is set the hand is currently the dominant hand
        MenuPressed = (1 << 8) // if this is set the hand performed a menu press
    }

    public enum BoneId
    {
        Invalid = -1,

        // hand bones
        Hand_Start = 0,
        Hand_WristRoot = Hand_Start + 0, // root frame of the hand, where the wrist is located
        Hand_ForearmStub = Hand_Start + 1, // frame for user's forearm
        Hand_Thumb0 = Hand_Start + 2, // thumb trapezium bone
        Hand_Thumb1 = Hand_Start + 3, // thumb metacarpal bone
        Hand_Thumb2 = Hand_Start + 4, // thumb proximal phalange bone
        Hand_Thumb3 = Hand_Start + 5, // thumb distal phalange bone
        Hand_Index1 = Hand_Start + 6, // index proximal phalange bone
        Hand_Index2 = Hand_Start + 7, // index intermediate phalange bone
        Hand_Index3 = Hand_Start + 8, // index distal phalange bone
        Hand_Middle1 = Hand_Start + 9, // middle proximal phalange bone
        Hand_Middle2 = Hand_Start + 10, // middle intermediate phalange bone
        Hand_Middle3 = Hand_Start + 11, // middle distal phalange bone
        Hand_Ring1 = Hand_Start + 12, // ring proximal phalange bone
        Hand_Ring2 = Hand_Start + 13, // ring intermediate phalange bone
        Hand_Ring3 = Hand_Start + 14, // ring distal phalange bone
        Hand_Pinky0 = Hand_Start + 15, // pinky metacarpal bone
        Hand_Pinky1 = Hand_Start + 16, // pinky proximal phalange bone
        Hand_Pinky2 = Hand_Start + 17, // pinky intermediate phalange bone
        Hand_Pinky3 = Hand_Start + 18, // pinky distal phalange bone
        Hand_MaxSkinnable = Hand_Start + 19,

        // Bone tips are position only. They are not used for skinning but are useful for hit-testing.
        // NOTE: Hand_ThumbTip == Hand_MaxSkinnable since the extended tips need to be contiguous
        Hand_ThumbTip = Hand_MaxSkinnable + 0, // tip of the thumb
        Hand_IndexTip = Hand_MaxSkinnable + 1, // tip of the index finger
        Hand_MiddleTip = Hand_MaxSkinnable + 2, // tip of the middle finger
        Hand_RingTip = Hand_MaxSkinnable + 3, // tip of the ring finger
        Hand_PinkyTip = Hand_MaxSkinnable + 4, // tip of the pinky
        Hand_End = Hand_MaxSkinnable + 5,

        // body bones (upper body)
        Body_Start = 0,
        Body_Root = Body_Start + 0,
        Body_Hips = Body_Start + 1,
        Body_SpineLower = Body_Start + 2,
        Body_SpineMiddle = Body_Start + 3,
        Body_SpineUpper = Body_Start + 4,
        Body_Chest = Body_Start + 5,
        Body_Neck = Body_Start + 6,
        Body_Head = Body_Start + 7,
        Body_LeftShoulder = Body_Start + 8,
        Body_LeftScapula = Body_Start + 9,
        Body_LeftArmUpper = Body_Start + 10,
        Body_LeftArmLower = Body_Start + 11,
        Body_LeftHandWristTwist = Body_Start + 12,
        Body_RightShoulder = Body_Start + 13,
        Body_RightScapula = Body_Start + 14,
        Body_RightArmUpper = Body_Start + 15,
        Body_RightArmLower = Body_Start + 16,
        Body_RightHandWristTwist = Body_Start + 17,
        Body_LeftHandPalm = Body_Start + 18,
        Body_LeftHandWrist = Body_Start + 19,
        Body_LeftHandThumbMetacarpal = Body_Start + 20,
        Body_LeftHandThumbProximal = Body_Start + 21,
        Body_LeftHandThumbDistal = Body_Start + 22,
        Body_LeftHandThumbTip = Body_Start + 23,
        Body_LeftHandIndexMetacarpal = Body_Start + 24,
        Body_LeftHandIndexProximal = Body_Start + 25,
        Body_LeftHandIndexIntermediate = Body_Start + 26,
        Body_LeftHandIndexDistal = Body_Start + 27,
        Body_LeftHandIndexTip = Body_Start + 28,
        Body_LeftHandMiddleMetacarpal = Body_Start + 29,
        Body_LeftHandMiddleProximal = Body_Start + 30,
        Body_LeftHandMiddleIntermediate = Body_Start + 31,
        Body_LeftHandMiddleDistal = Body_Start + 32,
        Body_LeftHandMiddleTip = Body_Start + 33,
        Body_LeftHandRingMetacarpal = Body_Start + 34,
        Body_LeftHandRingProximal = Body_Start + 35,
        Body_LeftHandRingIntermediate = Body_Start + 36,
        Body_LeftHandRingDistal = Body_Start + 37,
        Body_LeftHandRingTip = Body_Start + 38,
        Body_LeftHandLittleMetacarpal = Body_Start + 39,
        Body_LeftHandLittleProximal = Body_Start + 40,
        Body_LeftHandLittleIntermediate = Body_Start + 41,
        Body_LeftHandLittleDistal = Body_Start + 42,
        Body_LeftHandLittleTip = Body_Start + 43,
        Body_RightHandPalm = Body_Start + 44,
        Body_RightHandWrist = Body_Start + 45,
        Body_RightHandThumbMetacarpal = Body_Start + 46,
        Body_RightHandThumbProximal = Body_Start + 47,
        Body_RightHandThumbDistal = Body_Start + 48,
        Body_RightHandThumbTip = Body_Start + 49,
        Body_RightHandIndexMetacarpal = Body_Start + 50,
        Body_RightHandIndexProximal = Body_Start + 51,
        Body_RightHandIndexIntermediate = Body_Start + 52,
        Body_RightHandIndexDistal = Body_Start + 53,
        Body_RightHandIndexTip = Body_Start + 54,
        Body_RightHandMiddleMetacarpal = Body_Start + 55,
        Body_RightHandMiddleProximal = Body_Start + 56,
        Body_RightHandMiddleIntermediate = Body_Start + 57,
        Body_RightHandMiddleDistal = Body_Start + 58,
        Body_RightHandMiddleTip = Body_Start + 59,
        Body_RightHandRingMetacarpal = Body_Start + 60,
        Body_RightHandRingProximal = Body_Start + 61,
        Body_RightHandRingIntermediate = Body_Start + 62,
        Body_RightHandRingDistal = Body_Start + 63,
        Body_RightHandRingTip = Body_Start + 64,
        Body_RightHandLittleMetacarpal = Body_Start + 65,
        Body_RightHandLittleProximal = Body_Start + 66,
        Body_RightHandLittleIntermediate = Body_Start + 67,
        Body_RightHandLittleDistal = Body_Start + 68,
        Body_RightHandLittleTip = Body_Start + 69,
        Body_End = Body_Start + 70,

        // full body bones
        FullBody_Start = 0,
        FullBody_Root = FullBody_Start + 0,
        FullBody_Hips = FullBody_Start + 1,
        FullBody_SpineLower = FullBody_Start + 2,
        FullBody_SpineMiddle = FullBody_Start + 3,
        FullBody_SpineUpper = FullBody_Start + 4,
        FullBody_Chest = FullBody_Start + 5,
        FullBody_Neck = FullBody_Start + 6,
        FullBody_Head = FullBody_Start + 7,
        FullBody_LeftShoulder = FullBody_Start + 8,
        FullBody_LeftScapula = FullBody_Start + 9,
        FullBody_LeftArmUpper = FullBody_Start + 10,
        FullBody_LeftArmLower = FullBody_Start + 11,
        FullBody_LeftHandWristTwist = FullBody_Start + 12,
        FullBody_RightShoulder = FullBody_Start + 13,
        FullBody_RightScapula = FullBody_Start + 14,
        FullBody_RightArmUpper = FullBody_Start + 15,
        FullBody_RightArmLower = FullBody_Start + 16,
        FullBody_RightHandWristTwist = FullBody_Start + 17,
        FullBody_LeftHandPalm = FullBody_Start + 18,
        FullBody_LeftHandWrist = FullBody_Start + 19,
        FullBody_LeftHandThumbMetacarpal = FullBody_Start + 20,
        FullBody_LeftHandThumbProximal = FullBody_Start + 21,
        FullBody_LeftHandThumbDistal = FullBody_Start + 22,
        FullBody_LeftHandThumbTip = FullBody_Start + 23,
        FullBody_LeftHandIndexMetacarpal = FullBody_Start + 24,
        FullBody_LeftHandIndexProximal = FullBody_Start + 25,
        FullBody_LeftHandIndexIntermediate = FullBody_Start + 26,
        FullBody_LeftHandIndexDistal = FullBody_Start + 27,
        FullBody_LeftHandIndexTip = FullBody_Start + 28,
        FullBody_LeftHandMiddleMetacarpal = FullBody_Start + 29,
        FullBody_LeftHandMiddleProximal = FullBody_Start + 30,
        FullBody_LeftHandMiddleIntermediate = FullBody_Start + 31,
        FullBody_LeftHandMiddleDistal = FullBody_Start + 32,
        FullBody_LeftHandMiddleTip = FullBody_Start + 33,
        FullBody_LeftHandRingMetacarpal = FullBody_Start + 34,
        FullBody_LeftHandRingProximal = FullBody_Start + 35,
        FullBody_LeftHandRingIntermediate = FullBody_Start + 36,
        FullBody_LeftHandRingDistal = FullBody_Start + 37,
        FullBody_LeftHandRingTip = FullBody_Start + 38,
        FullBody_LeftHandLittleMetacarpal = FullBody_Start + 39,
        FullBody_LeftHandLittleProximal = FullBody_Start + 40,
        FullBody_LeftHandLittleIntermediate = FullBody_Start + 41,
        FullBody_LeftHandLittleDistal = FullBody_Start + 42,
        FullBody_LeftHandLittleTip = FullBody_Start + 43,
        FullBody_RightHandPalm = FullBody_Start + 44,
        FullBody_RightHandWrist = FullBody_Start + 45,
        FullBody_RightHandThumbMetacarpal = FullBody_Start + 46,
        FullBody_RightHandThumbProximal = FullBody_Start + 47,
        FullBody_RightHandThumbDistal = FullBody_Start + 48,
        FullBody_RightHandThumbTip = FullBody_Start + 49,
        FullBody_RightHandIndexMetacarpal = FullBody_Start + 50,
        FullBody_RightHandIndexProximal = FullBody_Start + 51,
        FullBody_RightHandIndexIntermediate = FullBody_Start + 52,
        FullBody_RightHandIndexDistal = FullBody_Start + 53,
        FullBody_RightHandIndexTip = FullBody_Start + 54,
        FullBody_RightHandMiddleMetacarpal = FullBody_Start + 55,
        FullBody_RightHandMiddleProximal = FullBody_Start + 56,
        FullBody_RightHandMiddleIntermediate = FullBody_Start + 57,
        FullBody_RightHandMiddleDistal = FullBody_Start + 58,
        FullBody_RightHandMiddleTip = FullBody_Start + 59,
        FullBody_RightHandRingMetacarpal = FullBody_Start + 60,
        FullBody_RightHandRingProximal = FullBody_Start + 61,
        FullBody_RightHandRingIntermediate = FullBody_Start + 62,
        FullBody_RightHandRingDistal = FullBody_Start + 63,
        FullBody_RightHandRingTip = FullBody_Start + 64,
        FullBody_RightHandLittleMetacarpal = FullBody_Start + 65,
        FullBody_RightHandLittleProximal = FullBody_Start + 66,
        FullBody_RightHandLittleIntermediate = FullBody_Start + 67,
        FullBody_RightHandLittleDistal = FullBody_Start + 68,
        FullBody_RightHandLittleTip = FullBody_Start + 69,
        FullBody_LeftUpperLeg = FullBody_Start + 70,
        FullBody_LeftLowerLeg = FullBody_Start + 71,
        FullBody_LeftFootAnkleTwist = FullBody_Start + 72,
        FullBody_LeftFootAnkle = FullBody_Start + 73,
        FullBody_LeftFootSubtalar = FullBody_Start + 74,
        FullBody_LeftFootTransverse = FullBody_Start + 75,
        FullBody_LeftFootBall = FullBody_Start + 76,
        FullBody_RightUpperLeg = FullBody_Start + 77,
        FullBody_RightLowerLeg = FullBody_Start + 78,
        FullBody_RightFootAnkleTwist = FullBody_Start + 79,
        FullBody_RightFootAnkle = FullBody_Start + 80,
        FullBody_RightFootSubtalar = FullBody_Start + 81,
        FullBody_RightFootTransverse = FullBody_Start + 82,
        FullBody_RightFootBall = FullBody_Start + 83,
        FullBody_End = FullBody_Start + 84,
        FullBody_Invalid = FullBody_Start + 85,

        // add new bones here

        Max = FullBody_End,
    }

    public enum HandFinger
    {
        Thumb = 0,
        Index = 1,
        Middle = 2,
        Ring = 3,
        Pinky = 4,
        Max = 5,
    }


    [Flags]
    public enum HandFingerPinch
    {
        Thumb = (1 << HandFinger.Thumb),
        Index = (1 << HandFinger.Index),
        Middle = (1 << HandFinger.Middle),
        Ring = (1 << HandFinger.Ring),
        Pinky = (1 << HandFinger.Pinky),
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HandState
    {
        public HandStatus Status;
        public Posef RootPose;
        public Quatf[] BoneRotations;
        public HandFingerPinch Pinches;
        public float[] PinchStrength;
        public Posef PointerPose;
        public float HandScale;
        public TrackingConfidence HandConfidence;
        public TrackingConfidence[] FingerConfidences;
        public double RequestedTimeStamp;
        public double SampleTimeStamp;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct HandStateInternal
    {
        public HandStatus Status;
        public Posef RootPose;
        public Quatf BoneRotations_0;
        public Quatf BoneRotations_1;
        public Quatf BoneRotations_2;
        public Quatf BoneRotations_3;
        public Quatf BoneRotations_4;
        public Quatf BoneRotations_5;
        public Quatf BoneRotations_6;
        public Quatf BoneRotations_7;
        public Quatf BoneRotations_8;
        public Quatf BoneRotations_9;
        public Quatf BoneRotations_10;
        public Quatf BoneRotations_11;
        public Quatf BoneRotations_12;
        public Quatf BoneRotations_13;
        public Quatf BoneRotations_14;
        public Quatf BoneRotations_15;
        public Quatf BoneRotations_16;
        public Quatf BoneRotations_17;
        public Quatf BoneRotations_18;
        public Quatf BoneRotations_19;
        public Quatf BoneRotations_20;
        public Quatf BoneRotations_21;
        public Quatf BoneRotations_22;
        public Quatf BoneRotations_23;
        public HandFingerPinch Pinches;
        public float PinchStrength_0;
        public float PinchStrength_1;
        public float PinchStrength_2;
        public float PinchStrength_3;
        public float PinchStrength_4;
        public Posef PointerPose;
        public float HandScale;
        public TrackingConfidence HandConfidence;
        public TrackingConfidence FingerConfidences_0;
        public TrackingConfidence FingerConfidences_1;
        public TrackingConfidence FingerConfidences_2;
        public TrackingConfidence FingerConfidences_3;
        public TrackingConfidence FingerConfidences_4;
        public double RequestedTimeStamp;
        public double SampleTimeStamp;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoneCapsule
    {
        public short BoneIndex;
        public Vector3f StartPoint;
        public Vector3f EndPoint;
        public float Radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bone
    {
        public BoneId Id;
        public short ParentBoneIndex;
        public Posef Pose;
    }

    public enum SkeletonConstants
    {
        MaxHandBones = BoneId.Hand_End,
        MaxBodyBones = BoneId.Body_End,
        MaxBones = BoneId.Max,
        MaxBoneCapsules = 19,
    }

    public enum SkeletonType
    {
        None = -1,
        HandLeft = 0,
        HandRight = 1,
        Body = 2,
        FullBody = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skeleton
    {
        public SkeletonType Type;
        public uint NumBones;
        public uint NumBoneCapsules;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)SkeletonConstants.MaxHandBones)]
        public Bone[] Bones;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)SkeletonConstants.MaxBoneCapsules)]
        public BoneCapsule[] BoneCapsules;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skeleton2
    {
        public SkeletonType Type;
        public uint NumBones;
        public uint NumBoneCapsules;
        public Bone[] Bones;
        public BoneCapsule[] BoneCapsules;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skeleton2Internal
    {
        public SkeletonType Type;
        public uint NumBones;
        public uint NumBoneCapsules;
        public Bone Bones_0;
        public Bone Bones_1;
        public Bone Bones_2;
        public Bone Bones_3;
        public Bone Bones_4;
        public Bone Bones_5;
        public Bone Bones_6;
        public Bone Bones_7;
        public Bone Bones_8;
        public Bone Bones_9;
        public Bone Bones_10;
        public Bone Bones_11;
        public Bone Bones_12;
        public Bone Bones_13;
        public Bone Bones_14;
        public Bone Bones_15;
        public Bone Bones_16;
        public Bone Bones_17;
        public Bone Bones_18;
        public Bone Bones_19;
        public Bone Bones_20;
        public Bone Bones_21;
        public Bone Bones_22;
        public Bone Bones_23;
        public Bone Bones_24;
        public Bone Bones_25;
        public Bone Bones_26;
        public Bone Bones_27;
        public Bone Bones_28;
        public Bone Bones_29;
        public Bone Bones_30;
        public Bone Bones_31;
        public Bone Bones_32;
        public Bone Bones_33;
        public Bone Bones_34;
        public Bone Bones_35;
        public Bone Bones_36;
        public Bone Bones_37;
        public Bone Bones_38;
        public Bone Bones_39;
        public Bone Bones_40;
        public Bone Bones_41;
        public Bone Bones_42;
        public Bone Bones_43;
        public Bone Bones_44;
        public Bone Bones_45;
        public Bone Bones_46;
        public Bone Bones_47;
        public Bone Bones_48;
        public Bone Bones_49;
        public Bone Bones_50;
        public Bone Bones_51;
        public Bone Bones_52;
        public Bone Bones_53;
        public Bone Bones_54;
        public Bone Bones_55;
        public Bone Bones_56;
        public Bone Bones_57;
        public Bone Bones_58;
        public Bone Bones_59;
        public Bone Bones_60;
        public Bone Bones_61;
        public Bone Bones_62;
        public Bone Bones_63;
        public Bone Bones_64;
        public Bone Bones_65;
        public Bone Bones_66;
        public Bone Bones_67;
        public Bone Bones_68;
        public Bone Bones_69;
        public BoneCapsule BoneCapsules_0;
        public BoneCapsule BoneCapsules_1;
        public BoneCapsule BoneCapsules_2;
        public BoneCapsule BoneCapsules_3;
        public BoneCapsule BoneCapsules_4;
        public BoneCapsule BoneCapsules_5;
        public BoneCapsule BoneCapsules_6;
        public BoneCapsule BoneCapsules_7;
        public BoneCapsule BoneCapsules_8;
        public BoneCapsule BoneCapsules_9;
        public BoneCapsule BoneCapsules_10;
        public BoneCapsule BoneCapsules_11;
        public BoneCapsule BoneCapsules_12;
        public BoneCapsule BoneCapsules_13;
        public BoneCapsule BoneCapsules_14;
        public BoneCapsule BoneCapsules_15;
        public BoneCapsule BoneCapsules_16;
        public BoneCapsule BoneCapsules_17;
        public BoneCapsule BoneCapsules_18;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skeleton3Internal
    {
        public SkeletonType Type;
        public uint NumBones;
        public uint NumBoneCapsules;
        public Bone Bones_0;
        public Bone Bones_1;
        public Bone Bones_2;
        public Bone Bones_3;
        public Bone Bones_4;
        public Bone Bones_5;
        public Bone Bones_6;
        public Bone Bones_7;
        public Bone Bones_8;
        public Bone Bones_9;
        public Bone Bones_10;
        public Bone Bones_11;
        public Bone Bones_12;
        public Bone Bones_13;
        public Bone Bones_14;
        public Bone Bones_15;
        public Bone Bones_16;
        public Bone Bones_17;
        public Bone Bones_18;
        public Bone Bones_19;
        public Bone Bones_20;
        public Bone Bones_21;
        public Bone Bones_22;
        public Bone Bones_23;
        public Bone Bones_24;
        public Bone Bones_25;
        public Bone Bones_26;
        public Bone Bones_27;
        public Bone Bones_28;
        public Bone Bones_29;
        public Bone Bones_30;
        public Bone Bones_31;
        public Bone Bones_32;
        public Bone Bones_33;
        public Bone Bones_34;
        public Bone Bones_35;
        public Bone Bones_36;
        public Bone Bones_37;
        public Bone Bones_38;
        public Bone Bones_39;
        public Bone Bones_40;
        public Bone Bones_41;
        public Bone Bones_42;
        public Bone Bones_43;
        public Bone Bones_44;
        public Bone Bones_45;
        public Bone Bones_46;
        public Bone Bones_47;
        public Bone Bones_48;
        public Bone Bones_49;
        public Bone Bones_50;
        public Bone Bones_51;
        public Bone Bones_52;
        public Bone Bones_53;
        public Bone Bones_54;
        public Bone Bones_55;
        public Bone Bones_56;
        public Bone Bones_57;
        public Bone Bones_58;
        public Bone Bones_59;
        public Bone Bones_60;
        public Bone Bones_61;
        public Bone Bones_62;
        public Bone Bones_63;
        public Bone Bones_64;
        public Bone Bones_65;
        public Bone Bones_66;
        public Bone Bones_67;
        public Bone Bones_68;
        public Bone Bones_69;
        public Bone Bones_70;
        public Bone Bones_71;
        public Bone Bones_72;
        public Bone Bones_73;
        public Bone Bones_74;
        public Bone Bones_75;
        public Bone Bones_76;
        public Bone Bones_77;
        public Bone Bones_78;
        public Bone Bones_79;
        public Bone Bones_80;
        public Bone Bones_81;
        public Bone Bones_82;
        public Bone Bones_83;
        public BoneCapsule BoneCapsules_0;
        public BoneCapsule BoneCapsules_1;
        public BoneCapsule BoneCapsules_2;
        public BoneCapsule BoneCapsules_3;
        public BoneCapsule BoneCapsules_4;
        public BoneCapsule BoneCapsules_5;
        public BoneCapsule BoneCapsules_6;
        public BoneCapsule BoneCapsules_7;
        public BoneCapsule BoneCapsules_8;
        public BoneCapsule BoneCapsules_9;
        public BoneCapsule BoneCapsules_10;
        public BoneCapsule BoneCapsules_11;
        public BoneCapsule BoneCapsules_12;
        public BoneCapsule BoneCapsules_13;
        public BoneCapsule BoneCapsules_14;
        public BoneCapsule BoneCapsules_15;
        public BoneCapsule BoneCapsules_16;
        public BoneCapsule BoneCapsules_17;
        public BoneCapsule BoneCapsules_18;
    }
    public enum MeshConstants
    {
        MaxVertices = 3000,
        MaxIndices = MaxVertices * 6,
    }

    public enum MeshType
    {
        None = -1,
        HandLeft = 0,
        HandRight = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Mesh
    {
        public MeshType Type;
        public uint NumVertices;
        public uint NumIndices;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxVertices)]
        public Vector3f[] VertexPositions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxIndices)]
        public short[] Indices;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxVertices)]
        public Vector3f[] VertexNormals;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxVertices)]
        public Vector2f[] VertexUV0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxVertices)]
        public Vector4s[] BlendIndices;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)MeshConstants.MaxVertices)]
        public Vector4f[] BlendWeights;
    }

    /// <summary>
    /// Space location flags
    /// </summary>
    /// <remarks>
    /// See the [OpenXR spc](https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrSpaceLocationFlags.html) for
    /// more information.
    /// </remarks>
    [Flags]
    public enum SpaceLocationFlags : ulong
    {
        /// <summary>
        /// Indicates that the pose field's orientation field contains valid data.
        /// </summary>
        /// <remarks>
        /// Applications must not read a pose field's orientation if this flag is unset.
        /// </remarks>
        OrientationValid = 0x00000001,

        /// <summary>
        /// Indicates that the pose field's position field contains valid data.
        /// </summary>
        /// <remarks>
        /// Applications must not read a pose field's position if this flag is unset.
        /// </remarks>
        PositionValid = 0x00000002,

        /// <summary>
        /// Indicates that a pose field's orientation field represents an actively tracked orientation.
        /// </summary>
        /// <remarks>
        /// When a space location tracking an object whose orientation is no longer known during tracking loss
        /// (e.g. an observed QR code), the orientation will be a valid but untracked orientation and will be
        /// meaningful to use.
        /// </remarks>
        OrientationTracked = 0x00000004,

        /// <summary>
        /// Indicates that a pose field's position field represents an actively tracked position.
        /// </summary>
        /// <remarks>
        /// When a space location loses tracking, the position will be a valid but untracked value that is inferred or
        /// last-known, e.g. based on neck model updates, inertial dead reckoning, or a last-known position, and will be
        /// meaningful to use.
        /// </remarks>
        PositionTracked = 0x00000008,
    }

    public static bool IsPositionValid(this SpaceLocationFlags value) =>
        (value & SpaceLocationFlags.PositionValid) != 0;

    public static bool IsOrientationValid(this SpaceLocationFlags value) =>
        (value & SpaceLocationFlags.OrientationValid) != 0;

    public static bool IsPositionTracked(this SpaceLocationFlags value) =>
        (value & SpaceLocationFlags.PositionTracked) != 0;

    public static bool IsOrientationTracked(this SpaceLocationFlags value) =>
        (value & SpaceLocationFlags.OrientationTracked) != 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceLocationf
    {
        public SpaceLocationFlags locationFlags;
        public Posef pose;
    }

    public enum BodyJointSet
    {
        None = -1,
        UpperBody = 0,
        FullBody = 1,
    }


    public enum BodyTrackingFidelity2
    {
        Low = 1,
        High = 2
    }


    public enum BodyTrackingCalibrationState
    {
        Valid = 1,
        Calibrating = 2,
        Invalid = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BodyTrackingCalibrationInfo
    {
        public float BodyHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BodyJointLocation
    {
        /// <summary>
        /// The <see cref="SpaceLocationFlags"/> for this <see cref="BodyJointLocation"/>.
        /// </summary>
        public SpaceLocationFlags LocationFlags;

        /// <summary>
        /// The pose of this <see cref="BodyJointLocation"/>.
        /// </summary>
        public Posef Pose;

        /// <summary>
        /// Indicates that the <see cref="Pose"/>'s <see cref="Posef.Orientation"/> contains valid data.
        /// </summary>
        public bool OrientationValid => (LocationFlags & SpaceLocationFlags.OrientationValid) != 0;

        /// <summary>
        /// Indicates that the <see cref="Pose"/>'s <see cref="Posef.Position"/> contains valid data.
        /// </summary>
        public bool PositionValid => (LocationFlags & SpaceLocationFlags.PositionValid) != 0;

        /// <summary>
        /// Indicates that the <see cref="Pose"/>'s <see cref="Posef.Orientation"/> represents an actively tracked
        /// orientation.
        /// </summary>
        public bool OrientationTracked => (LocationFlags & SpaceLocationFlags.OrientationTracked) != 0;

        /// <summary>
        /// Indicates that the <see cref="Pose"/>'s <see cref="Posef.Position"/> represents an actively tracked
        /// position.
        /// </summary>
        public bool PositionTracked => (LocationFlags & SpaceLocationFlags.PositionTracked) != 0;

        public static readonly BodyJointLocation invalid = new BodyJointLocation
        { LocationFlags = 0, Pose = Posef.identity };
    }

    /// <summary>
    /// Represents the state of a tracked body.
    /// </summary>
    public struct BodyState
    {
        /// <summary>
        /// The <see cref="BodyJointLocation"/>s for each joint in the tracked body.
        /// </summary>
        public BodyJointLocation[] JointLocations;

        /// <summary>
        /// The confidence of the <see cref="JointLocations"/>.
        /// </summary>
        /// <remarks>
        /// This value ranges from 0 to 1, inclusive. 0 means no confidence while 1 means full confidence.
        /// </remarks>
        public float Confidence;

        /// <summary>
        /// The number of times the skeleton has changed.
        /// </summary>
        public uint SkeletonChangedCount;

        /// <summary>
        /// The time, in seconds, corresponding to this state.
        /// </summary>
        public double Time;

        public BodyJointSet JointSet;
        public BodyTrackingCalibrationState CalibrationStatus;
        public BodyTrackingFidelity2 Fidelity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BodyStateInternal
    {
        public Bool IsActive;
        public float Confidence;
        public uint SkeletonChangedCount;
        public double Time;
        public BodyJointLocation JointLocation_0;
        public BodyJointLocation JointLocation_1;
        public BodyJointLocation JointLocation_2;
        public BodyJointLocation JointLocation_3;
        public BodyJointLocation JointLocation_4;
        public BodyJointLocation JointLocation_5;
        public BodyJointLocation JointLocation_6;
        public BodyJointLocation JointLocation_7;
        public BodyJointLocation JointLocation_8;
        public BodyJointLocation JointLocation_9;
        public BodyJointLocation JointLocation_10;
        public BodyJointLocation JointLocation_11;
        public BodyJointLocation JointLocation_12;
        public BodyJointLocation JointLocation_13;
        public BodyJointLocation JointLocation_14;
        public BodyJointLocation JointLocation_15;
        public BodyJointLocation JointLocation_16;
        public BodyJointLocation JointLocation_17;
        public BodyJointLocation JointLocation_18;
        public BodyJointLocation JointLocation_19;
        public BodyJointLocation JointLocation_20;
        public BodyJointLocation JointLocation_21;
        public BodyJointLocation JointLocation_22;
        public BodyJointLocation JointLocation_23;
        public BodyJointLocation JointLocation_24;
        public BodyJointLocation JointLocation_25;
        public BodyJointLocation JointLocation_26;
        public BodyJointLocation JointLocation_27;
        public BodyJointLocation JointLocation_28;
        public BodyJointLocation JointLocation_29;
        public BodyJointLocation JointLocation_30;
        public BodyJointLocation JointLocation_31;
        public BodyJointLocation JointLocation_32;
        public BodyJointLocation JointLocation_33;
        public BodyJointLocation JointLocation_34;
        public BodyJointLocation JointLocation_35;
        public BodyJointLocation JointLocation_36;
        public BodyJointLocation JointLocation_37;
        public BodyJointLocation JointLocation_38;
        public BodyJointLocation JointLocation_39;
        public BodyJointLocation JointLocation_40;
        public BodyJointLocation JointLocation_41;
        public BodyJointLocation JointLocation_42;
        public BodyJointLocation JointLocation_43;
        public BodyJointLocation JointLocation_44;
        public BodyJointLocation JointLocation_45;
        public BodyJointLocation JointLocation_46;
        public BodyJointLocation JointLocation_47;
        public BodyJointLocation JointLocation_48;
        public BodyJointLocation JointLocation_49;
        public BodyJointLocation JointLocation_50;
        public BodyJointLocation JointLocation_51;
        public BodyJointLocation JointLocation_52;
        public BodyJointLocation JointLocation_53;
        public BodyJointLocation JointLocation_54;
        public BodyJointLocation JointLocation_55;
        public BodyJointLocation JointLocation_56;
        public BodyJointLocation JointLocation_57;
        public BodyJointLocation JointLocation_58;
        public BodyJointLocation JointLocation_59;
        public BodyJointLocation JointLocation_60;
        public BodyJointLocation JointLocation_61;
        public BodyJointLocation JointLocation_62;
        public BodyJointLocation JointLocation_63;
        public BodyJointLocation JointLocation_64;
        public BodyJointLocation JointLocation_65;
        public BodyJointLocation JointLocation_66;
        public BodyJointLocation JointLocation_67;
        public BodyJointLocation JointLocation_68;
        public BodyJointLocation JointLocation_69;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct BodyState4Internal
    {
        public Bool IsActive;
        public float Confidence;
        public uint SkeletonChangedCount;
        public double Time;
        public BodyJointLocation JointLocation_0;
        public BodyJointLocation JointLocation_1;
        public BodyJointLocation JointLocation_2;
        public BodyJointLocation JointLocation_3;
        public BodyJointLocation JointLocation_4;
        public BodyJointLocation JointLocation_5;
        public BodyJointLocation JointLocation_6;
        public BodyJointLocation JointLocation_7;
        public BodyJointLocation JointLocation_8;
        public BodyJointLocation JointLocation_9;
        public BodyJointLocation JointLocation_10;
        public BodyJointLocation JointLocation_11;
        public BodyJointLocation JointLocation_12;
        public BodyJointLocation JointLocation_13;
        public BodyJointLocation JointLocation_14;
        public BodyJointLocation JointLocation_15;
        public BodyJointLocation JointLocation_16;
        public BodyJointLocation JointLocation_17;
        public BodyJointLocation JointLocation_18;
        public BodyJointLocation JointLocation_19;
        public BodyJointLocation JointLocation_20;
        public BodyJointLocation JointLocation_21;
        public BodyJointLocation JointLocation_22;
        public BodyJointLocation JointLocation_23;
        public BodyJointLocation JointLocation_24;
        public BodyJointLocation JointLocation_25;
        public BodyJointLocation JointLocation_26;
        public BodyJointLocation JointLocation_27;
        public BodyJointLocation JointLocation_28;
        public BodyJointLocation JointLocation_29;
        public BodyJointLocation JointLocation_30;
        public BodyJointLocation JointLocation_31;
        public BodyJointLocation JointLocation_32;
        public BodyJointLocation JointLocation_33;
        public BodyJointLocation JointLocation_34;
        public BodyJointLocation JointLocation_35;
        public BodyJointLocation JointLocation_36;
        public BodyJointLocation JointLocation_37;
        public BodyJointLocation JointLocation_38;
        public BodyJointLocation JointLocation_39;
        public BodyJointLocation JointLocation_40;
        public BodyJointLocation JointLocation_41;
        public BodyJointLocation JointLocation_42;
        public BodyJointLocation JointLocation_43;
        public BodyJointLocation JointLocation_44;
        public BodyJointLocation JointLocation_45;
        public BodyJointLocation JointLocation_46;
        public BodyJointLocation JointLocation_47;
        public BodyJointLocation JointLocation_48;
        public BodyJointLocation JointLocation_49;
        public BodyJointLocation JointLocation_50;
        public BodyJointLocation JointLocation_51;
        public BodyJointLocation JointLocation_52;
        public BodyJointLocation JointLocation_53;
        public BodyJointLocation JointLocation_54;
        public BodyJointLocation JointLocation_55;
        public BodyJointLocation JointLocation_56;
        public BodyJointLocation JointLocation_57;
        public BodyJointLocation JointLocation_58;
        public BodyJointLocation JointLocation_59;
        public BodyJointLocation JointLocation_60;
        public BodyJointLocation JointLocation_61;
        public BodyJointLocation JointLocation_62;
        public BodyJointLocation JointLocation_63;
        public BodyJointLocation JointLocation_64;
        public BodyJointLocation JointLocation_65;
        public BodyJointLocation JointLocation_66;
        public BodyJointLocation JointLocation_67;
        public BodyJointLocation JointLocation_68;
        public BodyJointLocation JointLocation_69;
        public BodyJointLocation JointLocation_70;
        public BodyJointLocation JointLocation_71;
        public BodyJointLocation JointLocation_72;
        public BodyJointLocation JointLocation_73;
        public BodyJointLocation JointLocation_74;
        public BodyJointLocation JointLocation_75;
        public BodyJointLocation JointLocation_76;
        public BodyJointLocation JointLocation_77;
        public BodyJointLocation JointLocation_78;
        public BodyJointLocation JointLocation_79;
        public BodyJointLocation JointLocation_80;
        public BodyJointLocation JointLocation_81;
        public BodyJointLocation JointLocation_82;
        public BodyJointLocation JointLocation_83;
        public BodyTrackingCalibrationState CalibrationStatus;
        public BodyTrackingFidelity2 Fidelity;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardState
    {
        public Bool IsActive;
        public Bool OrientationValid;
        public Bool PositionValid;
        public Bool OrientationTracked;
        public Bool PositionTracked;
        public PoseStatef PoseState;
        public Vector4f ContrastParameters;
    }

    public enum KeyboardDescriptionConstants
    {
        NameMaxLength = 128,
    }

    // Enum defining the type of the keyboard model, effect render parameters and passthrough configuration.
    public enum TrackedKeyboardPresentationStyles
    {
        Unknown = 0,
        Opaque = 1,
        MR = 2,
    }

    // Enum defining the type of the keyboard returned
    public enum TrackedKeyboardFlags
    {
        Exists = 1,
        Local = 2,
        Remote = 4,
        Connected = 8,
    }

    // Enum defining the type of the keyboard requested
    public enum TrackedKeyboardQueryFlags
    {
        Local = 2,
        Remote = 4,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardDescription
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)KeyboardDescriptionConstants.NameMaxLength)]
        public byte[] Name;

        public UInt64 TrackedKeyboardId;
        public Vector3f Dimensions;
        public TrackedKeyboardFlags KeyboardFlags;
        public TrackedKeyboardPresentationStyles SupportedPresentationStyles;
    }


    public struct FaceExpressionStatus
    {
        public bool IsValid;
        public bool IsEyeFollowingBlendshapesValid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceState
    {
        public float[] ExpressionWeights;
        public float[] ExpressionWeightConfidences;
        public FaceExpressionStatus Status;
        public FaceTrackingDataSource DataSource;
        public double Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceExpressionStatusInternal
    {
        public Bool IsValid;
        public Bool IsEyeFollowingBlendshapesValid;

        public FaceExpressionStatus ToFaceExpressionStatus() => new FaceExpressionStatus
        {
            IsValid = IsValid == Bool.True,
            IsEyeFollowingBlendshapesValid = IsEyeFollowingBlendshapesValid == Bool.True,
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceStateInternal
    {
        public float ExpressionWeights_0;
        public float ExpressionWeights_1;
        public float ExpressionWeights_2;
        public float ExpressionWeights_3;
        public float ExpressionWeights_4;
        public float ExpressionWeights_5;
        public float ExpressionWeights_6;
        public float ExpressionWeights_7;
        public float ExpressionWeights_8;
        public float ExpressionWeights_9;
        public float ExpressionWeights_10;
        public float ExpressionWeights_11;
        public float ExpressionWeights_12;
        public float ExpressionWeights_13;
        public float ExpressionWeights_14;
        public float ExpressionWeights_15;
        public float ExpressionWeights_16;
        public float ExpressionWeights_17;
        public float ExpressionWeights_18;
        public float ExpressionWeights_19;
        public float ExpressionWeights_20;
        public float ExpressionWeights_21;
        public float ExpressionWeights_22;
        public float ExpressionWeights_23;
        public float ExpressionWeights_24;
        public float ExpressionWeights_25;
        public float ExpressionWeights_26;
        public float ExpressionWeights_27;
        public float ExpressionWeights_28;
        public float ExpressionWeights_29;
        public float ExpressionWeights_30;
        public float ExpressionWeights_31;
        public float ExpressionWeights_32;
        public float ExpressionWeights_33;
        public float ExpressionWeights_34;
        public float ExpressionWeights_35;
        public float ExpressionWeights_36;
        public float ExpressionWeights_37;
        public float ExpressionWeights_38;
        public float ExpressionWeights_39;
        public float ExpressionWeights_40;
        public float ExpressionWeights_41;
        public float ExpressionWeights_42;
        public float ExpressionWeights_43;
        public float ExpressionWeights_44;
        public float ExpressionWeights_45;
        public float ExpressionWeights_46;
        public float ExpressionWeights_47;
        public float ExpressionWeights_48;
        public float ExpressionWeights_49;
        public float ExpressionWeights_50;
        public float ExpressionWeights_51;
        public float ExpressionWeights_52;
        public float ExpressionWeights_53;
        public float ExpressionWeights_54;
        public float ExpressionWeights_55;
        public float ExpressionWeights_56;
        public float ExpressionWeights_57;
        public float ExpressionWeights_58;
        public float ExpressionWeights_59;
        public float ExpressionWeights_60;
        public float ExpressionWeights_61;
        public float ExpressionWeights_62;
        public float ExpressionWeightConfidences_0;
        public float ExpressionWeightConfidences_1;
        public FaceExpressionStatusInternal Status;
        public double Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceState2Internal
    {
        public float ExpressionWeights_0;
        public float ExpressionWeights_1;
        public float ExpressionWeights_2;
        public float ExpressionWeights_3;
        public float ExpressionWeights_4;
        public float ExpressionWeights_5;
        public float ExpressionWeights_6;
        public float ExpressionWeights_7;
        public float ExpressionWeights_8;
        public float ExpressionWeights_9;
        public float ExpressionWeights_10;
        public float ExpressionWeights_11;
        public float ExpressionWeights_12;
        public float ExpressionWeights_13;
        public float ExpressionWeights_14;
        public float ExpressionWeights_15;
        public float ExpressionWeights_16;
        public float ExpressionWeights_17;
        public float ExpressionWeights_18;
        public float ExpressionWeights_19;
        public float ExpressionWeights_20;
        public float ExpressionWeights_21;
        public float ExpressionWeights_22;
        public float ExpressionWeights_23;
        public float ExpressionWeights_24;
        public float ExpressionWeights_25;
        public float ExpressionWeights_26;
        public float ExpressionWeights_27;
        public float ExpressionWeights_28;
        public float ExpressionWeights_29;
        public float ExpressionWeights_30;
        public float ExpressionWeights_31;
        public float ExpressionWeights_32;
        public float ExpressionWeights_33;
        public float ExpressionWeights_34;
        public float ExpressionWeights_35;
        public float ExpressionWeights_36;
        public float ExpressionWeights_37;
        public float ExpressionWeights_38;
        public float ExpressionWeights_39;
        public float ExpressionWeights_40;
        public float ExpressionWeights_41;
        public float ExpressionWeights_42;
        public float ExpressionWeights_43;
        public float ExpressionWeights_44;
        public float ExpressionWeights_45;
        public float ExpressionWeights_46;
        public float ExpressionWeights_47;
        public float ExpressionWeights_48;
        public float ExpressionWeights_49;
        public float ExpressionWeights_50;
        public float ExpressionWeights_51;
        public float ExpressionWeights_52;
        public float ExpressionWeights_53;
        public float ExpressionWeights_54;
        public float ExpressionWeights_55;
        public float ExpressionWeights_56;
        public float ExpressionWeights_57;
        public float ExpressionWeights_58;
        public float ExpressionWeights_59;
        public float ExpressionWeights_60;
        public float ExpressionWeights_61;
        public float ExpressionWeights_62;
        public float ExpressionWeights_63;
        public float ExpressionWeights_64;
        public float ExpressionWeights_65;
        public float ExpressionWeights_66;
        public float ExpressionWeights_67;
        public float ExpressionWeights_68;
        public float ExpressionWeights_69;
        public float ExpressionWeightConfidences_0;
        public float ExpressionWeightConfidences_1;
        public FaceExpressionStatusInternal Status;
        public FaceTrackingDataSource DataSource;
        public double Time;
    }

    public enum FaceRegionConfidence
    {
        Lower = 0,
        Upper = 1,
        Max = 2,
    }

    public enum FaceExpression
    {
        Invalid = -1,
        Brow_Lowerer_L = 0,
        Brow_Lowerer_R = 1,
        Cheek_Puff_L = 2,
        Cheek_Puff_R = 3,
        Cheek_Raiser_L = 4,
        Cheek_Raiser_R = 5,
        Cheek_Suck_L = 6,
        Cheek_Suck_R = 7,
        Chin_Raiser_B = 8,
        Chin_Raiser_T = 9,
        Dimpler_L = 10,
        Dimpler_R = 11,
        Eyes_Closed_L = 12,
        Eyes_Closed_R = 13,
        Eyes_Look_Down_L = 14,
        Eyes_Look_Down_R = 15,
        Eyes_Look_Left_L = 16,
        Eyes_Look_Left_R = 17,
        Eyes_Look_Right_L = 18,
        Eyes_Look_Right_R = 19,
        Eyes_Look_Up_L = 20,
        Eyes_Look_Up_R = 21,
        Inner_Brow_Raiser_L = 22,
        Inner_Brow_Raiser_R = 23,
        Jaw_Drop = 24,
        Jaw_Sideways_Left = 25,
        Jaw_Sideways_Right = 26,
        Jaw_Thrust = 27,
        Lid_Tightener_L = 28,
        Lid_Tightener_R = 29,
        Lip_Corner_Depressor_L = 30,
        Lip_Corner_Depressor_R = 31,
        Lip_Corner_Puller_L = 32,
        Lip_Corner_Puller_R = 33,
        Lip_Funneler_LB = 34,
        Lip_Funneler_LT = 35,
        Lip_Funneler_RB = 36,
        Lip_Funneler_RT = 37,
        Lip_Pressor_L = 38,
        Lip_Pressor_R = 39,
        Lip_Pucker_L = 40,
        Lip_Pucker_R = 41,
        Lip_Stretcher_L = 42,
        Lip_Stretcher_R = 43,
        Lip_Suck_LB = 44,
        Lip_Suck_LT = 45,
        Lip_Suck_RB = 46,
        Lip_Suck_RT = 47,
        Lip_Tightener_L = 48,
        Lip_Tightener_R = 49,
        Lips_Toward = 50,
        Lower_Lip_Depressor_L = 51,
        Lower_Lip_Depressor_R = 52,
        Mouth_Left = 53,
        Mouth_Right = 54,
        Nose_Wrinkler_L = 55,
        Nose_Wrinkler_R = 56,
        Outer_Brow_Raiser_L = 57,
        Outer_Brow_Raiser_R = 58,
        Upper_Lid_Raiser_L = 59,
        Upper_Lid_Raiser_R = 60,
        Upper_Lip_Raiser_L = 61,
        Upper_Lip_Raiser_R = 62,
        Max = 63,
    }

    public enum FaceExpression2
    {
        Invalid = -1,
        Brow_Lowerer_L = 0,
        Brow_Lowerer_R = 1,
        Cheek_Puff_L = 2,
        Cheek_Puff_R = 3,
        Cheek_Raiser_L = 4,
        Cheek_Raiser_R = 5,
        Cheek_Suck_L = 6,
        Cheek_Suck_R = 7,
        Chin_Raiser_B = 8,
        Chin_Raiser_T = 9,
        Dimpler_L = 10,
        Dimpler_R = 11,
        Eyes_Closed_L = 12,
        Eyes_Closed_R = 13,
        Eyes_Look_Down_L = 14,
        Eyes_Look_Down_R = 15,
        Eyes_Look_Left_L = 16,
        Eyes_Look_Left_R = 17,
        Eyes_Look_Right_L = 18,
        Eyes_Look_Right_R = 19,
        Eyes_Look_Up_L = 20,
        Eyes_Look_Up_R = 21,
        Inner_Brow_Raiser_L = 22,
        Inner_Brow_Raiser_R = 23,
        Jaw_Drop = 24,
        Jaw_Sideways_Left = 25,
        Jaw_Sideways_Right = 26,
        Jaw_Thrust = 27,
        Lid_Tightener_L = 28,
        Lid_Tightener_R = 29,
        Lip_Corner_Depressor_L = 30,
        Lip_Corner_Depressor_R = 31,
        Lip_Corner_Puller_L = 32,
        Lip_Corner_Puller_R = 33,
        Lip_Funneler_LB = 34,
        Lip_Funneler_LT = 35,
        Lip_Funneler_RB = 36,
        Lip_Funneler_RT = 37,
        Lip_Pressor_L = 38,
        Lip_Pressor_R = 39,
        Lip_Pucker_L = 40,
        Lip_Pucker_R = 41,
        Lip_Stretcher_L = 42,
        Lip_Stretcher_R = 43,
        Lip_Suck_LB = 44,
        Lip_Suck_LT = 45,
        Lip_Suck_RB = 46,
        Lip_Suck_RT = 47,
        Lip_Tightener_L = 48,
        Lip_Tightener_R = 49,
        Lips_Toward = 50,
        Lower_Lip_Depressor_L = 51,
        Lower_Lip_Depressor_R = 52,
        Mouth_Left = 53,
        Mouth_Right = 54,
        Nose_Wrinkler_L = 55,
        Nose_Wrinkler_R = 56,
        Outer_Brow_Raiser_L = 57,
        Outer_Brow_Raiser_R = 58,
        Upper_Lid_Raiser_L = 59,
        Upper_Lid_Raiser_R = 60,
        Upper_Lip_Raiser_L = 61,
        Upper_Lip_Raiser_R = 62,
        Tongue_Tip_Interdental = 63,
        Tongue_Tip_Alveolar = 64,
        Tongue_Front_Dorsal_Palate = 65,
        Tongue_Mid_Dorsal_Palate = 66,
        Tongue_Back_Dorsal_Velar = 67,
        Tongue_Out = 68,
        Tongue_Retreat = 69,
        Max = 70,
    }

    public enum FaceTrackingDataSource
    {
        Visual = 0,
        Audio = 1,
        Count = 2,
    }

    public enum FaceConstants
    {
        MaxFaceExpressions = FaceExpression.Max,
        MaxFaceRegionConfidences = FaceRegionConfidence.Max,
        MaxFaceExpressions2 = FaceExpression2.Max
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EyeGazeState
    {
        public Posef Pose;
        public float Confidence;
        internal Bool _isValid;
        public bool IsValid => _isValid == Bool.True;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EyeGazesState
    {
        public EyeGazeState[] EyeGazes;
        public double Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EyeGazesStateInternal
    {
        public EyeGazeState EyeGazes_0;
        public EyeGazeState EyeGazes_1;
        public double Time;
    }



    public enum ColorSpace
    {
        /// The default value from GetHmdColorSpace until SetClientColorDesc is called. Only valid on PC, and will be remapped to Quest on Mobile
        Unknown = 0,

        /// No color correction, not recommended for production use. See documentation for more info
        Unmanaged = 1,

        /// Preferred color space for standardized color across all Oculus HMDs with D65 white point
        Rec_2020 = 2,

        /// Rec. 709 is used on Oculus Go and shares the same primary color coordinates as sRGB
        Rec_709 = 3,

        /// Oculus Rift CV1 uses a unique color space, see documentation for more info
        Rift_CV1 = 4,

        /// Oculus Rift S uses a unique color space, see documentation for more info
        Rift_S = 5,

        /// Oculus Quest's native color space is slightly different than Rift CV1
        Quest = 6,

        /// Similar to DCI-P3. See documentation for more details on P3
        P3 = 7,

        /// Similar to sRGB but with deeper greens using D65 white point
        Adobe_RGB = 8,
    }

    public enum EventType
    {
        Unknown = 0,
        DisplayRefreshRateChanged = 1,

        SpatialAnchorCreateComplete = 49,
        SpaceSetComponentStatusComplete = 50,
        SpaceQueryResults = 51,
        SpaceQueryComplete = 52,
        SpaceSaveComplete = 53,
        SpaceEraseComplete = 54,
        SpaceShareResult = 56,
        SpaceListSaveResult = 57,

        SceneCaptureComplete = 100,


        VirtualKeyboardCommitText = 201,
        VirtualKeyboardBackspace = 202,
        VirtualKeyboardEnter = 203,
        VirtualKeyboardShown = 204,
        VirtualKeyboardHidden = 205,


    }

    private const int EventDataBufferSize = 4000;

    [StructLayout(LayoutKind.Sequential)]
    public struct EventDataBuffer
    {
        public EventType EventType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = EventDataBufferSize)]
        public byte[] EventData;
    }

    public const int RENDER_MODEL_NULL_KEY = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderModelProperties
    {
        public string ModelName;
        public UInt64 ModelKey;
        public uint VendorId;
        public uint ModelVersion;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderModelPropertiesInternal
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = OVRP_1_68_0.OVRP_RENDER_MODEL_MAX_NAME_LENGTH)]
        public byte[] ModelName;

        public UInt64 ModelKey;
        public uint VendorId;
        public uint ModelVersion;
    }

    [Flags]
    public enum RenderModelFlags
    {
        SupportsGltf20Subset1 = 1,
        SupportsGltf20Subset2 = 2,
    }

    public enum VirtualKeyboardLocationType
    {
        Custom = 0,
        Far = 1,
        Direct = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardSpaceCreateInfo
    {
        public VirtualKeyboardLocationType locationType;

        // Pose only set if locationType == Custom
        public Posef pose;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardLocationInfo
    {
        public VirtualKeyboardLocationType locationType;

        // Pose & Scale only set if locationType == Custom
        public Posef pose;
        public float scale;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardCreateInfo
    {
    }

    public enum VirtualKeyboardInputSource
    {
        Invalid = 0,
        ControllerRayLeft = 1,
        ControllerRayRight = 2,
        HandRayLeft = 3,
        HandRayRight = 4,
        ControllerDirectLeft = 5,
        ControllerDirectRight = 6,
        HandDirectIndexTipLeft = 7,
        HandDirectIndexTipRight = 8,
        EnumSize = 0x7FFFFFFF
    }

    [Flags]
    public enum VirtualKeyboardInputStateFlags : ulong
    {
        IsPressed = 0x0000000000000001,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardInputInfo
    {
        public VirtualKeyboardInputSource inputSource;
        public Posef inputPose;
        public VirtualKeyboardInputStateFlags inputState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardModelAnimationState
    {
        public int AnimationIndex;
        public float Fraction;
    }

    public struct VirtualKeyboardModelAnimationStates
    {
        public VirtualKeyboardModelAnimationState[] States;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardModelAnimationStatesInternal
    {
        public uint StateCapacityInput;
        public uint StateCountOutput;
        public IntPtr StatesBuffer;
    }

    public struct VirtualKeyboardTextureIds
    {
        public UInt64[] TextureIds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardTextureIdsInternal
    {
        public uint TextureIdCapacityInput;
        public uint TextureIdCountOutput;
        public IntPtr TextureIdsBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardTextureData
    {
        public uint TextureWidth;
        public uint TextureHeight;
        public uint BufferCapacityInput;
        public uint BufferCountOutput;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VirtualKeyboardModelVisibility
    {
        internal Bool _visible;

        public bool Visible
        {
            get => _visible == Bool.True;
            set => _visible = (value) ? Bool.True : Bool.False;
        }
    }


    public enum InsightPassthroughColorMapType
    {
        None = 0,
        MonoToRgba = 1,
        MonoToMono = 2,
        BrightnessContrastSaturation = 4,
        ColorLut = 6,
        InterpolatedColorLut = 7
    }

    public enum InsightPassthroughStyleFlags
    {
        HasTextureOpacityFactor = 1 << 0,
        HasEdgeColor = 1 << 1,
        HasTextureColorMap = 1 << 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InsightPassthroughStyle
    {
        public InsightPassthroughStyleFlags Flags;
        public float TextureOpacityFactor;
        public Colorf EdgeColor;
        public InsightPassthroughColorMapType TextureColorMapType;
        public uint TextureColorMapDataSize;
        public IntPtr TextureColorMapData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InsightPassthroughStyle2
    {
        public InsightPassthroughStyleFlags Flags;
        public float TextureOpacityFactor;
        public Colorf EdgeColor;
        public InsightPassthroughColorMapType TextureColorMapType;
        public uint TextureColorMapDataSize;
        public IntPtr TextureColorMapData;
        public UInt64 LutSource;
        public UInt64 LutTarget;
        public float LutWeight;

        public void CopyTo(ref InsightPassthroughStyle target)
        {
            target.Flags = Flags;
            target.TextureOpacityFactor = TextureOpacityFactor;
            target.EdgeColor = EdgeColor;
            target.TextureColorMapType = TextureColorMapType;
            target.TextureColorMapDataSize = TextureColorMapDataSize;
            target.TextureColorMapData = TextureColorMapData;
        }
    }

    public enum PassthroughColorLutChannels
    {
        Rgb = 1,
        Rgba = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PassthroughColorLutData
    {
        public uint BufferSize;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InsightPassthroughKeyboardHandsIntensity
    {
        public float LeftHandIntensity;
        public float RightHandIntensity;
    }

    public enum PassthroughCapabilityFlags
    {
        Passthrough = 1 << 0,
        Color = 1 << 1,
        Depth = 1 << 2
    }

    public enum PassthroughCapabilityFields
    {
        Flags = 1 << 0,
        MaxColorLutResolution = 1 << 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PassthroughCapabilities
    {
        public PassthroughCapabilityFields Fields;
        public PassthroughCapabilityFlags Flags;
        public uint MaxColorLutResolution;
    }

    public enum SpaceComponentType
    {
        Locatable = 0,
        Storable = 1,
        Sharable = 2,
        Bounded2D = 3,
        Bounded3D = 4,
        SemanticLabels = 5,
        RoomLayout = 6,
        SpaceContainer = 7,
        TriangleMesh = 1000269000,
    }

    public enum SpaceStorageLocation
    {
        Invalid = 0,
        Local = 1,
        Cloud = 2,
    }

    public enum SpaceStoragePersistenceMode
    {
        Invalid = 0,
        Indefinite = 1
    }

    public enum SpaceQueryActionType
    {
        Load = 0,
    }

    public enum SpaceQueryType
    {
        Action = 0
    }

    public enum SpaceQueryFilterType
    {
        None = 0,
        Ids = 1,
        Components = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpatialAnchorCreateInfo
    {
        public TrackingOrigin BaseTracking;
        public Posef PoseInSpace;
        public double Time;
    }


    public const int SpaceFilterInfoIdsMaxSize = 1024;

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceFilterInfoIds
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SpaceFilterInfoIdsMaxSize)]
        public Guid[] Ids;

        public int NumIds;
    }

    public const int SpaceFilterInfoComponentsMaxSize = 16;

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceFilterInfoComponents
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SpaceFilterInfoComponentsMaxSize)]
        public SpaceComponentType[] Components;

        public int NumComponents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceQueryInfo
    {
        public SpaceQueryType QueryType;
        public int MaxQuerySpaces;
        public double Timeout;
        public SpaceStorageLocation Location;
        public SpaceQueryActionType ActionType;
        public SpaceQueryFilterType FilterType;
        public SpaceFilterInfoIds IdInfo;
        public SpaceFilterInfoComponents ComponentsInfo;
    }


    public const int SpatialEntityMaxQueryResultsPerEvent = 128;

    [StructLayout(LayoutKind.Sequential)]
    public struct SpaceQueryResult
    {
        public UInt64 space;
        public Guid uuid;
    }


    public static string GuidToUuidString(Guid guid)
    {
        const int GUID_BYTE_LENGTH = 36;

        byte[] guidBytes = guid.ToByteArray();
        string unformattedUuid = BitConverter.ToString(guidBytes).Replace("-", "").ToLower();
        var formattedUuid = new System.Text.StringBuilder(GUID_BYTE_LENGTH);
        for (var i = 0; i < 32; i++)
        {
            formattedUuid.Append(unformattedUuid[i]);

            if (i == 7 || i == 11 || i == 15 || i == 19)
            {
                formattedUuid.Append("-");
            }
        }

        return formattedUuid.ToString();
    }



    private const string pluginName = "OVRPlugin";
    private static readonly System.Version _versionZero = new System.Version(0, 0, 0);

    // Disable all the DllImports when the platform is not supported
#if !OVRPLUGIN_UNSUPPORTED_PLATFORM

    public static class OVRP_0_1_0
    {
        public static readonly System.Version version = new System.Version(0, 1, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Sizei ovrp_GetEyeTextureSize(Eye eyeId);
    }

    public static class OVRP_0_1_1
    {
        public static readonly System.Version version = new System.Version(0, 1, 1);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetOverlayQuad2(Bool onTop, Bool headLocked, IntPtr texture, IntPtr device,
            Posef pose, Vector3f scale);
    }

    public static class OVRP_0_1_2
    {
        public static readonly System.Version version = new System.Version(0, 1, 2);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodePose(Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetControllerVibration(uint controllerMask, float frequency, float amplitude);
    }

    public static class OVRP_0_1_3
    {
        public static readonly System.Version version = new System.Version(0, 1, 3);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodeVelocity(Node nodeId);

        [System.Obsolete("Deprecated. Acceleration is not supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodeAcceleration(Node nodeId);
    }

    public static class OVRP_0_5_0
    {
        public static readonly System.Version version = new System.Version(0, 5, 0);
    }

    public static class OVRP_1_0_0
    {
        public static readonly System.Version version = new System.Version(1, 0, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern TrackingOrigin ovrp_GetTrackingOriginType();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingOriginType(TrackingOrigin originType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetTrackingCalibratedOrigin();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_RecenterTrackingOrigin(uint flags);
    }

    public static class OVRP_1_1_0
    {
        public static readonly System.Version version = new System.Version(1, 1, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetInitialized();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetVersion")]
        private static extern IntPtr _ovrp_GetVersion();
        public static string ovrp_GetVersion() { return Marshal.PtrToStringAnsi(OVRP_1_1_0._ovrp_GetVersion()); }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetNativeSDKVersion")]
        private static extern IntPtr _ovrp_GetNativeSDKVersion();
        public static string ovrp_GetNativeSDKVersion() { return Marshal.PtrToStringAnsi(OVRP_1_1_0._ovrp_GetNativeSDKVersion()); }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrp_GetAudioOutId();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrp_GetAudioInId();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetEyeTextureScale();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetEyeTextureScale(float value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetTrackingOrientationSupported();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetTrackingOrientationEnabled();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingOrientationEnabled(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetTrackingPositionSupported();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetTrackingPositionEnabled();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingPositionEnabled(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodePresent(Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodeOrientationTracked(Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetNodePositionTracked(Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Frustumf ovrp_GetNodeFrustum(Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ControllerState ovrp_GetControllerState(uint controllerMask);

        [System.Obsolete("Deprecated. Replaced by ovrp_GetSuggestedCpuPerformanceLevel", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ovrp_GetSystemCpuLevel();

        [System.Obsolete("Deprecated. Replaced by ovrp_SetSuggestedCpuPerformanceLevel", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetSystemCpuLevel(int value);

        [System.Obsolete("Deprecated. Replaced by ovrp_GetSuggestedGpuPerformanceLevel", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ovrp_GetSystemGpuLevel();

        [System.Obsolete("Deprecated. Replaced by ovrp_SetSuggestedGpuPerformanceLevel", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetSystemGpuLevel(int value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetSystemPowerSavingMode();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetSystemDisplayFrequency();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ovrp_GetSystemVSyncCount();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetSystemVolume();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern BatteryStatus ovrp_GetSystemBatteryStatus();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetSystemBatteryLevel();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetSystemBatteryTemperature();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetSystemProductName")]
        private static extern IntPtr _ovrp_GetSystemProductName();
        public static string ovrp_GetSystemProductName() { return Marshal.PtrToStringAnsi(OVRP_1_1_0._ovrp_GetSystemProductName()); }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_ShowSystemUI(PlatformUI ui);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppMonoscopic();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetAppMonoscopic(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppHasVrFocus();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppShouldQuit();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppShouldRecenter();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovrp_GetAppLatencyTimings")]
        private static extern IntPtr _ovrp_GetAppLatencyTimings();
        public static string ovrp_GetAppLatencyTimings() { return Marshal.PtrToStringAnsi(OVRP_1_1_0._ovrp_GetAppLatencyTimings()); }

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetUserPresent();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetUserIPD();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetUserIPD(float value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetUserEyeDepth();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetUserEyeDepth(float value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetUserEyeHeight();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetUserEyeHeight(float value);
    }

    public static class OVRP_1_2_0
    {
        public static readonly System.Version version = new System.Version(1, 2, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetSystemVSyncCount(int vsyncCount);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrpi_SetTrackingCalibratedOrigin();
    }

    public static class OVRP_1_3_0
    {
        public static readonly System.Version version = new System.Version(1, 3, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetEyeOcclusionMeshEnabled();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetEyeOcclusionMeshEnabled(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetSystemHeadphonesPresent();
    }

    public static class OVRP_1_5_0
    {
        public static readonly System.Version version = new System.Version(1, 5, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern SystemRegion ovrp_GetSystemRegion();
    }

    public static class OVRP_1_6_0
    {
        public static readonly System.Version version = new System.Version(1, 6, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetTrackingIPDEnabled();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetTrackingIPDEnabled(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern HapticsDesc ovrp_GetControllerHapticsDesc(uint controllerMask);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern HapticsState ovrp_GetControllerHapticsState(uint controllerMask);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetControllerHaptics(uint controllerMask, HapticsBuffer hapticsBuffer);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetOverlayQuad3(uint flags, IntPtr textureLeft, IntPtr textureRight,
            IntPtr device, Posef pose, Vector3f scale, int layerIndex);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetEyeRecommendedResolutionScale();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetAppCpuStartToGpuEndTime();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ovrp_GetSystemRecommendedMSAALevel();
    }

    public static class OVRP_1_7_0
    {
        public static readonly System.Version version = new System.Version(1, 7, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetAppChromaticCorrection();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetAppChromaticCorrection(Bool value);
    }

    public static class OVRP_1_8_0
    {
        public static readonly System.Version version = new System.Version(1, 8, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetBoundaryConfigured();

        [System.Obsolete("Deprecated. This function will not be supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern BoundaryTestResult ovrp_TestBoundaryNode(Node nodeId, BoundaryType boundaryType);

        [System.Obsolete("Deprecated. This function will not be supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern BoundaryTestResult ovrp_TestBoundaryPoint(Vector3f point, BoundaryType boundaryType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern BoundaryGeometry ovrp_GetBoundaryGeometry(BoundaryType boundaryType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3f ovrp_GetBoundaryDimensions(BoundaryType boundaryType);

        [System.Obsolete("Deprecated. This function will not be supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetBoundaryVisible();

        [System.Obsolete("Deprecated. This function will not be supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetBoundaryVisible(Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_Update2(int stateId, int frameIndex, double predictionSeconds);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodePose2(int stateId, Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodeVelocity2(int stateId, Node nodeId);

        [System.Obsolete("Deprecated. Acceleration is not supported in OpenXR", false)]
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Posef ovrp_GetNodeAcceleration2(int stateId, Node nodeId);
    }

    public static class OVRP_1_9_0
    {
        public static readonly System.Version version = new System.Version(1, 9, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern SystemHeadset ovrp_GetSystemHeadsetType();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Controller ovrp_GetActiveController();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Controller ovrp_GetConnectedControllers();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetBoundaryGeometry2(BoundaryType boundaryType, IntPtr points,
            ref int pointsCount);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern AppPerfStats ovrp_GetAppPerfStats();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_ResetAppPerfStats();
    }

    public static class OVRP_1_10_0
    {
        public static readonly System.Version version = new System.Version(1, 10, 0);
    }

    public static class OVRP_1_11_0
    {
        public static readonly System.Version version = new System.Version(1, 11, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_SetDesiredEyeTextureFormat(EyeTextureFormat value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern EyeTextureFormat ovrp_GetDesiredEyeTextureFormat();
    }

    public static class OVRP_1_12_0
    {
        public static readonly System.Version version = new System.Version(1, 12, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ovrp_GetAppFramerate();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern PoseStatef ovrp_GetNodePoseState(Step stepId, Node nodeId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ControllerState2 ovrp_GetControllerState2(uint controllerMask);
    }

    public static class OVRP_1_15_0
    {
        public static readonly System.Version version = new System.Version(1, 15, 0);

        public const int OVRP_EXTERNAL_CAMERA_NAME_SIZE = 32;

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_InitializeMixedReality();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ShutdownMixedReality();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetMixedRealityInitialized();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_UpdateExternalCamera();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetExternalCameraCount(out int cameraCount);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetExternalCameraName(int cameraId,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = OVRP_EXTERNAL_CAMERA_NAME_SIZE)]
            char[] cameraName);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetExternalCameraIntrinsics(int cameraId,
            out CameraIntrinsics cameraIntrinsics);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetExternalCameraExtrinsics(int cameraId,
            out CameraExtrinsics cameraExtrinsics);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CalculateLayerDesc(OverlayShape shape, LayerLayout layout,
            ref Sizei textureSize,
            int mipLevels, int sampleCount, EyeTextureFormat format, int layerFlags, ref LayerDesc layerDesc);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EnqueueSetupLayer(ref LayerDesc desc, IntPtr layerId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EnqueueDestroyLayer(IntPtr layerId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLayerTextureStageCount(int layerId, ref int layerTextureStageCount);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLayerTexturePtr(int layerId, int stage, Eye eyeId,
            ref IntPtr textureHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EnqueueSubmitLayer(uint flags, IntPtr textureLeft, IntPtr textureRight,
            int layerId, int frameIndex, ref Posef pose, ref Vector3f scale, int layerIndex);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodeFrustum2(Node nodeId, out Frustumf2 nodeFrustum);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetEyeTextureArrayEnabled();
    }

    public static class OVRP_1_16_0
    {
        public static readonly System.Version version = new System.Version(1, 16, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_UpdateCameraDevices();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_IsCameraDeviceAvailable(CameraDevice cameraDevice);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetCameraDevicePreferredColorFrameSize(CameraDevice cameraDevice,
            Sizei preferredColorFrameSize);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_OpenCameraDevice(CameraDevice cameraDevice);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CloseCameraDevice(CameraDevice cameraDevice);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_HasCameraDeviceOpened(CameraDevice cameraDevice);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_IsCameraDeviceColorFrameAvailable(CameraDevice cameraDevice);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceColorFrameSize(CameraDevice cameraDevice,
            out Sizei colorFrameSize);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceColorFrameBgraPixels(CameraDevice cameraDevice,
            out IntPtr colorFrameBgraPixels, out int colorFrameRowPitch);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerState4(uint controllerMask, ref ControllerState4 controllerState);
    }

    public static class OVRP_1_17_0
    {
        public static readonly System.Version version = new System.Version(1, 17, 0);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || OVRPLUGIN_EDITOR_MOCK_ENABLED
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetExternalCameraPose(CameraDevice camera, out Posef cameraPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ConvertPoseToCameraSpace(CameraDevice camera, ref Posef trackingSpacePose,
            out Posef cameraSpacePose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceIntrinsicsParameters(CameraDevice camera,
            out Bool supportIntrinsics, out CameraDeviceIntrinsicsParameters intrinsicsParameters);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DoesCameraDeviceSupportDepth(CameraDevice camera, out Bool supportDepth);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceDepthSensingMode(CameraDevice camera,
            out CameraDeviceDepthSensingMode depthSensoringMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetCameraDeviceDepthSensingMode(CameraDevice camera,
            CameraDeviceDepthSensingMode depthSensoringMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDevicePreferredDepthQuality(CameraDevice camera,
            out CameraDeviceDepthQuality depthQuality);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetCameraDevicePreferredDepthQuality(CameraDevice camera,
            CameraDeviceDepthQuality depthQuality);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_IsCameraDeviceDepthFrameAvailable(CameraDevice camera, out Bool available);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceDepthFrameSize(CameraDevice camera, out Sizei depthFrameSize);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceDepthFramePixels(CameraDevice cameraDevice,
            out IntPtr depthFramePixels, out int depthFrameRowPitch);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCameraDeviceDepthConfidencePixels(CameraDevice cameraDevice,
            out IntPtr depthConfidencePixels, out int depthConfidenceRowPitch);
#endif
    }

    public static class OVRP_1_18_0
    {
        public static readonly System.Version version = new System.Version(1, 18, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetHandNodePoseStateLatency(double latencyInSeconds);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetHandNodePoseStateLatency(out double latencyInSeconds);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetAppHasInputFocus(out Bool appHasInputFocus);
    }

    public static class OVRP_1_19_0
    {
        public static readonly System.Version version = new System.Version(1, 19, 0);
    }

    public static class OVRP_1_21_0
    {
        public static readonly System.Version version = new System.Version(1, 21, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTiledMultiResSupported(out Bool foveationSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTiledMultiResLevel(out FoveatedRenderingLevel level);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetTiledMultiResLevel(FoveatedRenderingLevel level);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetGPUUtilSupported(out Bool gpuUtilSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetGPUUtilLevel(out float gpuUtil);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSystemDisplayFrequency2(out float systemDisplayFrequency);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSystemDisplayAvailableFrequencies(IntPtr systemDisplayAvailableFrequencies,
            ref int numFrequencies);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetSystemDisplayFrequency(float requestedFrequency);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetAppAsymmetricFov(out Bool useAsymmetricFov);
    }

    public static class OVRP_1_28_0
    {
        public static readonly System.Version version = new System.Version(1, 28, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetDominantHand(out Handedness dominantHand);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SendEvent(string name, string param);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern Result ovrp_EnqueueSetupLayer2(ref LayerDesc desc, int compositionDepth,
            int* layerId);

    }

    public static class OVRP_1_29_0
    {
        public static readonly System.Version version = new System.Version(1, 29, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLayerAndroidSurfaceObject(int layerId, ref IntPtr surfaceObject);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetHeadPoseModifier(ref Quatf relativeRotation,
            ref Vector3f relativeTranslation);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetHeadPoseModifier(out Quatf relativeRotation,
            out Vector3f relativeTranslation);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodePoseStateRaw(Step stepId, int frameIndex, Node nodeId,
            out PoseStatef nodePoseState);
    }

    public static class OVRP_1_30_0
    {
        public static readonly System.Version version = new System.Version(1, 30, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCurrentTrackingTransformPose(out Posef trackingTransformPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTrackingTransformRawPose(out Posef trackingTransformRawPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SendEvent2(string name, string param, string source);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_IsPerfMetricsSupported(PerfMetrics perfMetrics, out Bool isSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetPerfMetricsFloat(PerfMetrics perfMetrics, out float value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetPerfMetricsInt(PerfMetrics perfMetrics, out int value);
    }

    public static class OVRP_1_31_0
    {
        public static readonly System.Version version = new System.Version(1, 31, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTimeInSeconds(out double value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetColorScaleAndOffset(Vector4f colorScale, Vector4f colorOffset,
            Bool applyToAllLayers);

    }

    public static class OVRP_1_32_0
    {
        public static readonly System.Version version = new System.Version(1, 32, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_AddCustomMetadata(string name, string param);
    }

    public static class OVRP_1_34_0
    {
        public static readonly System.Version version = new System.Version(1, 34, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EnqueueSubmitLayer2(uint flags, IntPtr textureLeft, IntPtr textureRight,
            int layerId, int frameIndex, ref Posef pose, ref Vector3f scale, int layerIndex,
            Bool overrideTextureRectMatrix, ref TextureRectMatrixf textureRectMatrix,
            Bool overridePerLayerColorScaleAndOffset, ref Vector4f colorScale, ref Vector4f colorOffset);
    }

    public static class OVRP_1_35_0
    {
        public static readonly System.Version version = new System.Version(1, 35, 0);
    }

    public static class OVRP_1_36_0
    {
        public static readonly System.Version version = new System.Version(1, 36, 0);
    }

    public static class OVRP_1_37_0
    {
        public static readonly System.Version version = new System.Version(1, 37, 0);
    }

    public static class OVRP_1_38_0
    {
        public static readonly System.Version version = new System.Version(1, 38, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTrackingTransformRelativePose(ref Posef trackingTransformRelativePose,
            TrackingOrigin trackingOrigin);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_Initialize();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_Shutdown();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetInitialized(out Bool initialized);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_Update();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetMrcActivationMode(out Media.MrcActivationMode activationMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetMrcActivationMode(Media.MrcActivationMode activationMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_IsMrcEnabled(out Bool mrcEnabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_IsMrcActivated(out Bool mrcActivated);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_UseMrcDebugCamera(out Bool useMrcDebugCamera);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetMrcInputVideoBufferType(
            Media.InputVideoBufferType inputVideoBufferType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetMrcInputVideoBufferType(
            ref Media.InputVideoBufferType inputVideoBufferType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetMrcFrameSize(int frameWidth, int frameHeight);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetMrcFrameSize(ref int frameWidth, ref int frameHeight);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetMrcAudioSampleRate(int sampleRate);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetMrcAudioSampleRate(ref int sampleRate);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetMrcFrameImageFlipped(Bool flipped);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetMrcFrameImageFlipped(ref Bool flipped);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_EncodeMrcFrame(System.IntPtr rawBuffer, System.IntPtr audioDataPtr,
            int audioDataLen, int audioChannels, double timestamp, ref int outSyncId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_EncodeMrcFrameWithDualTextures(System.IntPtr backgroundTextureHandle,
            System.IntPtr foregroundTextureHandle, System.IntPtr audioData, int audioDataLen, int audioChannels,
            double timestamp, ref int outSyncId);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SyncMrcFrame(int syncId);


        //[DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        //public static extern Result ovrp_GetExternalCameraCalibrationRawPose(int cameraId, out Posef rawPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetDeveloperMode(Bool active);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodeOrientationValid(Node nodeId, ref Bool nodeOrientationValid);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodePositionValid(Node nodeId, ref Bool nodePositionValid);
    }

    public static class OVRP_1_39_0
    {
        public static readonly System.Version version = new System.Version(1, 39, 0);
    }

    public static class OVRP_1_40_0
    {
        public static readonly System.Version version = new System.Version(1, 40, 0);
    }

    public static class OVRP_1_41_0
    {
        public static readonly System.Version version = new System.Version(1, 41, 0);
    }

    public static class OVRP_1_42_0
    {
        public static readonly System.Version version = new System.Version(1, 42, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetAdaptiveGpuPerformanceScale2(ref float adaptiveGpuPerformanceScale);
    }

    public static class OVRP_1_43_0
    {
        public static readonly System.Version version = new System.Version(1, 43, 0);
    }

    public static class OVRP_1_44_0
    {
        public static readonly System.Version version = new System.Version(1, 44, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetHandTrackingEnabled(ref Bool handTrackingEnabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetHandState(Step stepId, Hand hand, out HandStateInternal handState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSkeleton(SkeletonType skeletonType, out Skeleton skeleton);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetMesh(MeshType meshType, System.IntPtr meshPtr);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_OverrideExternalCameraFov(int cameraId, Bool useOverriddenFov, ref Fovf fov);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetUseOverriddenExternalCameraFov(int cameraId, out Bool useOverriddenFov);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_OverrideExternalCameraStaticPose(int cameraId, Bool useOverriddenPose,
            ref Posef poseInStageOrigin);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetUseOverriddenExternalCameraStaticPose(int cameraId,
            out Bool useOverriddenStaticPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ResetDefaultExternalCamera();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetDefaultExternalCamera(string cameraName,
            ref CameraIntrinsics cameraIntrinsics, ref CameraExtrinsics cameraExtrinsics);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLocalTrackingSpaceRecenterCount(ref int recenterCount);
    }

    public static class OVRP_1_45_0
    {
        public static readonly System.Version version = new System.Version(1, 45, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSystemHmd3DofModeEnabled(ref Bool enabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetAvailableQueueIndexVulkan(uint queueIndexVk);
    }

    public static class OVRP_1_46_0
    {
        public static readonly System.Version version = new System.Version(1, 46, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetTiledMultiResDynamic(out Bool isDynamic);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetTiledMultiResDynamic(Bool isDynamic);
    }

    public static class OVRP_1_47_0
    {
        public static readonly System.Version version = new System.Version(1, 47, 0);
    }

    public static class OVRP_1_48_0
    {
        public static readonly System.Version version = new System.Version(1, 48, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetExternalCameraProperties(string cameraName,
            ref CameraIntrinsics cameraIntrinsics, ref CameraExtrinsics cameraExtrinsics);

    }

    public static class OVRP_1_49_0
    {
        public static readonly System.Version version = new System.Version(1, 49, 0);

        public const int OVRP_ANCHOR_NAME_SIZE = 32;

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetClientColorDesc(ColorSpace colorSpace);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetHmdColorDesc(ref ColorSpace colorSpace);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_EncodeMrcFrameWithPoseTime(IntPtr rawBuffer, IntPtr audioDataPtr,
            int audioDataLen, int audioChannels, double timestamp, double poseTime, ref int outSyncId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_EncodeMrcFrameDualTexturesWithPoseTime(IntPtr backgroundTextureHandle,
            IntPtr foregroundTextureHandle, IntPtr audioData, int audioDataLen, int audioChannels, double timestamp,
            double poseTime, ref int outSyncId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetHeadsetControllerPose(Posef headsetPose, Posef leftControllerPose,
            Posef rightControllerPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_EnumerateCameraAnchorHandles(ref int anchorCount,
            ref IntPtr CameraAnchorHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetCurrentCameraAnchorHandle(ref IntPtr anchorHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetCameraAnchorName(IntPtr anchorHandle,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = OVRP_ANCHOR_NAME_SIZE)]
            char[] cameraName);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetCameraAnchorHandle(IntPtr anchorName, ref IntPtr anchorHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result
            ovrp_Media_GetCameraAnchorType(IntPtr anchorHandle, ref CameraAnchorType anchorType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_CreateCustomCameraAnchor(IntPtr anchorName, ref IntPtr anchorHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_DestroyCustomCameraAnchor(IntPtr anchorHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetCustomCameraAnchorPose(IntPtr anchorHandle, ref Posef pose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetCustomCameraAnchorPose(IntPtr anchorHandle, Posef pose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetCameraMinMaxDistance(IntPtr anchorHandle, ref double minDistance,
            ref double maxDistance);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetCameraMinMaxDistance(IntPtr anchorHandle, double minDistance,
            double maxDistance);
    }

    public static class OVRP_1_50_0
    {
        public static readonly System.Version version = new System.Version(1, 50, 0);
    }

    public static class OVRP_1_51_0
    {
        public static readonly System.Version version = new System.Version(1, 51, 0);
    }

    public static class OVRP_1_52_0
    {
        public static readonly System.Version version = new System.Version(1, 52, 0);
    }

    public static class OVRP_1_53_0
    {
        public static readonly System.Version version = new System.Version(1, 53, 0);
    }

    public static class OVRP_1_54_0
    {
        public static readonly System.Version version = new System.Version(1, 54, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetPlatformInitialized();
    }

    public static class OVRP_1_55_0
    {
        public static readonly System.Version version = new System.Version(1, 55, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSkeleton2(SkeletonType skeletonType, out Skeleton2Internal skeleton);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_PollEvent(ref EventDataBuffer eventDataBuffer);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNativeXrApiType(out XrApi xrApi);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNativeOpenXRHandles(out UInt64 xrInstance, out UInt64 xrSession);
    }

    public static class OVRP_1_55_1
    {
        public static readonly System.Version version = new System.Version(1, 55, 1);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_PollEvent2(ref EventType eventType, ref IntPtr eventData);
    }

    public static class OVRP_1_56_0
    {
        public static readonly System.Version version = new System.Version(1, 56, 0);
    }

    public static class OVRP_1_57_0
    {
        public static readonly System.Version version = new System.Version(1, 57, 0);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_GetPlatformCameraMode(out Media.PlatformCameraMode platformCameraMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_SetPlatformCameraMode(Media.PlatformCameraMode platformCameraMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetEyeFovPremultipliedAlphaMode(Bool enabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetEyeFovPremultipliedAlphaMode(ref Bool enabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetKeyboardOverlayUV(Vector2f uv);
    }

    public static class OVRP_1_58_0
    {
        public static readonly System.Version version = new System.Version(1, 58, 0);
    }

    public static class OVRP_1_59_0
    {
        public static readonly System.Version version = new System.Version(1, 59, 0);
    }

    public static class OVRP_1_60_0
    {
        public static readonly System.Version version = new System.Version(1, 60, 0);

    }

    public static class OVRP_1_61_0
    {
        public static readonly System.Version version = new System.Version(1, 61, 0);
    }

    public static class OVRP_1_62_0
    {
        public static readonly System.Version version = new System.Version(1, 62, 0);
    }

    public static class OVRP_1_63_0
    {
        public static readonly System.Version version = new System.Version(1, 63, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_InitializeInsightPassthrough();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ShutdownInsightPassthrough();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_GetInsightPassthroughInitialized();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetInsightPassthroughStyle(int layerId, InsightPassthroughStyle style);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreateInsightTriangleMesh(
            int layerId, IntPtr vertices, int vertexCount, IntPtr triangles, int triangleCount, out ulong meshHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroyInsightTriangleMesh(ulong meshHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_AddInsightPassthroughSurfaceGeometry(int layerId, ulong meshHandle,
            Matrix4x4 T_world_model, out ulong geometryInstanceHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroyInsightPassthroughGeometryInstance(ulong geometryInstanceHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_UpdateInsightPassthroughGeometryTransform(ulong geometryInstanceHandle,
            Matrix4x4 T_world_model);
    }
#endif // !OVRPLUGIN_UNSUPPORTED_PLATFORM

    public static class OVRP_1_64_0
    {
        public static readonly System.Version version = new System.Version(1, 64, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_LocateSpace(ref Posef location, ref UInt64 space,
            TrackingOrigin trackingOrigin);
    }

    public static class OVRP_1_65_0
    {
        public static readonly System.Version version = new System.Version(1, 65, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxLoadFromMemory(ref IntPtr data, uint length, ref System.IntPtr texture);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxTextureWidth(IntPtr texture, ref uint width);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxTextureHeight(IntPtr texture, ref uint height);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxTranscode(IntPtr texture, uint format);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxGetTextureData(IntPtr texture, IntPtr data, uint bufferSize);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxTextureSize(IntPtr texture, ref uint size);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_KtxDestroy(IntPtr texture);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroySpace(ref UInt64 space);
    }

    public static class OVRP_1_66_0
    {
        public static readonly System.Version version = new System.Version(1, 66, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetInsightPassthroughInitializationState();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_Media_IsCastingToRemoteClient(out Bool isCasting);
    }

    public static class OVRP_1_67_0
    {
        public static readonly System.Version version = new System.Version(1, 67, 0);
    }

    public static class OVRP_1_68_0
    {
        public static readonly System.Version version = new System.Version(1, 68, 0);

        public const int OVRP_RENDER_MODEL_MAX_PATH_LENGTH = 256;
        public const int OVRP_RENDER_MODEL_MAX_NAME_LENGTH = 64;

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_LoadRenderModel(UInt64 modelKey, uint bufferInputCapacity,
            ref uint bufferCountOutput, IntPtr buffer);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetRenderModelPaths(uint index, IntPtr path);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetRenderModelProperties(string path,
            out RenderModelPropertiesInternal properties);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetInsightPassthroughKeyboardHandsIntensity(int layerId,
            InsightPassthroughKeyboardHandsIntensity intensity);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartKeyboardTracking(UInt64 trackedKeyboardId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StopKeyboardTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSystemKeyboardDescription(TrackedKeyboardQueryFlags keyboardQueryFlags,
            out KeyboardDescription keyboardDescription);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetKeyboardState(Step stepId, int frameIndex, out KeyboardState keyboardState);
    }

    public static class OVRP_1_69_0
    {
        public static readonly System.Version version = new System.Version(1, 69, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodePoseStateImmediate(Node nodeId, out PoseStatef nodePoseState);

    }

    public static class OVRP_1_70_0
    {
        public static readonly System.Version version = new System.Version(1, 70, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetLogCallback2(LogCallback2DelegateType logCallback);
    }

    public static class OVRP_1_71_0
    {
        public static readonly System.Version version = new System.Version(1, 71, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_IsInsightPassthroughSupported(ref Bool supported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_SetClientVersion(int majorVersion, int minorVersion,
            int patchVersion);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ovrp_UnityOpenXR_HookGetInstanceProcAddr(IntPtr func);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_UnityOpenXR_OnInstanceCreate(UInt64 xrInstance);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnInstanceDestroy(UInt64 xrInstance);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionCreate(UInt64 xrSession);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnAppSpaceChange(UInt64 xrSpace);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionStateChange(int oldState, int newState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionBegin(UInt64 xrSession);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionEnd(UInt64 xrSession);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionExiting(UInt64 xrSession);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ovrp_UnityOpenXR_OnSessionDestroy(UInt64 xrSession);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetSuggestedCpuPerformanceLevel(ProcessorPerformanceLevel perfLevel);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSuggestedCpuPerformanceLevel(out ProcessorPerformanceLevel perfLevel);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetSuggestedGpuPerformanceLevel(ProcessorPerformanceLevel perfLevel);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSuggestedGpuPerformanceLevel(out ProcessorPerformanceLevel perfLevel);


    }

    public static class OVRP_1_72_0
    {
        public static readonly System.Version version = new System.Version(1, 72, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreateSpatialAnchor(ref SpatialAnchorCreateInfo createInfo,
            out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetSpaceComponentStatus(ref UInt64 space, SpaceComponentType componentType,
            Bool enable, double timeout, out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceComponentStatus(ref UInt64 space, SpaceComponentType componentType,
            out Bool enabled, out Bool changePending);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EnumerateSpaceSupportedComponents(ref UInt64 space,
            uint componentTypesCapacityInput, out uint componentTypesCountOutput,
            [MarshalAs(UnmanagedType.LPArray), In, Out] SpaceComponentType[] componentTypes);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SaveSpace(ref UInt64 space, SpaceStorageLocation location,
            SpaceStoragePersistenceMode mode, out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QuerySpaces(ref SpaceQueryInfo queryInfo, out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_RetrieveSpaceQueryResults(ref UInt64 requestId, UInt32 resultCapacityInput,
            ref UInt32 resultCountOutput, System.IntPtr results);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_EraseSpace(ref UInt64 space, SpaceStorageLocation location,
            out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceContainer(ref UInt64 space,
            ref SpaceContainerInternal containerInternal);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceBoundingBox2D(ref UInt64 space, out Rectf rect);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceBoundingBox3D(ref UInt64 space, out Boundsf bounds);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceSemanticLabels(ref UInt64 space,
            ref SpaceSemanticLabelInternal labelsInternal);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceRoomLayout(ref UInt64 space,
            ref RoomLayoutInternal roomLayoutInternal);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceBoundary2D(ref UInt64 space,
            ref PolygonalBoundary2DInternal boundaryInternal);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_RequestSceneCapture(ref SceneCaptureRequestInternal request,
            out UInt64 requestId);
    }

    public static class OVRP_1_73_0
    {
        public static readonly System.Version version = new System.Version(1, 73, 0);

    }

    public static class OVRP_1_74_0
    {
        public static readonly System.Version version = new System.Version(1, 74, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceUuid(in UInt64 space, out Guid uuid);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreateVirtualKeyboard(VirtualKeyboardCreateInfo createInfo);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroyVirtualKeyboard();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SendVirtualKeyboardInput(VirtualKeyboardInputInfo inputInfo,
            ref Posef interactorRootPose);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ChangeVirtualKeyboardTextContext(string textContext);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreateVirtualKeyboardSpace(VirtualKeyboardSpaceCreateInfo createInfo,
            out UInt64 keyboardSpace);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SuggestVirtualKeyboardLocation(VirtualKeyboardLocationInfo locationInfo);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetVirtualKeyboardScale(out float location);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetRenderModelProperties2(string path, RenderModelFlags flags,
            out RenderModelPropertiesInternal properties);
    }

    public static class OVRP_1_75_0
    {
        public static readonly System.Version version = new System.Version(1, 75, 0);
    }

    public static class OVRP_1_76_0
    {
        public static readonly System.Version version = new System.Version(1, 76, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetNodePoseStateAtTime(double time, Node nodeId, out PoseStatef nodePoseState);

    }

    public static class OVRP_1_78_0
    {
        public static readonly System.Version version = new System.Version(1, 78, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetPassthroughCapabilityFlags(ref PassthroughCapabilityFlags capabilityFlags);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFoveationEyeTrackedSupported(out Bool foveationSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFoveationEyeTracked(out Bool isEyeTrackedFoveation);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetFoveationEyeTracked(Bool isEyeTrackedFoveation);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartFaceTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StopFaceTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartBodyTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StopBodyTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartEyeTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StopEyeTracking();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetEyeTrackingSupported(out Bool eyeTrackingSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceTrackingSupported(out Bool faceTrackingSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetBodyTrackingEnabled(out Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetBodyTrackingSupported(out Bool value);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetBodyState(Step stepId, int frameIndex, out BodyStateInternal bodyState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceTrackingEnabled(out Bool faceTrackingEnabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceState(Step stepId, int frameIndex, out FaceStateInternal faceState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetEyeTrackingEnabled(out Bool eyeTrackingEnabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetEyeGazesState(Step stepId, int frameIndex,
            out EyeGazesStateInternal eyeGazesState);


        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerState5(uint controllerMask, ref ControllerState5 controllerState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetControllerLocalizedVibration(Controller controllerMask,
            HapticsLocation hapticsLocationMask, float frequency, float amplitude);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLocalDimmingSupported(out Bool localDimmingSupported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetLocalDimming(Bool localDimmingMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLocalDimming(out Bool localDimmingMode);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCurrentInteractionProfile(Hand hand,
            out InteractionProfile interactionProfile);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetControllerHapticsAmplitudeEnvelope(
            Controller controllerMask,
            HapticsAmplitudeEnvelopeVibration hapticsVibration);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetControllerHapticsPcm(
            Controller controllerMask,
            HapticsPcmVibration hapticsVibration);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerSampleRateHz(Controller controller, out float sampleRateHz);
    }

    public static class OVRP_1_79_0
    {
        public static readonly System.Version version = new System.Version(1, 79, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe Result ovrp_ShareSpaces(UInt64* spaces, UInt32 numSpaces, ulong* userHandles,
            UInt32 numUsers, out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe Result ovrp_SaveSpaceList(UInt64* spaces, UInt32 numSpaces,
            SpaceStorageLocation location, out UInt64 requestId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceUserId(in UInt64 spaceUserHandle, out UInt64 spaceUserId);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreateSpaceUser(in UInt64 spaceUserId, out UInt64 spaceUserHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroySpaceUser(in UInt64 userHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_LocateSpace2(out SpaceLocationf location, in UInt64 space,
            TrackingOrigin trackingOrigin);



        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DeclareUser(in UInt64 userId, out UInt64 userHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplMarkerStart(int markerId, int instanceKey, long timestampMs);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplMarkerEnd(int markerId, Qpl.ResultType resultTypeId,
            int instanceKey, long timestampMs);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplMarkerPointCached(int markerId, int nameHandle, int instanceKey,
            long timestampMs);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplMarkerAnnotation(int markerId,
            [MarshalAs(UnmanagedType.LPStr)] string annotationKey,
            [MarshalAs(UnmanagedType.LPStr)] string annotationValue, int instanceKey);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplCreateMarkerHandle([MarshalAs(UnmanagedType.LPStr)] string name,
            out int nameHandle);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplDestroyMarkerHandle(int nameHandle);

    }

    public static class OVRP_1_81_0
    {
        public static readonly System.Version version = new System.Version(1, 81, 0);

    }

    public static class OVRP_1_82_0
    {
        public static readonly System.Version version = new System.Version(1, 82, 0);



        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSpaceTriangleMesh(ref UInt64 space,
            ref TriangleMeshInternal triangleMeshInternal);
    }

    public static class OVRP_1_83_0
    {
        public static readonly System.Version version = new System.Version(1, 83, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerState6(uint controllerMask, ref ControllerState6 controllerState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetVirtualKeyboardModelAnimationStates(
            ref VirtualKeyboardModelAnimationStatesInternal animationStates);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetVirtualKeyboardDirtyTextures(
            ref VirtualKeyboardTextureIdsInternal textureIds);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetVirtualKeyboardTextureData(UInt64 textureId,
            ref VirtualKeyboardTextureData textureData);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetVirtualKeyboardModelVisibility(
            ref VirtualKeyboardModelVisibility visibility);
    }

    public static class OVRP_1_84_0
    {
        public static readonly System.Version version = new System.Version(1, 84, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_CreatePassthroughColorLut(PassthroughColorLutChannels channels,
            UInt32 resolution, PassthroughColorLutData data, out ulong colorLut);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_DestroyPassthroughColorLut(ulong colorLut);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_UpdatePassthroughColorLut(ulong colorLut, PassthroughColorLutData data);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetInsightPassthroughStyle2(int layerId, in InsightPassthroughStyle2 style);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetLayerRecommendedResolution(int layerId, out Sizei recommendedDimensions);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetEyeLayerRecommendedResolution(out Sizei recommendedDimensions);

    }

    public static class OVRP_1_85_0
    {
        public static readonly System.Version version = new System.Version(1, 85, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_OnEditorShutdown();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetPassthroughCapabilities(ref PassthroughCapabilities capabilityFlags);
    }

    public static class OVRP_1_86_0
    {
        public static readonly System.Version version = new System.Version(1, 86, 0);



        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetControllerDrivenHandPoses(Bool controllerDrivenHandPoses);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_IsControllerDrivenHandPosesEnabled(ref Bool enabled);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_AreHandPosesGeneratedByControllerData(Step stepId, Node nodeId, ref Bool isGeneratedByControllerData);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetMultimodalHandsControllersSupported(Bool supported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_IsMultimodalHandsControllersSupported(ref Bool supported);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetCurrentDetachedInteractionProfile(Hand hand,
            out InteractionProfile interactionProfile);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetControllerIsInHand(Step stepId, Node nodeId, ref Bool isInHand);

    }

    public static class OVRP_1_87_0
    {
        public static readonly System.Version version = new System.Version(1, 87, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetPassthroughPreferences(out PassthroughPreferences preferences);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetEyeBufferSharpenType(LayerSharpenType sharpenType);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetControllerDrivenHandPosesAreNatural(Bool controllerDrivenHandPosesAreNatural);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_AreControllerDrivenHandPosesNatural(ref Bool natural);
    }

    public static class OVRP_1_88_0
    {
        public static readonly System.Version version = new System.Version(1, 88, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SetSimultaneousHandsAndControllersEnabled(Bool enabled);
    }

    public static class OVRP_1_89_0
    {
        public static readonly System.Version version = new System.Version(1, 89, 0);

    }

    public static class OVRP_1_90_0
    {
        public static readonly System.Version version = new System.Version(1, 90, 0);

    }

    public static class OVRP_1_91_0
    {
        public static readonly System.Version version = new System.Version(1, 91, 0);


    }


    public static class OVRP_OBSOLETE
    {
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_PreInitialize();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_Initialize(int apiType, IntPtr platformArgs);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Bool ovrp_Shutdown();

    }

    public static class OVRP_1_92_0
    {
        public static readonly System.Version version = new System.Version(1, 92, 0);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceState2(Step stepId, int frameIndex, out FaceState2Internal faceState);
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartFaceTracking2(FaceTrackingDataSource[] requestedDataSources, uint requestedDataSourcesCount);
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StopFaceTracking2();
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceTracking2Enabled(out Bool faceTracking2Enabled);
        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetFaceTracking2Supported(out Bool faceTracking2Enabled);



        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_RequestBodyTrackingFidelity(BodyTrackingFidelity2 fidelity);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_SuggestBodyTrackingCalibrationOverride(BodyTrackingCalibrationInfo calibrationInfo);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_ResetBodyTrackingCalibration();

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetBodyState4(Step stepId, int frameIndex, out BodyState4Internal bodyState);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_GetSkeleton3(SkeletonType skeletonType, out Skeleton3Internal skeleton);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_StartBodyTracking2(BodyJointSet jointSet);

        [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Result ovrp_QplSetConsent(Bool consent);
    }
    /* INSERT NEW OVRP CLASS ABOVE THIS LINE */
    // After modify this file, run `fbpython arvr/projects/integrations/codegen/generate_mockovrplugin.py` to update OculusInternal/Tests/MockOVRPlugin.cs
}
