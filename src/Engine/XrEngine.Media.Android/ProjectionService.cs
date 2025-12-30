using Android.Content;
using Android.Content.PM;
using Android.Media.Projection;
using Android.OS;


namespace XrEngine.Media.Android
{
    [Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaProjection)]
    public class ProjectionService : Service
    {
        const int NotifId = 10001;
        const string ChannelId = "projection";

        MediaProjection? _projection;

        public override IBinder? OnBind(Intent? intent) => null;

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            StartAsForeground();

            var mpm = (MediaProjectionManager)GetSystemService(MediaProjectionService)!;

            var resultCode = intent?.GetIntExtra("ResultCode", (int)Result.Canceled) ?? (int)Result.Canceled;
            var data = GetIntentExtraIntent(intent, "Data");

            try
            {
                _projection = mpm.GetMediaProjection(resultCode, data!);
                _mpSource?.Invoke(_projection, null);
            }
            catch (Exception ex)
            {
                _mpSource?.Invoke(null, ex);
                StopSelf();
            }

            _mpSource = null;

            return StartCommandResult.NotSticky;
        }

        void StartAsForeground()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var nm = (NotificationManager)GetSystemService(NotificationService)!;
                var chan = new NotificationChannel(ChannelId, "Screen capture", NotificationImportance.Low);
                nm.CreateNotificationChannel(chan);
            }

            var notif = new Notification.Builder(this, ChannelId)
                .SetContentTitle("Screen capture running")
                .SetOngoing(true)
                //.SetSmallIcon(Android.Resource.Drawable.IcMenuCamera)
                .Build();


            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                StartForeground(NotifId, notif, ForegroundService.TypeMediaProjection);
            }
            else
            {
                StartForeground(NotifId, notif);
            }
        }

        static Intent? GetIntentExtraIntent(Intent? outer, string key)
        {
            if (outer == null)
                return null;


            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                return (Intent?)outer.GetParcelableExtra(key, Java.Lang.Class.FromType(typeof(Intent)));
            else
                return (Intent?)outer.GetParcelableExtra(key);
        }

        public override void OnDestroy()
        {
            try { _projection?.Stop(); } catch { }
            _projection = null;
            base.OnDestroy();
        }


        internal static Action<MediaProjection?, Exception?>? _mpSource;
    }
}
