using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Usb;
using Android.Runtime;
using Android.Util;
using Common.Interop;
using XrEngine;
using XrEngine.Android.Devices;
using XrEngine.Devices;
using XrEngine.OpenXr;
using static Android.Views.Choreographer;
using ImageFormat = XrEngine.Media.ImageFormat;
using Log = Android.Util.Log;

namespace EngineSamples.Android
{
    [Activity(
        Name = "net.eusoft.xrengine.MainActivity",
        Label = "@string/app_name",
        MainLauncher = true)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/usb_device_filter")]
    public class MainActivity : Activity
    {
        private AndroidUsbCameraManager? _usbCameraManager;
        private CancellationTokenSource? _cameraWatchCancel;
        private Task? _cameraWatchTask;

        private ICameraDevice? _camera;

        private ImageView? _imageView;

        private Bitmap? _bitmap;
        private IMemoryBuffer<byte>? _frameBytes;
        private Java.Nio.ByteBuffer? _bitmapBuffer;

        private int _uiFramePending;

        public MainActivity()
        {
            Context.Implement<IProgressLogger>(new AndroidProgressLogger());
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            _imageView = FindViewById<ImageView>(Resource.Id.preview);

            _usbCameraManager = new AndroidUsbCameraManager(this);

        }

        protected override void OnStart()
        {
            base.OnStart();

            _usbCameraManager?.Start();

            _cameraWatchCancel = new CancellationTokenSource();
            _cameraWatchTask = WatchCameraAsync(_cameraWatchCancel.Token);
        }

        protected override void OnStop()
        {
            _cameraWatchCancel?.Cancel();

            _camera?.StopCapture();
            _camera?.Close();
            _camera = null;

            _usbCameraManager?.Stop();

            base.OnStop();
        }

        protected override void OnDestroy()
        {
            _cameraWatchCancel?.Cancel();
            _cameraWatchCancel?.Dispose();
            _cameraWatchCancel = null;

            _usbCameraManager?.Dispose();
            _usbCameraManager = null;

            base.OnDestroy();
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            _usbCameraManager?.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        private async Task WatchCameraAsync(CancellationToken cancel)
        {
            await Task.Delay(400);

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    if (_camera != null && !_camera.IsOpen)
                        _camera = null;

                    if (_camera != null && _camera.IsOpen)
                    {
                        await Task.Delay(400, cancel);
                        continue;
                    }

                    var manager = _usbCameraManager;
                    if (manager == null)
                        return;

                    var cameras = manager.GetCameras();

                    if (cameras.Count == 0)
                    {
                        await Task.Delay(1000, cancel);
                        continue;
                    }

                    var info = cameras[0];

                    Log.Info("UsbCamera", $"Opening USB camera {info.Id} {info.Name}");

                    var camera = await manager.OpenCameraAsync(info.Id!);

                    var formats = camera.GetSupportedFormats();

                    foreach (var fmt in formats)
                    {
                        Log.Info(
                            "UsbCamera",
                            $"Format {fmt.Width}x{fmt.Height} {fmt.FrameRate:0.##}fps {fmt.ImageFormat} size={fmt.ImageSize} stride={fmt.RowStride}");
                    }

                    var selected = formats
                        .Where(a=> a.Width < 1000)
                        .OrderByDescending(a => a.Width * a.Height)
                        .ThenByDescending(a => a.FrameRate)
                        .First(a => a.ImageFormat == XrEngine.Media.ImageFormat.Rgb32);

                    Log.Info(
                        "UsbCamera",
                        $"Selected {selected.Width}x{selected.Height} {selected.FrameRate:0.##}fps {selected.ImageFormat}");

                    await Task.Delay(200, cancel);

                    await camera.StartCaptureAsync(selected);

                    var frameData = new byte[selected.Width * selected.Height * 4];

                    _frameBytes = MemoryBuffer.Create(frameData);

                    _bitmap = Bitmap.CreateBitmap(
                        selected.Width,
                        selected.Height,
                        Bitmap.Config.Argb8888!);


                    camera.NewImage += OnNewImage;

                    _camera = camera;

                    Log.Info("UsbCamera", "USB camera started");

                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error("UsbCamera", ex.ToString());

                    _camera?.Close();

                    _camera = null;

                    await Task.Delay(1000, cancel);
                }
            }
        }

        private void OnNewImage(CaptureImage image)
        {
            if (_frameBytes == null || _bitmap == null)
                return;

            if (image.Format != ImageFormat.Rgb32)
                return;

            if (Interlocked.Exchange(ref _uiFramePending, 1) != 0)
                return;

            image.GetData?.Invoke(_frameBytes);

            RunOnUiThread(() =>
            {
                try
                {

                    using var bitmapBuffer = Java.Nio.ByteBuffer.Wrap(_frameBytes.AsArray());
                    _bitmap.CopyPixelsFromBuffer(bitmapBuffer);
                    _imageView!.SetImageBitmap(_bitmap);
                }
                finally
                {
                    Interlocked.Exchange(ref _uiFramePending, 0);
                }
            });
        }
    }
}