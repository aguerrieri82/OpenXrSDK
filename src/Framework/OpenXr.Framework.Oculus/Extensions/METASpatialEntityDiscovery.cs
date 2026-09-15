using Silk.NET.OpenXR;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OpenXr.Framework.Oculus
{
    public class METASpatialEntityDiscovery : BaseXrExtension
    {
        public const string ExtensionName = "XR_META_spatial_entity_discovery";

        public const StructureType TypeSystemSpaceDiscoveryPropertiesMeta = (StructureType)1000247000;
        public const StructureType TypeSpaceDiscoveryInfoMeta = (StructureType)1000247001;
        public const StructureType TypeSpaceFilterUuidMeta = (StructureType)1000247003;
        public const StructureType TypeSpaceFilterComponentMeta = (StructureType)1000247004;
        public const StructureType TypeEventDataSpaceDiscoveryResultsAvailableMeta = (StructureType)1000247005;
        public const StructureType TypeSpaceDiscoveryResultsMeta = (StructureType)1000247006;
        public const StructureType TypeEventDataSpaceDiscoveryCompleteMeta = (StructureType)1000247007;

        public METASpatialEntityDiscovery(XR xr, Instance instance) : base(xr, instance)
        {
        }

        [AllowNull]
        public DiscoverSpacesMETADelegate DiscoverSpacesMETA;

        [AllowNull]
        public RetrieveSpaceDiscoveryResultsMETADelegate RetrieveSpaceDiscoveryResultsMETA;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result DiscoverSpacesMETADelegate(Session session, ref SpaceDiscoveryInfoMETA info, ref ulong requestId);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate Result RetrieveSpaceDiscoveryResultsMETADelegate(Session session, ulong requestId, ref SpaceDiscoveryResultsMETA results);
    }
}