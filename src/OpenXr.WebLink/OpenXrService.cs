using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenXr.Framework;
using OpenXr.WebLink.Entities;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
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

        public OpenXrService(XrApp app, IHubContext<OpenXrHub> hub, ILogger<OpenXrService> logger)
        {
            _app = app;
            _hub = hub;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopSource = new CancellationTokenSource(); 

            _eventLoopTask = Task.Run(EventLoop);

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

        public async Task EventLoop()
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
                _app.WaitForSession(SessionState.Ready);

                Entities.Posef lastPose = new Entities.Posef();

                try
                {
                    var location = await Task.Run(() => _app.LocateSpace(_app.Head, _app.Stage, 1))
                                   .WaitAsync(TimeSpan.FromMilliseconds(200));

                    var curPose = location.Pose.Convert().To<Entities.Posef>();
                    if (curPose.Similar(lastPose, 0.0001f))
                        continue;

                    lastPose = curPose;

                    var info = new TrackInfo
                    {
                        ObjectType = TrackObjectType.Head,
                        Pose = curPose
                    };

                    await _hub.Clients.Group("track/head")
                        .SendAsync("ObjectChanged", info);

                    //_logger.LogInformation("Send track/head {pos}", info.Pose.Position);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LocateSpace Head: {ex}", ex);
                }
            }
        }
    }
}
