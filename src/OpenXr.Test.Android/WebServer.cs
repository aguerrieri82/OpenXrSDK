using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Test.Android
{
    [Service(Enabled = true, 
        IsolatedProcess = false,
        ForegroundServiceType = ForegroundService.TypeRemoteMessaging)]
    public class WebServer : Service
    {
        private WebApplication? _webApp;
        private Task _appTask;

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override void OnCreate()
        {

            base.OnCreate();

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.ConfigureKestrel(op => op.ListenAnyIP(8080));

            builder.Services.AddOpenXrWebLink(GlobalServices.App!);

            _webApp = builder.Build();

            _webApp.UseOpenXrWebLink();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            _appTask = _webApp!.RunAsync();

            return base.OnStartCommand(intent, flags, startId);
        }

        public override async void OnDestroy()
        {
            await _webApp!.StopAsync();
            base.OnDestroy();
        }
    }
}
