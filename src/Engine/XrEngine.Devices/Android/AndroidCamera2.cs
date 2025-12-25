

#if ANDROID28_0_OR_GREATER

using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Common.Interop;
using Java.Util.Concurrent;
using System.Runtime.Versioning;
using XrEngine.Media;
using Debug = System.Diagnostics.Debug;
using ImageReaderA = Android.Media.ImageReader;

namespace XrEngine.Devices.Android

{
    [SupportedOSPlatform("android28.0")]
    public class AndroidCamera2 : ICameraDevice, IDisposable
    {
        class CameraDeviceState : CameraDevice.StateCallback
        {
            readonly AndroidCamera2 _host;

            public CameraDeviceState(AndroidCamera2 host)
            {
                _host = host;
            }

            public override void OnDisconnected(CameraDevice camera)
            {
                _host.OnDisconnected();
            }

            public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
            {
                _host._openSource?.SetException(new InvalidOperationException(error.ToString()));
            }

            public override void OnOpened(CameraDevice camera)
            {
                _host._openSource?.SetResult(camera);
            }
        }

        class CameraCaptureSessionState : CameraCaptureSession.StateCallback
        {
            readonly AndroidCamera2 _host;

            public CameraCaptureSessionState(AndroidCamera2 host)
            {
                _host = host;
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                _host._sessionSource!.SetResult(session);
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                _host._sessionSource!.SetException(new InvalidOperationException());
            }
        }

        class ImageAvailableListener : Java.Lang.Object, ImageReaderA.IOnImageAvailableListener
        {
            readonly AndroidCamera2 _host;

            public ImageAvailableListener(AndroidCamera2 host)
            {
                _host = host;
            }

            public void OnImageAvailable(ImageReaderA? reader)
            {
                using var image = reader?.AcquireLatestImage();

                if (image != null)
                {
                    _host.OnNewImage(image);

                    image.Close();
                }
            }
        }


        protected string _deviceId;
        protected CameraManager _manager;
        protected TaskCompletionSource<CameraDevice>? _openSource;
        protected TaskCompletionSource<CameraCaptureSession>? _sessionSource;
        protected CameraDevice? _device;
        protected IExecutorService? _executor;
        protected ImageReaderA? _reader;
        protected CameraCaptureSession? _session;
        protected HandlerThread? _backgroundThread;
        protected Handler? _backgroundHandler;
        protected int? _imageSize;
        protected SurfaceTexture? _surfaceTex;

        public AndroidCamera2(string deviceId, CameraManager manager)
        {
            _manager = manager;
            _deviceId = deviceId;

        }

        public IList<VideoFormat> GetSupportedFormats()
        {
            var result = new List<VideoFormat>();

            var chars = _manager.GetCameraCharacteristics(_deviceId);

            var map = (StreamConfigurationMap?)chars.Get(CameraCharacteristics.ScalerStreamConfigurationMap)!;


            foreach (var format in map.GetOutputFormats()!)
            {
                var imgFormat = FromAndroid((ImageFormatType)format);
                if (imgFormat == null)
                    continue;

                var sizes = map.GetOutputSizes(format)!;

                foreach (var size in sizes)
                {
                    var minDurationNs = map.GetOutputMinFrameDuration(format, size);

                    var maxFps = 0;
                    if (minDurationNs > 0)
                        maxFps = (int)(1_000_000_000.0 / minDurationNs);

                    var item = new VideoFormat()
                    {
                        Width = size.Width,
                        Height = size.Height,
                        ImageFormat = imgFormat.Value,
                        FrameRate = maxFps
                    };

                    result.Add(item);

                }
            }

            return result;
        }

        public async Task OpenAsync()
        {
            _backgroundThread = new HandlerThread("CameraBackground");
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper!);

            _openSource = new TaskCompletionSource<CameraDevice>();
            _manager.OpenCamera(_deviceId, new CameraDeviceState(this), new Handler(Looper.MainLooper!));
            _device = await _openSource.Task;
        }

