using System.Numerics;
using XrMath;
using static XrEngine.IK.IkNative;

namespace XrEngine.IK;

[Flags]
public enum IkAxis
{
    None = 0,
    X = 1,
    Y = 2,
    Z = 4,
    All = X | Y | Z
}

public struct IkBone
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

    public IkBone(int parent, Vector3 offset, float length, IkAxis axes = IkAxis.None)
    {
        Parent = parent;
        Axes = axes;
        Offset = offset;
        RestOrientation = Quaternion.Identity;
        InitialRotation = Quaternion.Identity;
        Length = length;
        LimitedAxes = IkAxis.None;
        Minimum = Vector3.Zero;
        Maximum = Vector3.Zero;
        Stiffness = Vector3.Zero;
    }
}

public struct IkTarget
{
    public int Bone;
    public Pose3 Pose;
    public float PositionWeight;
    public float OrientationWeight;

    public IkTarget(int bone, Pose3 pose, float positionWeight = 1, float orientationWeight = 0)
    {
        Bone = bone;
        Pose = pose;
        PositionWeight = positionWeight;
        OrientationWeight = orientationWeight;
    }

    public IkTarget(int bone, Vector3 position, float weight = 1)
    {
        Bone = bone;
        Pose = new Pose3
        {
            Position = position,
            Orientation = Quaternion.Identity
        };
        PositionWeight = weight;
        OrientationWeight = 0;
    }
}

public struct IkPole
{
    public int Bone;
    public Vector3 Position;
    public float Angle;
    public bool ComputeAngle;
}

public struct IkBonePose
{
    public Quaternion LocalRotation;
    public Vector3 Start;
    public Pose3 Tip;
}

public struct IkSolveOptions
{
    public int MaximumIterations;
    public float PositionTolerance;
    public float OrientationTolerance;

    public static IkSolveOptions Default => new()
    {
        MaximumIterations = 64,
        PositionTolerance = 0.001f,
        OrientationTolerance = 0.01f
    };
}

public struct IkSolveResult
{
    public float PositionError;
    public float OrientationError;
    public float PoleAngle;
    public bool TargetsReached;
    public bool SolverConverged;
}

public sealed unsafe class IkSolver : IDisposable
{
    nint _context;

    IkNative.Target[] _nativeTargets = [];
    readonly IkNative.BonePose[] _nativePoses;
    readonly IkBonePose[] _poses;

    public IkSolver(ReadOnlySpan<IkBone> bones)
    {
        if (IkNative.xr_ik_abi_version() != IkNative.AbiVersion)
            throw new InvalidOperationException($"Unsupported native IK ABI version.");

        if (bones.Length == 0)
            throw new ArgumentException("Skeleton cannot be empty.", nameof(bones));

        var nativeBones = new IkNative.Bone[bones.Length];

        for (var i = 0; i < bones.Length; i++)
        {
            ref readonly var bone = ref bones[i];

            nativeBones[i] = new IkNative.Bone
            {
                Parent = bone.Parent,
                Axes = bone.Axes,
                Offset = bone.Offset,
                RestOrientation = bone.RestOrientation,
                InitialRotation = bone.InitialRotation,
                Length = bone.Length,
                LimitedAxes = bone.LimitedAxes,
                Minimum = bone.Minimum,
                Maximum = bone.Maximum,
                Stiffness = bone.Stiffness
            };
        }

        fixed (IkNative.Bone* ptr = nativeBones)
            IkNative.Check(IkNative.xr_ik_create(ptr, nativeBones.Length, out _context));

        _nativePoses = new IkNative.BonePose[bones.Length];
        _poses = new IkBonePose[bones.Length];

        ReadPose();
    }

    public void Reset()
    {
        EnsureValid();
        IkNative.Check(IkNative.xr_ik_reset(_context));
        ReadPose();
    }

