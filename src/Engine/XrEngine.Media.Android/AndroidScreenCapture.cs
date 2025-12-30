using Android.Content;
using Android.Hardware.Display;
using Android.Media.Projection;
using Android.OS;
using Android.Util;
using Android.Views;
using XrInteraction;
using ContextA = global::Android.Content.Context;

namespace XrEngine.Media.Android
{

    public class AndroidScreenCapture : IScreenCapture
    {
        sealed class ProjectionCallback : MediaProjection.Callback
        {
            readonly Action _onStop;

            public ProjectionCallback(Action onStop) => _onStop = onStop;

            public override void OnStop()
            {
                _onStop();
            }
        }

        private MediaProjection? _mediaProjection;
        private VirtualDisplay? _virtualDisplay;

        public AndroidScreenCapture()
        {

        }
        static DisplayMetrics GetRealMetrics()
        {
            var dm = (DisplayManager)Application.Context.GetSystemService(ContextA.DisplayService)!;


            var display = dm.GetDisplay(Display.DefaultDisplay)!;

            var metrics = new DisplayMetrics();
            display.GetRealMetrics(metrics);
            return metrics;
        }

        public Task<bool> StartCaptureAsync(ScreenCaptureOptions options)
        {
            var manager = (MediaProjectionManager)Application.Context.GetSystemService(ContextA.MediaProjectionService)!;
            var intent = manager.CreateScreenCaptureIntent();
            var main = Context.Require<IMainActivity>();

            var taskComp = new TaskCompletionSource<bool>();

            ProjectionService._mpSource = (mp, ex) =>
            {
                _mediaProjection = mp;

                if (ex != null)
                {
                    taskComp.SetException(ex);
                    return;
                }

                try
                {
                    var metrics = GetRealMetrics();

                    _mediaProjection!.RegisterCallback(new ProjectionCallback(OnStop), null);

                    _virtualDisplay = _mediaProjection.CreateVirtualDisplay(
                         "ScreenCapture",
                         options.Width == 0 ? metrics.WidthPixels : (int)options.Width,
                         options.Height == 0 ? metrics.HeightPixels : (int)options.Height,
                         (int)metrics.DensityDpi,
                         (DisplayFlags)VirtualDisplayFlags.AutoMirror,
                         (Surface)options.OutSurface.Native!,
                         null, null);

                    taskComp.SetResult(true);
                }
                catch (Exception ex2)
                {
                    taskComp.SetException(ex2);
                }

            };

            main.StartActivityForResult(intent, 300, (result, intent) =>
            {
                if (result == Result.Ok && intent != null)
                {
                    var svc = new Intent(main.Context, typeof(ProjectionService));
                    svc.PutExtra("ResultCode", (int)result);
                    svc.PutExtra("Data", intent);

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                        main.StartForegroundService(svc);
                    else
                        main.StartService(svc);
                }
                else
                    taskComp.SetResult(false);
            });

            return taskComp.Task;
        }

        protected void OnStop()
        {

        }

        public void StopCapture()
        {
            _mediaProjection?.Stop();
            _mediaProjection = null;
            _virtualDisplay = null;
        }
    }
}