        public async Task StartCaptureAsync(VideoFormat format, Texture2D? outTexture = null)
        {
            Debug.Assert(_device != null);

            _executor = Executors.NewSingleThreadExecutor()!;

            _reader = ImageReaderA.NewInstance(format.Width, format.Height, ToAndroid(format.ImageFormat), 2);
            _reader.SetOnImageAvailableListener(new ImageAvailableListener(this), _backgroundHandler);

            List<OutputConfiguration> outs = [new OutputConfiguration(_reader.Surface!)];

            Surface? texSurface = null;

            if (outTexture != null)
            {
                var glText = outTexture!.Handle;

                _surfaceTex = new SurfaceTexture((int)glText);
                _surfaceTex.SetDefaultBufferSize(format.Width, format.Height);

                texSurface = new Surface(_surfaceTex);

                outTexture.Width = (uint)format.Width;
                outTexture.Height = (uint)format.Height;

                outs.Add(new OutputConfiguration(texSurface));
            }

            var config = new SessionConfiguration(
                (int)SessionType.Regular,
                outs,
                _executor,
                new CameraCaptureSessionState(this));

            _sessionSource = new();

            _device.CreateCaptureSession(config);

            _session = await _sessionSource.Task;

            var captureRequest = _device.CreateCaptureRequest(CameraTemplate.Record);
            captureRequest.AddTarget(_reader.Surface!);

            if (texSurface != null)
                captureRequest.AddTarget(texSurface);

            _session.SetRepeatingRequest(captureRequest.Build(), null, null);
        }

        public void StopCapture()
        {
            _executor?.Shutdown();
            _executor = null;

            _session?.Close();
            _session = null;
            _imageSize = null;
        }

        public void Close()
        {
            StopCapture();

            _device?.Close();
            _device = null;

            _surfaceTex?.Dispose();
            _surfaceTex = null;

            _backgroundThread?.QuitSafely();

            try
            {
                _backgroundThread?.Join();
            }
            catch
            {
            }

            _backgroundThread = null;
            _backgroundHandler = null;
        }

        protected virtual void OnDisconnected()
        {

        }

        protected virtual void OnNewImage(Image image)
        {
            NewImage?.Invoke(new CaptureImage
            {
                TimeStamp = image.Timestamp,
                Width = image.Width,
                Height = image.Height,
                Format = FromAndroid(image.Format)!.Value,
                Native = image,
                GetData = buffer => GetImageData(image, buffer)
            });
        }

        public void UpdateTexture()
        {
            _surfaceTex?.UpdateTexImage();

        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }


        public event Action<CaptureImage>? NewImage;

        public bool CanRenderOnTexture => true;


        unsafe void GetImageData(Image image, IMemoryBuffer<byte> buffer)
        {
            var planes = image.GetPlanes()!;

            if (_imageSize == null)
            {
                _imageSize = 0;

                foreach (var plane in planes!)
                {
                    _imageSize += plane.Buffer!.Limit();

                    if (plane.PixelStride == 2)
                        break;
                }
            }

            if (buffer.Size < _imageSize)
                buffer.Allocate((uint)_imageSize);

            var dstSpan = buffer.AsSpan();

            var offset = 0;

            foreach (var plane in planes!)
            {
                var data = (byte*)plane.Buffer!.GetDirectBufferAddress();
                var pSrc = new Span<byte>(data, plane.Buffer.Limit());

                pSrc.CopyTo(dstSpan[offset..]);

                offset += pSrc.Length;

                if (plane.PixelStride == 2)
                    break;
            }
        }

        static ImageFormatType ToAndroid(Media.ImageFormat format)
        {
            return format switch
            {
                Media.ImageFormat.Rgb24 => ImageFormatType.FlexRgb888,
                Media.ImageFormat.Rgb32 => ImageFormatType.FlexRgba8888,
                Media.ImageFormat.Yuv420888 => ImageFormatType.Yuv420888,
                _ => throw new NotSupportedException()
            };
        }

        static Media.ImageFormat? FromAndroid(ImageFormatType format)
        {
            return format switch
            {
                ImageFormatType.Yuv420888 => Media.ImageFormat.Yuv420888,
                ImageFormatType.Jpeg => Media.ImageFormat.MJPG,
                _ => null
            };
        }
    }
}

#endif