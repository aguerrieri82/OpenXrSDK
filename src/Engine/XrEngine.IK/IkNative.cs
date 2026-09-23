using System.Numerics;
using System.Runtime.InteropServices;

namespace XrEngine.IK;

internal static unsafe class IkNative
{
    public const int AbiVersion = 2;
    const string LibName = "ik-native";

    [StructLayout(LayoutKind.Sequential)]
    public struct Pose
    {
        public Vector3 Position;
        public Quaternion Orientation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bone
    {
        public int Parent;
        public IkAxis Axes;
        public Vector3 Offset;
        public Quaternion RestOrientation;
        public Quaternion InitialRotation;
        public float Length;
        public IkAxis LimitedAxes;
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 Stiffness;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Target
    {
        public int Bone;
        public Pose Pose;
        public float PositionWeight;
        public float OrientationWeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Pole
    {
        public int Bone;
        public Vector3 Position;
        public float Angle;
        public int ComputeAngle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BonePose
    {
        public Quaternion LocalRotation;
        public Vector3 Start;
        public Pose Tip;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Options
    {
        public int MaximumIterations;
        public float PositionTolerance;
        public float OrientationTolerance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Result
    {
        public float PositionError;
        public float OrientationError;
        public float PoleAngle;
        public int TargetsReached;
        public int SolverConverged;
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_abi_version();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint xr_ik_last_error();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_create(Bone* bones, int count, out nint context);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void xr_ik_destroy(nint context);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_get_bone_count(nint context);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_reset(nint context);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_set_rotations(nint context, Quaternion* rotations, int count);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_read_pose(nint context, BonePose* poses, int count);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int xr_ik_solve(
        nint context,
        Target* targets,
        int count,
        Pole* pole,
        Options* options,
        BonePose* poses,
        int poseCount,
        Result* result);

    public static void Check(int result)
    {
        if (result == 0)
            return;

        var ptr = xr_ik_last_error();
        var message = ptr == 0 ? null : Marshal.PtrToStringUTF8(ptr);

        throw new InvalidOperationException(message ?? "Native IK error.");
    }
}