using Android.Hardware.Display;
using Android.Media.Projection;
using Android.Util;
using Android.Views;
using XrInteraction;
using ContextA = global::Android.Content.Context;

namespace XrEngine.Media.Android
{

    public class AndroidScreenCapture : IScreenCapture
    {

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

            main.StartActivityForResult(intent, 300, (result, intent) =>
            {
                if (result == Result.Ok && intent != null)
                {
                    _mediaProjection = manager.GetMediaProjection(
                        (int)result,
                        intent
                    )!;

                    var metrics = GetRealMetrics();

                    _virtualDisplay = _mediaProjection.CreateVirtualDisplay(
                         "ScreenCapture",
                         (int)options.Width,
                         (int)options.Height,
                         (int)metrics.DensityDpi,
                         (DisplayFlags)VirtualDisplayFlags.AutoMirror,
                         (Surface)options.OutSurface.Native!,
                         null, null);

                    taskComp.SetResult(true);
                }
                else
                    taskComp.SetResult(false);
            });

            return taskComp.Task;
        }

        public void StopCapture()
        {
            _mediaProjection?.Stop();
            _mediaProjection = null;
            _virtualDisplay = null;
        }
    }
}
