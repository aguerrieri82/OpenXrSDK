using System.Numerics;
using System.Runtime.InteropServices;

namespace Oculus.XrPlugin
{
    public class OculusXrPlugin
    {
        public enum SystemHeadset
        {
            None = 0,

            // Standalone headsets
            Oculus_Quest = 8,
            Oculus_Quest_2 = 9,
            Meta_Quest_Pro = 10,
            Placeholder_10 = 10,
            Meta_Quest_3 = 11,
            Placeholder_11 = 11,
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
            PC_Placeholder_4103 = Meta_Link_Quest_Pro,
            Meta_Link_Quest_3,
            PC_Placeholder_4104 = Meta_Link_Quest_3,
            PC_Placeholder_4105,
            PC_Placeholder_4106,
            PC_Placeholder_4107
        }

        public enum BoundaryType
        {
            /// <summary>
            ///  Outer Boundary -  axis-aligned rectangular bounding box enclosing the outer boundary.
            /// </summary>
            OuterBoundary = 0,
            /// <summary>
            ///  Play area - axis-aligned smaller rectangle area inside outer boundary where gameplay happens.
            /// </summary>
            PlayArea = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UserDefinedSettings
        {
            public ushort sharedDepthBuffer;
            public ushort dashSupport;
            public ushort stereoRenderingMode;
            public ushort colorSpace;
            public ushort lowOverheadMode;
            public ushort optimizeBufferDiscards;
            public ushort phaseSync;
            public ushort symmetricProjection;
            public ushort subsampledLayout;
            public ushort lateLatching;
            public ushort lateLatchingDebug;
            public ushort enableTrackingOriginStageMode;
            public ushort spaceWarp;
            public ushort depthSubmission;
            public ushort foveatedRenderingMethod;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EnvironmentDepthFrameDescpublic
        {
            public bool isValid;
            public double createTime;
            public double predictedDisplayTime;
            public int swapchainIndex;
            public Vector3 createPoseLocation;
            public Vector4 createPoseRotation;
            public float fovLeftAngle;
            public float fovRightAngle;
            public float fovTopAngle;
            public float fovDownAngle;
            public float nearZ;
            public float farZ;
            public float minDepth;
            public float maxDepth;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EnvironmentDepthCreateParamspublic
        {
            public bool removeHands;
        }


        [DllImport("OculusXRPlugin")]
        public static extern void SetColorScale(float x, float y, float z, float w);

        [DllImport("OculusXRPlugin")]
        public static extern void SetColorOffset(float x, float y, float z, float w);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetIsSupportedDevice();

        [DllImport("OculusXRPlugin", CharSet = CharSet.Unicode)]
        public static extern bool LoadOVRPlugin(string? ovrpPath);

        [DllImport("OculusXRPlugin")]
        public static extern void UnloadOVRPlugin();

        [DllImport("OculusXRPlugin")]
        public static extern void SetUserDefinedSettings(UserDefinedSettings settings);

        [DllImport("OculusXRPlugin")]
        public static extern int SetCPULevel(int cpuLevel);

        [DllImport("OculusXRPlugin")]
        public static extern int SetGPULevel(int gpuLevel);

        [DllImport("OculusXRPlugin", CharSet = CharSet.Auto)]
        public static extern void GetOVRPVersion(byte[] version);

        [DllImport("OculusXRPlugin")]
        public static extern void EnablePerfMetrics(bool enable);

        [DllImport("OculusXRPlugin")]
        public static extern void EnableAppMetrics(bool enable);

        [DllImport("OculusXRPlugin")]
        public static extern bool SetDeveloperModeStrict(bool active);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetAppHasInputFocus();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetBoundaryConfigured();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetBoundaryDimensions(BoundaryType boundaryType, out Vector3 dimensions);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetBoundaryVisible();

        [DllImport("OculusXRPlugin")]
        public static extern void SetBoundaryVisible(bool boundaryVisible);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetAppShouldQuit();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetDisplayAvailableFrequencies(IntPtr ptr, ref int numFrequencies);

        [DllImport("OculusXRPlugin")]
        public static extern bool SetDisplayFrequency(float refreshRate);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetDisplayFrequency(out float refreshRate);

        [DllImport("OculusXRPlugin")]
        public static extern SystemHeadset GetSystemHeadsetType();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetTiledMultiResSupported();

        [DllImport("OculusXRPlugin")]
        public static extern void SetTiledMultiResLevel(int level);

        [DllImport("OculusXRPlugin")]
        public static extern int GetTiledMultiResLevel();

        [DllImport("OculusXRPlugin")]
        public static extern void SetTiledMultiResDynamic(bool isDynamic);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEyeTrackedFoveatedRenderingSupported();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEyeTrackedFoveatedRenderingEnabled();

        [DllImport("OculusXRPlugin")]
        public static extern void SetEyeTrackedFoveatedRenderingEnabled(bool isEnabled);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetShouldRestartSession();

        [DllImport("OculusXRPlugin")]
        public static extern bool SetupEnvironmentDepth(ref EnvironmentDepthCreateParamspublic createParams);

        [DllImport("OculusXRPlugin")]
        public static extern bool SetEnvironmentDepthRendering(bool isEnabled);

        [DllImport("OculusXRPlugin")]
        public static extern bool ShutdownEnvironmentDepth();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEnvironmentDepthTextureId(ref uint id);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEnvironmentDepthFrameDesc(ref EnvironmentDepthFrameDescpublic frameDesc, int eye);

        [DllImport("OculusXRPlugin")]
        public static extern bool SetEnvironmentDepthHandRemoval(bool isEnabled);

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEnvironmentDepthSupported();

        [DllImport("OculusXRPlugin")]
        public static extern bool GetEnvironmentDepthHandRemovalSupported();
    }
}
