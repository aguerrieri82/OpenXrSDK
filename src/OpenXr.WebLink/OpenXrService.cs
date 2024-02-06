using Microsoft.Extensions.Hosting;
using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.WebLink
{
    public class OpenXrService : IHostedService
    {
        XrApp _app;
        Task? _eventLoopTask;
        CancellationTokenSource? _stopSource;

        public OpenXrService(XrApp app)
        {
            _app = app; 
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopSource = new CancellationTokenSource(); 

            _eventLoopTask = Task.Run(EventLoop);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _stopSource!.Cancel();

            await _eventLoopTask!.WaitAsync(cancellationToken);
        }

        public async Task EventLoop()
        {
            while (!_stopSource!.IsCancellationRequested)
            {
                _app.HandleEvents(_stopSource!.Token);

                await Task.Delay(50, _stopSource.Token);
            }
        }
    }
}
