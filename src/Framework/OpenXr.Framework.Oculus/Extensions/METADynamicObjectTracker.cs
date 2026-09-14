using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public enum DynamicObjectClassMETA
    {
        KeyboardMeta = 1000587000,
        MaxEnumMeta = 0x7FFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DynamicObjectTrackerMETA
    {
        public ulong Handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SystemDynamicObjectTrackerPropertiesMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public uint SupportsDynamicObjectTracker;

        public SystemDynamicObjectTrackerPropertiesMETA()
        {
            Type = METADynamicObjectTracker.TypeSystemDynamicObjectTrackerPropertiesMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicObjectTrackerCreateInfoMETA
    {
        public readonly StructureType Type;
        public void* Next;

        public DynamicObjectTrackerCreateInfoMETA()
        {
            Type = METADynamicObjectTracker.TypeDynamicObjectTrackerCreateInfoMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicObjectTrackedClassesSetInfoMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public uint ClassCount;
        public DynamicObjectClassMETA* Classes;

        public DynamicObjectTrackedClassesSetInfoMETA()
        {
            Type = METADynamicObjectTracker.TypeDynamicObjectTrackedClassesSetInfoMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicObjectDataMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public DynamicObjectClassMETA ClassType;

        public DynamicObjectDataMETA()
        {
            Type = METADynamicObjectTracker.TypeDynamicObjectDataMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventDataDynamicObjectTrackerCreateResultMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public DynamicObjectTrackerMETA Handle;
        public Result Result;

        public EventDataDynamicObjectTrackerCreateResultMETA()
        {
            Type = METADynamicObjectTracker.TypeEventDataDynamicObjectTrackerCreateResultMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventDataDynamicObjectSetTrackedClassesResultMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public DynamicObjectTrackerMETA Handle;
        public Result Result;

        public EventDataDynamicObjectSetTrackedClassesResultMETA()
        {
            Type = METADynamicObjectTracker.TypeEventDataDynamicObjectSetTrackedClassesResultMeta;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct SystemDynamicObjectKeyboardPropertiesMETA
    {
        public readonly StructureType Type;
        public void* Next;
        public uint SupportsDynamicObjectKeyboard;

        public SystemDynamicObjectKeyboardPropertiesMETA()
        {
            Type = METADynamicObjectKeyboard.TypeSystemDynamicObjectKeyboardPropertiesMeta;
        }
    }

    public class METADynamicObjectTracker : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_dynamic_object_tracker";

        public const ObjectType ObjectTypeDynamicObjectTrackerMeta = (ObjectType)1000288000;

        public const StructureType TypeDynamicObjectTrackerCreateInfoMeta = (StructureType)1000288001;
        public const StructureType TypeDynamicObjectTrackedClassesSetInfoMeta = (StructureType)1000288002;
        public const StructureType TypeDynamicObjectDataMeta = (StructureType)1000288003;
        public const StructureType TypeEventDataDynamicObjectTrackerCreateResultMeta = (StructureType)1000288004;
        public const StructureType TypeEventDataDynamicObjectSetTrackedClassesResultMeta = (StructureType)1000288005;
        public const StructureType TypeSystemDynamicObjectTrackerPropertiesMeta = (StructureType)1000288006;

        public const SpaceComponentTypeFB SpaceComponentTypeDynamicObjectDataMeta = (SpaceComponentTypeFB)1000288007;

        public METADynamicObjectTracker(XR xr, Instance instance)
            : base(xr, instance)
        {
        }

        [AllowNull]
        public CreateDynamicObjectTrackerMETADelegate CreateDynamicObjectTrackerMETA;

        [AllowNull]
        public DestroyDynamicObjectTrackerMETADelegate DestroyDynamicObjectTrackerMETA;

        [AllowNull]
        public SetDynamicObjectTrackedClassesMETADelegate SetDynamicObjectTrackedClassesMETA;

        [AllowNull]
        public GetSpaceDynamicObjectDataMETADelegate GetSpaceDynamicObjectDataMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result CreateDynamicObjectTrackerMETADelegate(
            Session session,
            ref DynamicObjectTrackerCreateInfoMETA createInfo,
            ref DynamicObjectTrackerMETA dynamicObjectTracker);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DestroyDynamicObjectTrackerMETADelegate(
            DynamicObjectTrackerMETA dynamicObjectTracker);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result SetDynamicObjectTrackedClassesMETADelegate(
            DynamicObjectTrackerMETA dynamicObjectTracker,
            ref DynamicObjectTrackedClassesSetInfoMETA setInfo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result GetSpaceDynamicObjectDataMETADelegate(
            Space space,
            ref DynamicObjectDataMETA dynamicObjectDataOutput);
    }

    public class METADynamicObjectKeyboard : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_dynamic_object_keyboard";

        public const DynamicObjectClassMETA DynamicObjectClassKeyboardMeta = (DynamicObjectClassMETA)1000587000;
        public const StructureType TypeSystemDynamicObjectKeyboardPropertiesMeta = (StructureType)1000587001;

        public METADynamicObjectKeyboard(XR xr, Instance instance)
            : base(xr, instance)
        {
        }
    }
}