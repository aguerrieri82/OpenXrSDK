using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.WebLink.Entities;
using Silk.NET.OpenXR;
using System.Text;

namespace OpenXr.WebLink
{
    public class XrWebLinkHub : Hub<IXrWebLinkClient>
    {
        readonly XrApp _app;
        readonly ILogger<XrWebLinkHub> _logger;
        readonly IXrThread _xrThread;

        public XrWebLinkHub(XrApp app, ILogger<XrWebLinkHub> logger, IXrThread xrThread)
        {
            _app = app;
            _logger = logger;
            _xrThread = xrThread;
        }

        public void StartSession()
        {
            _app.Start();
        }

        public void StopSession()
        {
            _app.Stop();
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("Connected: {id}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Disconnected: {id}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task TrackObject(TrackObjectType type, Guid? anchorId, bool enabled)
        {
            var groupId = "track/" + type.ToString().ToLower();
            if (anchorId != null)
                groupId += "/" + anchorId;


            if (enabled)
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
            else
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);

            _logger.LogInformation("Join {connection} '{anchorId}' {on}", Context.ConnectionId, groupId, enabled ? "on" : "off");
        }

        public async Task<List<XrAnchorDetails>> GetAnchors(XrAnchorFilter filter)
        {
            var xrOculus = _app.Plugin<OculusXrPlugin>();
            var result = new List<XrAnchorDetails>();

            var anchors = await xrOculus.QueryAllAnchorsAsync();

            foreach (var space in anchors)
            {
                var item = new XrAnchorDetails();
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
                        item.Bounds2D = bounds.Convert().To<Entities.Rect2f>();
                    }

                    if ((filter.Components & XrAnchorComponent.Pose) != 0)
                    {
                        try
                        {
                            var local = _app.LocateSpace(_app.Stage, space.Space, 1);
                            item.Pose = local.Pose.Convert().To<Entities.Posef>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "LocateSpace {itemId}", item.Id);
                        }

                    }

                    if ((filter.Components & XrAnchorComponent.Mesh) != 0 &&
                        xrOculus.GetSpaceComponentEnabled(space.Space, OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META))
                    {
                        var mesh = xrOculus.GetSpaceTriangleMesh(space.Space);
                        item.Mesh = new Mesh
                        {
                            Indices = mesh.Indices,
                            Vertices = mesh.Vertices!.Convert().To<Entities.Vector3f>()
                        };

                    }
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "GetAnchors {itemId}", item.Id);
                }

                result.Add(item);
            }

            return result;
        }
    }
}
