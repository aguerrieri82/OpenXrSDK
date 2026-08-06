using Common.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TurboJpeg;
using XrEngine.Media;
using XrMath;
using static XrEngine.Devices.UsbCameraLib;

namespace XrEngine.Devices
{
    public sealed class UsbCamera : ICameraDevice, IDisposable
    {
        private readonly int _fd;
        private readonly int _vendorId;
        private readonly int _productId;

        private readonly object _frameLock = new();

        private CameraHandle _handle;

        private readonly List<VideoFormat> _formats = new();
        private readonly List<FormatInfo> _nativeFormats = new();

        private CancellationTokenSource? _cancel;
        private Task? _captureTask;

        private byte[]? _readyBuffer;
        private byte[]? _writeBuffer;

        private int _readySize;
        private int _lastWidth;
        private int _lastHeight;
        private ImageFormat _lastFormat;

        private long _lastFrame;
        private long _lastTimestamp;

        private int _selectedNativeFormatIndex = -1;
        private Texture2D? _outTexture;
        private readonly List<int> _formatNativeIndexes = new();
        private readonly List<bool> _formatDecodeMjpg = new();

        private nint _jpegDecoder;

        private byte[]? _jpegBuffer;
        private long _lastUpdateFrame;
        private bool _isClosing;
        private readonly Action<IMemoryBuffer<byte>> _getDataAction;

        public UsbCamera(int fd, int vendorId, int productId)
        {
            _fd = fd;
            _vendorId = vendorId;
            _productId = productId;
            _getDataAction = GetLastFrameData;
        }

        public Task OpenAsync()
        {
            Log.Info(this, "Open Camera");

            _handle = UsbCameraLib.Create();

            if (_handle.IsNull)
                throw new InvalidOperationException("CameraNative.Create failed");

            Check(_handle.Init(true, false));
            Check(_handle.OpenDeviceFd(_fd, _vendorId, _productId));

            RefreshFormats();

            Log.Info(this, "Open Camera OK!");

            return Task.CompletedTask;
        }

        public IList<VideoFormat> GetSupportedFormats()
        {
            if (_formats.Count == 0)
                RefreshFormats();

            return _formats;
        }

        public Task StartCaptureAsync(
            VideoFormat format,
            Texture2D? outTexture = null,
            NativeSurface? outSurface = null)
        {

            Log.Info(this, "StartCapture");

            if (outSurface != null)
                throw new NotSupportedException();

            if (_handle.IsNull)
                throw new InvalidOperationException("Camera is not open.");

            if (_captureTask != null)
                throw new InvalidOperationException("Capture already started.");

            _selectedNativeFormatIndex = FindFormat(format);

            _outTexture = outTexture;

            var nativeListIndex = _formatNativeIndexes[_selectedNativeFormatIndex];
            var decodeMjpg = _formatDecodeMjpg[_selectedNativeFormatIndex];

            var nativeFormat = _nativeFormats[nativeListIndex];

            if (decodeMjpg && _jpegDecoder == 0)
            {
                _jpegDecoder = TurboJpegLib.tjInitDecompress();

                if (_jpegDecoder == 0)
                    throw new InvalidOperationException("tjInitDecompress failed");
            }

            Check(_handle.OpenStreamByFormatIndex(
                nativeFormat.Index,
                (uint)Math.Round(format.FrameRate)));

            Check(_handle.StartStream());

            _cancel = new CancellationTokenSource();
            _captureTask = Task.Run(() => CaptureLoop(_cancel.Token));

            Log.Info(this, "StartCapture OK!");

            return Task.CompletedTask;
        }

        public void StopCapture()
        {
            Log.Info(this, "StopCapture");

            var cancel = _cancel;
            var task = _captureTask;

            _cancel = null;
            _captureTask = null;

            if (cancel != null)
            {
                cancel.Cancel();

                try
                {
                    task?.Wait();
                }
                catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
                {
                }

                cancel.Dispose();
            }

            if (!_handle.IsNull)
            {
                _handle.StopStream();
                _handle.CloseStream();
            }

            _selectedNativeFormatIndex = -1;
        }

