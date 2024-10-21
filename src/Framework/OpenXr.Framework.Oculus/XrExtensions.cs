using Microsoft.Extensions.Logging;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
using System.Reflection.Emit;
using XrMath;

namespace OpenXr.Framework
{
    public static class XrExtensions
    {
        public unsafe static UuidEXT[] GetWalls(this RoomLayoutFB layout)
        {
            var span = new Span<UuidEXT>(layout.WallUuids, (int)layout.WallUuidCountOutput);
            return span.ToArray();
        }

        public unsafe static Guid ToGuid(this UuidEXT uuid)
        {
            return new Guid(new Span<byte>(uuid.Data, 16));
        }

      

        public static async Task<List<XrAnchor>> GetAnchorsAsync(this OculusXrPlugin xrOculus, XrAnchorFilter filter)
        {
            var result = new List<XrAnchor>();

            var anchors = await xrOculus.QueryAllAnchorsAsync(filter.Ids?.ToArray());

            foreach (var space in anchors)
            {
                var hasLabel = filter.Labels != null || (filter.Components & XrAnchorComponent.Label) != 0;

                string[] labels = [];   

                if (hasLabel)
                {
                    if (xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.SemanticLabelsFB))
                        labels = xrOculus.GetSpaceSemanticLabels(space.Space);
                }

                if (filter.Labels != null && !labels.Any(filter.Labels.Contains))
                    continue;

                var supported = xrOculus.EnumerateSpaceSupportedComponentsFB(space.Space);

                var item = new XrAnchor
                {
                    Id = space.Uuid.ToGuid(),
                    Space = space.Space.Handle,
                    Labels = labels,    
                };

                try
                {
                    if ((filter.Components & XrAnchorComponent.Bounds) != 0 &&
                        xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.Bounded2DFB))
                    {
                        var bounds = xrOculus.GetSpaceBoundingBox2D(space.Space);
                        item.Bounds2D = bounds.Convert().To<Rect2>();
                    }

                    if ((filter.Components & XrAnchorComponent.Pose) != 0 &&
                        supported.Contains(SpaceComponentTypeFB.LocatableFB))
                    {
                        try
                        {
                            if (!xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.LocatableFB))
                                await xrOculus.SetSpaceComponentStatusAsync(space.Space, SpaceComponentTypeFB.LocatableFB, true);

                            var local = xrOculus.App.LocateSpace(space.Space, xrOculus.App.Stage, 1);
                            item.Pose = local.Pose;

                        }
                        catch (Exception ex)
                        {
                            xrOculus.App.Logger?.LogError(ex, "LocateSpace {itemId}", item.Id);
                        }
                    }

                    if ((filter.Components & XrAnchorComponent.Mesh) != 0 &&
                        xrOculus.GetSpaceComponentEnabled(space.Space, OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META))
                    {
                        var mesh = xrOculus.GetSpaceTriangleMesh(space.Space);
                        item.Mesh = new Mesh
                        {
                            Indices = mesh.Indices,
                            Vertices = mesh.Vertices!.Convert().To<Vector3>()
                        };
                    }
                }

                catch (Exception ex)
                {
                    //_logger.LogError(ex, "GetAnchors {itemId}", item.Id);
                }

                result.Add(item);
            }

            return result;
        }

    }
}
