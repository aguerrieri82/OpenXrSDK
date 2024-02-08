using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using OpenXr.Framework;



namespace OpenXr.Test.Android
{
    [Service(Enabled = true, 
        IsolatedProcess = false,
        ForegroundServiceType = ForegroundService.TypeRemoteMessaging)]
    public class WebServerService : Service
    {
        private WebApplication? _webApp;
        private bool _isActive;
        private Binder _localBinder;

        public class LocalBinder : Binder
        {
            private WebServerService _service;

            public LocalBinder(WebServerService service)
            {
                _service = service;
            }

            public WebServerService Instance => _service;
        }

        public WebServerService()
        {
            _localBinder = new LocalBinder(this);
        }

        public override IBinder? OnBind(Intent? intent)
        {
            return _localBinder;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            var builder = WebApplication.CreateBuilder();

            builder.WebHost.ConfigureKestrel(op => op.ListenAnyIP(8080));

            builder.Services.AddSingleton<IXrThread>(new HandlerXrThread(new Handler(Looper.MainLooper!)));

            builder.Services.AddSingleton(GlobalServices.App!);

            builder.Services.AddOpenXrWebLink();

            _webApp = builder.Build();

            _webApp.UseOpenXrWebLink();

            GlobalServices.ServiceProvider = _webApp.Services;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            _isActive = true;

            _ = _webApp!.RunAsync();

            return StartCommandResult.Sticky;
        }

        public override async void OnDestroy()
        {
            await _webApp!.StopAsync();
            
            _isActive = false;

            base.OnDestroy();
        }

        public bool IsActive => _isActive;
       
    }
}
