using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using OpenXr.WebLink.Entities;

namespace OpenXr.WebLink
{
    public class XrWebLinkHub : Hub
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

        public Task<List<XrAnchor>> GetAnchors(XrAnchorFilter filter)
        {
            return _app.Plugin<OculusXrPlugin>().GetAnchorsAsync(filter);
        }
    }
}