        public void Close()
        {
            if (_isClosing)
                return;

            _isClosing = true;

            Log.Info(this, "Close");

            StopCapture();

            if (!_handle.IsNull)
            {
                _handle.CloseDevice();
                _handle.Shutdown();
                _handle.Destroy();
                _handle = default;
            }

            _formats.Clear();
            _nativeFormats.Clear();

            lock (_frameLock)
            {
                _readySize = 0;
                _lastWidth = 0;
                _lastHeight = 0;
                _lastFormat = default;
            }

            if (_jpegDecoder != 0)
            {
                TurboJpegLib.tjDestroy(_jpegDecoder);
                _jpegDecoder = 0;
            }

            _isClosing = false;
        }

        public void Dispose()
        {
            Close();
        }

        public void Configure(CameraConfiguration configuration)
        {
            if (configuration.ExpositionTimeNs.Mode != CameraParamMode.Unset ||
                configuration.SensitivityIso.Mode != CameraParamMode.Unset)
            {
                throw new NotSupportedException();
            }
        }

        public CameraParams GetParams()
        {
            return new CameraParams
            {
                CurrentSize = new Size2I((uint)_lastWidth, (uint)_lastHeight)
            };
        }

        public void UpdateTexture()
        {
            if (_outTexture == null)
                throw new NotSupportedException();

            lock (_frameLock)
            {
                if (_readyBuffer == null)
                    return;

                if (_lastFormat != ImageFormat.Rgb32)
                    return;

                if (_lastUpdateFrame == _lastFrame)
                    return;

                Log.Debug(this, "Update Texture");

                _outTexture.LoadData(new TextureData
                {
                    Width = (uint)_lastWidth,
                    Height = (uint)_lastHeight,
                    Format = TextureFormat.Rgba8,
                    Content = MemoryBuffer.Create(_readyBuffer)
                });

                _lastUpdateFrame = _lastFrame;
            }
        }

        private void RefreshFormats()
        {
            Log.Info(this, "RefreshFormats");

            if (_handle.IsNull)
                throw new InvalidOperationException("Camera is not open.");

            Check(_handle.RefreshFormats());

            var count = _handle.GetFormatCount();

            _formats.Clear();
            _nativeFormats.Clear();
            _formatNativeIndexes.Clear();
            _formatDecodeMjpg.Clear();

            for (var i = 0; i < count; i++)
            {
                Check(_handle.GetFormatInfo(i, out var info));

                var nativeIndex = _nativeFormats.Count;
                _nativeFormats.Add(info);

                var nativeImageFormat = ToImageFormat(info.FrameFormat);

                _formatNativeIndexes.Add(nativeIndex);
                _formatDecodeMjpg.Add(false);

                _formats.Add(new VideoFormat
                {
                    Width = info.Width,
                    Height = info.Height,
                    FrameRate = info.DefaultFps,
                    IsFlipV = 0,
                    ImageFormat = nativeImageFormat,
                    RowStride = GetRowStride(info.Width, nativeImageFormat),
                    ImageSize = GetImageSize(info.Width, info.Height, nativeImageFormat)
                });

                Log.Info(
                     this,
                     "USB native format {0}: {1}x{2} {3}fps {4}, fpsCount={5}, minFps={6}, maxFps={7}, stepFps={8}, descriptor={9}, formatIndex={10}, frameIndex={11}",
                     i,
                     info.Width,
                     info.Height,
                     info.DefaultFps,
                     info.FrameFormat,
                     info.FpsCount,
                     info.MinFps,
                     info.MaxFps,
                     info.StepFps,
                     info.DescriptorSubtype,
                     info.FormatIndex,
                     info.FrameIndex);

                if (nativeImageFormat == ImageFormat.MJPG)
                {
                    _formatNativeIndexes.Add(nativeIndex);
                    _formatDecodeMjpg.Add(true);

                    _formats.Add(new VideoFormat
                    {
                        Width = info.Width,
                        Height = info.Height,
                        FrameRate = info.DefaultFps,
                        IsFlipV = 0,
                        ImageFormat = ImageFormat.Rgb32,
                        RowStride = info.Width * 4,
                        ImageSize = info.Width * info.Height * 4
                    });
                }
            }

        }

