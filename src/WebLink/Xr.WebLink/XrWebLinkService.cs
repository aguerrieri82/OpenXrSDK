using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using Xr.Math;
using Xr.WebLink.Entities;

namespace Xr.WebLink
{
    public class XrWebLinkService : IHostedService
    {
        protected readonly XrApp _app;
        protected readonly IHubContext<XrWebLinkHub> _hub;
        protected readonly ILogger<XrWebLinkService> _logger;
        protected CancellationTokenSource? _stopSource;
        protected Pose3? _lastPose;
        protected readonly IXrThread _xrThread;
        protected IList<Task> _serviceLoops;

        public XrWebLinkService(XrApp app, IHubContext<XrWebLinkHub> hub, ILogger<XrWebLinkService> logger, IXrThread xrThread)
        {
            _app = app;
            _hub = hub;
            _logger = logger;
            _xrThread = xrThread;
            _serviceLoops = new List<Task>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopSource = new CancellationTokenSource();

            //StartServiceLoop(HandleXrEventsAsync, 50);

            StartServiceLoop(TrackAsync, 1000 / 72);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopSource!.Cancel();

            await Task.WhenAll(_serviceLoops).WaitAsync(cancellationToken);

            _serviceLoops.Clear();
        }

        void StartServiceLoop(Func<CancellationToken, Task> action, int delayMs)
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    while (!_stopSource!.IsCancellationRequested)
                    {
                        await action(_stopSource.Token);

                        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), _stopSource.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                }
            });

            _serviceLoops.Add(task);
        }

        public async Task TrackAsync(CancellationToken cancellationToken)
        {
            if (!(_app.SessionState == SessionState.Ready || _app.SessionState == SessionState.Focused))
                return;

            try
            {
                var location = _app.LocateSpace(_app.Head, _app.Local, 1);

                var curPose = new Pose3
                {
                    Orientation = location.Pose!.Orientation,
                    Position = location.Pose!.Position,
                };

                if (curPose.Similar(_lastPose!.Value, 0.0001f))
                    return;

                _lastPose = curPose;

                var info = new TrackInfo
                {
                    ObjectType = TrackObjectType.Head,
                    Pose = curPose
                };

                await _hub.Clients.Group("track/head").SendAsync("ObjectChanged", info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LocateSpace Head: {ex}", ex);
            }
        }
    }
}
