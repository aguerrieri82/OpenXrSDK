using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using XrMath;

public static class VehicleNative
{
    private const string LibName = "xrengine-physx-native";

    public enum VehicleGearMode
    {
        Automatic = 0,
        Manual = 1
    }

    public enum VehicleDriveType
    {
        Front = 0,
        Rear = 1,
        All = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleWheelDesc
    {
        public Vector3 Position;

        public float Radius;
        public float Width;
        public float Mass;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleAxleSimpleDesc
    {
        public VehicleWheelDesc LeftWheel;
        public VehicleWheelDesc RightWheel;

        public float SuspensionTravel;
        public float SuspensionStiffness;
        public float SuspensionDamping;

        public float TireFriction;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleSimpleDesc
    {
        public VehicleAxleSimpleDesc FrontAxle;
        public VehicleAxleSimpleDesc RearAxle;

        public float MaxSteeringAngle;

        public VehicleDriveType DriveType;

        public float MaxMotorTorque;
        public float MaxBrakeTorque;
        public float MaxHandBrakeTorque;

        public float IdleMotorRpm;
        public float MaxMotorRpm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleInput
    {
        public float Throttle;
        public float Brake;
        public float Steering;
        public float HandBrake;

        public VehicleGearMode GearMode;
        public int Gear;
    }

    [InlineArray(4)]
    public struct Pose3Array4
    {
        private Pose3 _element0;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleState
    {
        public Pose3 BodyPose;
        public Pose3Array4 WheelPoses;

        public float SteeringAngle;

        public float Speed;
        public float MotorRpm;

        public int Gear;
        public VehicleGearMode GearMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VehicleWorldDesc
    {
        public nint Physics;
        public nint Scene;
        public nint DefaultMaterial;
    }

    [DllImport(LibName)]
    public static extern nint VehicleWorldCreate(ref VehicleWorldDesc desc);

    [DllImport(LibName)]
    public static extern void VehicleWorldDestroy(nint world);

    [DllImport(LibName)]
    public static extern nint VehicleCreateSimple(nint world, nint actor, ref VehicleSimpleDesc desc);

    [DllImport(LibName)]
    public static extern void VehicleDestroy(nint vehicle);

    [DllImport(LibName)]
    public static extern int VehicleUpdate(nint vehicle, float deltaTime, ref VehicleInput input, ref VehicleState state);

    [DllImport(LibName)]
    public static extern void VehicleSetPose(nint vehicle, ref Pose3 pose);

    [DllImport(LibName)]
    public static extern void VehicleReset(nint vehicle);
}