        private int FindFormat(VideoFormat format)
        {
            for (var i = 0; i < _formats.Count; i++)
            {
                var cur = _formats[i];

                if (cur.Width == format.Width &&
                    cur.Height == format.Height &&
                    cur.ImageFormat == format.ImageFormat &&
                    Math.Abs(cur.FrameRate - format.FrameRate) < 0.5)
                {
                    return i;
                }
            }

            throw new NotSupportedException();
        }

        private void EnsureJpegBuffer(int size)
        {
            if (_jpegBuffer == null || _jpegBuffer.Length < size)
                _jpegBuffer = new byte[size];
        }

        private unsafe void CaptureLoop(CancellationToken cancel)
        {
            var decodeMjpg = _selectedNativeFormatIndex >= 0 &&
                              _formatDecodeMjpg[_selectedNativeFormatIndex];

            var poolFailCount = 0;

            Thread.Sleep(500);

            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var frame = new FrameInfo();
                    var res = _handle.PullFrame(1000, ref frame);

                    if (res < 0)
                    {
                        if (cancel.IsCancellationRequested)
                            return;

                        Log.Warn(this, $"Pull Frame Failed: {0}", _handle.GetLastError());

                        poolFailCount++;

                        if (poolFailCount > 5)
                            throw new Exception("Unable to read");

                        continue;
                    }

                    if (frame.Data == 0 || frame.DataBytes <= 0)
                        continue;

                    poolFailCount = 0;

                    Log.Debug(this, $"New Frame! D: {frame.Data:X} S: {((IntPtr)(void*)&frame):X}");

                    var nativeFormat = ToImageFormat(frame.FrameFormat);

                    if (decodeMjpg)
                    {
                        if (nativeFormat != ImageFormat.MJPG)
                            throw new NotSupportedException($"Expected MJPG frame, got {nativeFormat}.");

                        var decodedSize = frame.Width * frame.Height * 4;

                        EnsureBuffers(decodedSize);
                        EnsureJpegBuffer(frame.DataBytes);

                        Marshal.Copy(frame.Data, _jpegBuffer!, 0, frame.DataBytes);

                        DecodeMjpgToRgb32(_jpegBuffer!, frame.DataBytes, _writeBuffer!, frame.Width, frame.Height);

                        PublishFrame(
                            frame.Width,
                            frame.Height,
                            ImageFormat.Rgb32,
                            decodedSize);
                    }
                    else
                    {
                        EnsureBuffers(frame.DataBytes);

                        Marshal.Copy(frame.Data, _writeBuffer!, 0, frame.DataBytes);

                        PublishFrame(
                            frame.Width,
                            frame.Height,
                            nativeFormat,
                            frame.DataBytes);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(this, ex, "Pull {0}");

                    Task.Run(() => Close());
                }
            }
        }

        private void EnsureBuffers(int size)
        {
            if (_readyBuffer == null || _readyBuffer.Length < size)
                _readyBuffer = new byte[size];

            if (_writeBuffer == null || _writeBuffer.Length < size)
                _writeBuffer = new byte[size];
        }

        private void PublishFrame(int width, int height, ImageFormat format, int size)
        {
            var timestamp = Stopwatch.GetTimestamp();

            lock (_frameLock)
            {
                var oldReady = _readyBuffer;
                _readyBuffer = _writeBuffer;
                _writeBuffer = oldReady;

                _readySize = size;
                _lastWidth = width;
                _lastHeight = height;
                _lastFormat = format;
                _lastTimestamp = timestamp;
                _lastFrame++;

            }

            NewImage?.Invoke(new CaptureImage
            {
                Native = this,
                TimeStamp = timestamp,
                GetData = _getDataAction,
                Width = width,
                Height = height,
                Format = format
            });
        }

