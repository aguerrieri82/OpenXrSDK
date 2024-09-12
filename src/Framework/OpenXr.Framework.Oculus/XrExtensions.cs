using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Numerics;
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

            var anchors = await xrOculus.QueryAllAnchorsAsync();

            foreach (var space in anchors)
            {
                var supported = xrOculus.EnumerateSpaceSupportedComponentsFB(space.Space);

                var item = new XrAnchor();
                item.Id = space.Uuid.ToGuid();
                try
                {
                    if ((filter.Components & XrAnchorComponent.Label) != 0 &&
                        xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.SemanticLabelsFB))
                    {
                        item.Labels = xrOculus.GetSpaceSemanticLabels(space.Space);
                    }


                    if ((filter.Components & XrAnchorComponent.Bounds) != 0 &&
                        xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.Bounded2DFB))
                    {
                        var bounds = xrOculus.GetSpaceBoundingBox2D(space.Space);
                        item.Bounds2D = bounds.Convert().To<Rect2>();
                    }

                    if ((filter.Components & XrAnchorComponent.Pose) != 0 &&
                        supported.Contains(SpaceComponentTypeFB.LocatableFB))
                    {
                        if (!xrOculus.GetSpaceComponentEnabled(space.Space, SpaceComponentTypeFB.LocatableFB))
                            await xrOculus.SetSpaceComponentStatusAsync(space.Space, SpaceComponentTypeFB.LocatableFB, true);

                        try
                        {
                            var local = xrOculus.App.LocateSpace(space.Space, xrOculus.App.Stage, 1);
                            item.Pose = local.Pose;
                            item.Space = space.Space.Handle;
                        }
                        catch (Exception ex)
                        {
                            //_logger.LogError(ex, "LocateSpace {itemId}", item.Id);
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
