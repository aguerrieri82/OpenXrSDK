using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.WebLink.Entities;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink
{
    public class OpenXrService : IHostedService
    {
        private readonly XrApp _app;
        private readonly IHubContext<OpenXrHub> _hub;
        private readonly ILogger<OpenXrService> _logger;
        Task? _eventLoopTask;
        Task? _trackLoopTask;
        CancellationTokenSource? _stopSource;
        private Entities.Posef _lastPose;
        readonly IXrThread _xrThread;

        public OpenXrService(XrApp app, IHubContext<OpenXrHub> hub, ILogger<OpenXrService> logger, IXrThread xrThread)
        {
            _app = app;
            _hub = hub;
            _logger = logger;
            _xrThread = xrThread;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopSource = new CancellationTokenSource();

            _eventLoopTask = Task.Run(XrEventLoop);

            _trackLoopTask = Task.Run(TrackLoop);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopSource!.Cancel();

            await Task.WhenAll(_eventLoopTask!, _trackLoopTask!).WaitAsync(cancellationToken);
        }

        public async Task XrEventLoop()
        {
            while (!_stopSource!.IsCancellationRequested)
            {
                _app.HandleEvents(_stopSource!.Token);

                await Task.Delay(50, _stopSource.Token);
            }
        }

        public async Task TrackLoop()
        {
            while (!_stopSource!.IsCancellationRequested)
            {
                _app.WaitForSession(SessionState.Ready, SessionState.Focused);

                await TrackAsync();

                await Task.Delay(1000/70, _stopSource.Token);
            }
          
        }

        public async Task TrackAsync()
        {
            if (!(_app.SessionState == SessionState.Ready || _app.SessionState == SessionState.Focused))
                return;

            try
            {
                var location = _app.LocateSpace(_app.Head, _app.Floor, 1);

                var curPose = location.Pose.Convert().To<Entities.Posef>();
                if (curPose.Similar(_lastPose, 0.0001f))
                    return;

                _lastPose = curPose;

                var info = new TrackInfo
                {
                    ObjectType = TrackObjectType.Head,
                    Pose = curPose
                };

                await _hub.Clients.Group("track/head")
                        .SendAsync("ObjectChanged", info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LocateSpace Head: {ex}", ex);
            }
        }
    }
}