        private void GetLastFrameData(IMemoryBuffer<byte> dst)
        {
            lock (_frameLock)
            {
                if (_readyBuffer == null || _readySize == 0)
                    return;

                _readyBuffer.AsSpan(0, _readySize).CopyTo(dst.AsSpan());
            }
        }

        private unsafe void DecodeMjpgToRgb32(
            byte[] jpeg,
            int jpegSize,
            byte[] dst,
            int expectedWidth,
            int expectedHeight)
        {
            fixed (byte* pJpeg = jpeg)
            fixed (byte* pDst = dst)
            {

                var res = TurboJpegLib.tjDecompressHeader2(
                    _jpegDecoder,
                    pJpeg,
                    (ulong)jpegSize,
                    out var width,
                    out var height,
                    out _);

                Log.Debug(this, $"Bytes: {pJpeg[0]},{pJpeg[1]},{pJpeg[2]}");

                if (width != expectedWidth || height != expectedHeight)
                    throw new InvalidOperationException($"MJPG decoded size changed: {res}: {width}x{height} - {expectedWidth}x{expectedHeight}.");

                res = TurboJpegLib.tjDecompress2(
                    _jpegDecoder,
                    pJpeg,
                    (ulong)jpegSize,
                    pDst,
                    expectedWidth,
                    expectedWidth * 4,
                    expectedHeight,
                    TurboJpegLib.TJPF.TJPF_RGBA,
                    TurboJpegLib.TJFLAG.TJFLAG_FASTDCT |
                    TurboJpegLib.TJFLAG.TJFLAG_FASTUPSAMPLE);
            }
        }

        private void Check(int result)
        {
            if (result < 0)
                throw new InvalidOperationException(_handle.GetLastError());
        }

        private static ImageFormat ToImageFormat(UvcFrameFormat frameFormat)
        {
            return frameFormat switch
            {
                UvcFrameFormat.Mjpeg => ImageFormat.MJPG,
                UvcFrameFormat.Yuyv => ImageFormat.YUY2,
                UvcFrameFormat.Nv12 => ImageFormat.NV12,
                UvcFrameFormat.H264 => ImageFormat.H264,
                _ => ImageFormat.Unknown
            };
        }

        private static int GetRowStride(int width, ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Rgb24 => width * 3,
                ImageFormat.Rgb32 => width * 4,
                ImageFormat.YUY2 => width * 2,
                ImageFormat.NV12 => width,
                ImageFormat.I420 => width,
                ImageFormat.YV12 => width,
                ImageFormat.MJPG => 0,
                ImageFormat.H264 => 0,
                _ => 0
            };
        }

        private static int GetImageSize(int width, int height, ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Rgb24 => width * height * 3,
                ImageFormat.Rgb32 => width * height * 4,
                ImageFormat.YUY2 => width * height * 2,
                ImageFormat.NV12 => width * height * 3 / 2,
                ImageFormat.I420 => width * height * 3 / 2,
                ImageFormat.YV12 => width * height * 3 / 2,
                ImageFormat.MJPG => 0,
                ImageFormat.H264 => 0,
                _ => 0
            };
        }

        public event Action<CaptureImage>? NewImage;

        public CameraDeviceCaps Caps => 0;

        public NativeSurface FrameSurface => throw new NotSupportedException();

        public long LastFrame => Interlocked.Read(ref _lastFrame);

        public long LastTimestamp => Interlocked.Read(ref _lastTimestamp);

        public bool IsOpen => !_handle.IsNull;

        public bool IsCapturing => _captureTask != null && !_captureTask.IsCompleted;
    }
}