    public void SetRotations(ReadOnlySpan<Quaternion> rotations)
    {
        EnsureValid();

        if (rotations.Length != BoneCount)
            throw new ArgumentException("Rotation count must match bone count.", nameof(rotations));

        fixed (Quaternion* ptr = rotations)
            IkNative.Check(IkNative.xr_ik_set_rotations(_context, ptr, rotations.Length));

        ReadPose();
    }

    public IkSolveResult Solve(
        ReadOnlySpan<IkTarget> targets,
        IkPole? pole = null,
        IkSolveOptions? options = null)
    {
        EnsureValid();

        if (_nativeTargets.Length < targets.Length)
            Array.Resize(ref _nativeTargets, targets.Length);

        for (var i = 0; i < targets.Length; i++)
        {
            ref readonly var target = ref targets[i];

            _nativeTargets[i] = new IkNative.Target
            {
                Bone = target.Bone,
                Pose = new IkNative.Pose
                {
                    Position = target.Pose.Position,
                    Orientation = target.Pose.Orientation
                },
                PositionWeight = target.PositionWeight,
                OrientationWeight = target.OrientationWeight
            };
        }

        var solveOptions = options ?? IkSolveOptions.Default;

        var nativeOptions = new IkNative.Options
        {
            MaximumIterations = solveOptions.MaximumIterations,
            PositionTolerance = solveOptions.PositionTolerance,
            OrientationTolerance = solveOptions.OrientationTolerance
        };

        IkNative.Pole nativePole = default;
        IkNative.Pole* polePtr = null;

        if (pole.HasValue)
        {
            var value = pole.Value;

            nativePole = new IkNative.Pole
            {
                Bone = value.Bone,
                Position = value.Position,
                Angle = value.Angle,
                ComputeAngle = value.ComputeAngle ? 1 : 0
            };

            polePtr = &nativePole;
        }

        IkNative.Result nativeResult;

        fixed (IkNative.Target* targetPtr = _nativeTargets)
        fixed (IkNative.BonePose* posePtr = _nativePoses)
        {
            IkNative.Check(IkNative.xr_ik_solve(
                _context,
                targets.Length == 0 ? null : targetPtr,
                targets.Length,
                polePtr,
                &nativeOptions,
                posePtr,
                _nativePoses.Length,
                &nativeResult));
        }

        UpdatePoses();

        return new IkSolveResult
        {
            PositionError = nativeResult.PositionError,
            OrientationError = nativeResult.OrientationError,
            PoleAngle = nativeResult.PoleAngle,
            TargetsReached = nativeResult.TargetsReached != 0,
            SolverConverged = nativeResult.SolverConverged != 0
        };
    }

    public void ReadPose()
    {
        EnsureValid();

        fixed (IkNative.BonePose* ptr = _nativePoses)
            IkNative.Check(IkNative.xr_ik_read_pose(_context, ptr, _nativePoses.Length));

        UpdatePoses();
    }

    void UpdatePoses()
    {
        for (var i = 0; i < _nativePoses.Length; i++)
        {
            ref readonly var source = ref _nativePoses[i];

            _poses[i] = new IkBonePose
            {
                LocalRotation = source.LocalRotation,
                Start = source.Start,
                Tip = new Pose3
                {
                    Position = source.Tip.Position,
                    Orientation = source.Tip.Orientation
                }
            };
        }
    }

    void EnsureValid()
    {
        ObjectDisposedException.ThrowIf(_context == 0, this);
    }

    public void Dispose()
    {
        if (_context == 0)
            return;

        IkNative.xr_ik_destroy(_context);
        _context = 0;

        GC.SuppressFinalize(this);
    }

    ~IkSolver()
    {
        if (_context != 0)
            IkNative.xr_ik_destroy(_context);
    }

    public int BoneCount => _poses.Length;

    public ReadOnlySpan<IkBonePose> Poses => _poses;

    public IkBonePose this[int index] => _poses[index];
}