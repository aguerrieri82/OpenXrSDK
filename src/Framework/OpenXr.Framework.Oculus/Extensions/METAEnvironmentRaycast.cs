using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public enum EnvironmentRaycastHitStatusMETA
    {
        HitMeta = 1,
        NoHitMeta = 2,
        HitPointOccludedMeta = 3,
        HitPointOutsideOfFovMeta = 4,
        RayOccludedMeta = 5,
        HitInvalidOrientationMeta = 6
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EnvironmentRaycasterMETA
    {
        public ulong Handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SystemEnvironmentRaycastPropertiesMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public uint SupportsEnvironmentRaycast;

        public SystemEnvironmentRaycastPropertiesMETA()
        {
            Type = METAEnvironmentRaycast.TypeSystemEnvironmentRaycastPropertiesMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycasterCreateInfoMETA
    {
        public readonly StructureType Type;
        public void* Next;

        public EnvironmentRaycasterCreateInfoMETA()
        {
            Type = METAEnvironmentRaycast.TypeEnvironmentRaycasterCreateInfoMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycasterCreateCompletionMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public Result FutureResult;
        public EnvironmentRaycasterMETA EnvironmentRaycaster;

        public EnvironmentRaycasterCreateCompletionMETA()
        {
            Type = METAEnvironmentRaycast.TypeEnvironmentRaycasterCreateCompletionMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycastHitGetInfoMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public Space BaseSpace;
        public long Time;
        public Vector3f Origin;
        public Vector3f Direction;
        public uint FilterCount;
        public EnvironmentRaycastFilterBaseHeaderMETA** Filters;

        public EnvironmentRaycastHitGetInfoMETA()
        {
            Type = METAEnvironmentRaycast.TypeEnvironmentRaycastHitGetInfoMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycastHitMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public EnvironmentRaycastHitStatusMETA Status;
        public Posef Pose;

        public EnvironmentRaycastHitMETA()
        {
            Type = METAEnvironmentRaycast.TypeEnvironmentRaycastHitMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycastFilterDistanceMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public float MaxDistance;

        public EnvironmentRaycastFilterDistanceMETA()
        {
            Type = METAEnvironmentRaycast.TypeEnvironmentRaycastFilterDistanceMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EnvironmentRaycastFilterBaseHeaderMETA
    {
        public readonly StructureType Type;
        public void* Next;

        public EnvironmentRaycastFilterBaseHeaderMETA(StructureType type)
        {
            Type = type;
            Next = null;
        }
    }

    public class METAEnvironmentRaycast : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_environment_raycast";

        public const StructureType TypeSystemEnvironmentRaycastPropertiesMeta = (StructureType)1000592000;
        public const StructureType TypeEnvironmentRaycasterCreateInfoMeta = (StructureType)1000592001;
        public const StructureType TypeEnvironmentRaycasterCreateCompletionMeta = (StructureType)1000592002;
        public const StructureType TypeEnvironmentRaycastHitGetInfoMeta = (StructureType)1000592003;
        public const StructureType TypeEnvironmentRaycastHitMeta = (StructureType)1000592004;
        public const StructureType TypeEnvironmentRaycastFilterDistanceMeta = (StructureType)1000592005;

        public METAEnvironmentRaycast(XR xr, Instance instance)
            : base(xr, instance)
        {
        }

        [AllowNull]
        public CreateEnvironmentRaycasterAsyncMETADelegate CreateEnvironmentRaycasterAsyncMETA;

        [AllowNull]
        public CreateEnvironmentRaycasterCompleteMETADelegate CreateEnvironmentRaycasterCompleteMETA;

        [AllowNull]
        public DestroyEnvironmentRaycasterMETADelegate DestroyEnvironmentRaycasterMETA;

        [AllowNull]
        public PerformEnvironmentRaycastMETADelegate PerformEnvironmentRaycastMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateEnvironmentRaycasterAsyncMETADelegate(
            Session session,
            ref EnvironmentRaycasterCreateInfoMETA info,
            ref FutureEXT future);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateEnvironmentRaycasterCompleteMETADelegate(
            Session session,
            FutureEXT future,
            ref EnvironmentRaycasterCreateCompletionMETA completion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyEnvironmentRaycasterMETADelegate(
            EnvironmentRaycasterMETA environmentRaycaster);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result PerformEnvironmentRaycastMETADelegate(
            EnvironmentRaycasterMETA environmentRaycaster,
            ref EnvironmentRaycastHitGetInfoMETA info,
            ref EnvironmentRaycastHitMETA hitPoint);
    }
}