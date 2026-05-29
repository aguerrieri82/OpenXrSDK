using Common.Interop;
using OpenXr.Framework;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.Json;
using XrEngine.Devices;
using XrEngine.Media;
using XrEngine.OpenGL;
using XrMath;


namespace XrEngine.Reconstruct
{

    public class XrReconstructRecorder
    {
        private int _frame;
        private ICameraDevice? _cameraLeft;
        private ICameraDevice? _cameraRight;
        private NativeSurface _leftSurface;
        private NativeSurface _rightSurface;
        private NativeSurface _screenSurface;
        private IVideoRecorder? _leftRecorder;
        private IVideoRecorder? _rightRecorder;
        private IVideoRecorder? _screenRecorder;
        private IScreenCapture? _capture;
        private bool _isRecording;
        private readonly PerspectiveCamera? _depthCamera;
        private readonly IMemoryBuffer<byte>[]? _buffers;
        private GZipStream? _zStream;
        private FileStream? _metaStream;
        private readonly Texture2D _leftTex;
        private readonly Texture2D _rightTex;
        protected RecordStats? _stats;
        private string? _outPath;

        static readonly JsonSerializerOptions JSON_OPT = new JsonSerializerOptions()
        {
            IncludeFields = true,
        };

        public XrReconstructRecorder()
        {
            _leftTex = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            _rightTex = new Texture2D
            {
                Format = TextureFormat.Rgba32,
                WrapT = WrapMode.ClampToEdge,
                WrapS = WrapMode.ClampToEdge,
                MagFilter = ScaleFilter.Linear,
                MinFilter = ScaleFilter.Linear,
                Type = TextureType.External
            };

            _depthCamera = new PerspectiveCamera();

            _buffers = [
                MemoryBuffer.Create<byte>(16),
                MemoryBuffer.Create<byte>(16)];
        }

        public async Task StartCaptureAsync(string outPath)
        {
            _outPath = outPath;

            var manager = Context.Require<ICameraManager>();
            var outZPath = Path.Combine(outPath, "out-z.bin");
            var outMetaPath = Path.Combine(outPath, "out-meta.json");
            var outPath1 = Path.Combine(outPath, "outL.mp4");
            var outPath2 = Path.Combine(outPath, "outR.mp4");
            var outPath3 = Path.Combine(outPath, "outScr.mp4");

            foreach (var file in new string[] { outZPath, outMetaPath, outPath1, outPath2, outPath3 })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            _zStream = new GZipStream(File.OpenWrite(outZPath), CompressionLevel.Fastest);

            _metaStream = File.OpenWrite(outMetaPath);

            _frame = 0;

            var cameras = manager.GetCameras();

            var infoLeft = cameras.First(a => a.Source == 0 && a.Position == 0);
            var infoRight = cameras.First(a => a.Source == 0 && a.Position == 1);

            _cameraLeft = await manager.OpenCameraAsync(infoLeft.Id!);
            _cameraRight = await manager.OpenCameraAsync(infoRight.Id!);

            var formats = _cameraLeft.GetSupportedFormats();

            var curFormat = formats.Last();

            var recOptions = new VideoRecordOptions
            {
                Format = VideoRecordFormat.Mp4,
                Height = curFormat.Height,
                Width = curFormat.Width,
                FrameRate = (int)curFormat.FrameRate,
                MimeType = "video/avc",
                IFrameInterval = 1,
                BitRate = 6000000
            };

            _leftRecorder = Context.RequireNew<IVideoRecorder>();
            _rightRecorder = Context.RequireNew<IVideoRecorder>();
            _screenRecorder = Context.RequireNew<IVideoRecorder>();

            _leftSurface = _leftRecorder.StartRecording(outPath1, recOptions);
            _rightSurface = _rightRecorder.StartRecording(outPath2, recOptions);


            _screenSurface = _screenRecorder.StartRecording(outPath3, new VideoRecordOptions
            {
                Format = VideoRecordFormat.Mp4,
                Width = 1200,
                Height = (int)(1200 * 1.0808f),
                FrameRate = 60,
                MimeType = "video/avc",
                IFrameInterval = 1,
                BitRate = 6000000
            });

            _stats = new RecordStats();

            _cameraLeft.NewImage += OnNewImage;

            await _cameraLeft.StartCaptureAsync(curFormat, _leftTex, _leftSurface);
            await _cameraRight.StartCaptureAsync(curFormat, _rightTex, _rightSurface);


            _capture = Context.RequireNew<IScreenCapture>();

            await _capture.StartCaptureAsync(new ScreenCaptureOptions
            {
                Width = 1280,
                Height = 1280,
                OutSurface = _screenSurface,
            });


            _isRecording = true;
        }

        private void OnNewImage(CaptureImage obj)
        {
            var pose = XrApp.Current!.LocateSpace(XrApp.Current!.Head, XrApp.Current.ReferenceSpace, obj.TimeStamp).Pose;

            var img = new RecordStatsImage()
            {
                ImageTime = obj.TimeStamp,
                Pose = pose,
                XrTime = XrApp.Current.FramePredictedDisplayTime
            };

#if __ANDROID__
            img.BootTime = Android.OS.SystemClock.ElapsedRealtimeNanos();
            img.NanoTime = Java.Lang.JavaSystem.NanoTime();
#endif
            _stats!.Images.Add(img);
        }

        public void UpdateTextures()
        {
            _cameraLeft!.UpdateTexture();
            _cameraRight!.UpdateTexture();
        }

        public void CaptureFrame(Camera activeCamera)
        {
            Debug.Assert(_stats != null);

            var displayTimeXr = XrApp.Current!.FramePredictedDisplayTime;

            var depth = activeCamera.Feature<IEnvDepthProvider>();

            var dephTex = depth!.Acquire(_depthCamera!);

            if (dephTex != null)
            {
                OpenGLRender.Current!.ReadTexture(dephTex, dephTex.Format, 0, 0, _buffers);

                _zStream!.Write(_buffers![0].AsSpan());
                _zStream.Write(_buffers![1].AsSpan());

                _stats.DepthFrame++;
            }

            var rightPose = Pose3.Identity;
            var capPose = Pose3.Identity;
            var leftPose = Pose3.Identity;
            var captureView = Matrix4x4.Identity;

            if (_rightRecorder!.ProcessEncodedFrames(out var tsRight))
            {
                if (tsRight != 0)
                    rightPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, tsRight * 1000).Pose;
                _stats.RightFrame++;
            }

            if (_screenRecorder.ProcessEncodedFrames(out var tsCap))
            {
                if (tsCap != 0)
                    capPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, tsCap * 1000).Pose;
                _stats.ScreenFrame++;
            }


            while (true)
            {
                var hasFrame = _leftRecorder!.ProcessEncodedFrames(out var tsLeft);

                if (!hasFrame)
                    return;

                _stats!.LeftFrame++;

                if (tsLeft != 0)
                    leftPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, tsLeft * 1000).Pose;

                if (tsCap != 0)
                {
                    var screenViews = new View[2];
                    screenViews[0].Type = StructureType.View;
                    screenViews[1].Type = StructureType.View;

                    XrApp.Current.LocateViews(XrApp.Current.ReferenceSpace, tsCap * 1000, screenViews);

                    var captureWord = screenViews[0].Pose.ToPose3().ToMatrix();
                    Matrix4x4.Invert(captureWord, out captureView);

                    //var leftOfs = screenViews[0].Pose.ToPose3().Difference(capPose);

                    //Console.WriteLine(leftOfs.ToString());
                }


                var frameData = new RecordFrameData
                {
                    LeftColor = new EyeData
                    {
                        Proj = activeCamera.Eyes![0].Projection.ToFloatArray(),
                        View = activeCamera.Eyes[0].View.ToFloatArray(),
                        Pose = leftPose,
                        Time = tsLeft,
                        Frame = _stats.LeftFrame
                    },
                    RightColor = new EyeData
                    {
                        Proj = activeCamera.Eyes[1].Projection.ToFloatArray(),
                        View = activeCamera.Eyes[1].View.ToFloatArray(),
                        Pose = rightPose,
                        Time = tsRight,
                        Frame = _stats.RightFrame
                    },
                    LeftDepth = new EyeData
                    {
                        Proj = _depthCamera!.Eyes![0].Projection.ToFloatArray(),
                        View = _depthCamera.Eyes[0].View.ToFloatArray(),
                        Frame = _stats.DepthFrame,
                        Time = displayTimeXr
                    },
                    RightDepth = new EyeData
                    {
                        Proj = _depthCamera.Eyes[1]!.Projection.ToFloatArray(),
                        View = _depthCamera.Eyes[1].View.ToFloatArray(),
                        Frame = _stats.DepthFrame,
                        Time = displayTimeXr
                    },
                    Screen = new EyeData
                    {
                        Time = tsCap,
                        Pose = capPose,
                        Frame = _stats.ScreenFrame,
                        View = captureView.ToFloatArray()
                    }
                };

                if (_frame == 0)
                {
                    frameData.LeftColor.CameraParams = _cameraLeft!.GetParams();
                    frameData.RightColor.CameraParams = _cameraRight!.GetParams();
                }

                var json = JsonSerializer.Serialize(frameData, JSON_OPT) + "\n";
                _metaStream!.Write(Encoding.UTF8.GetBytes(json));

                _frame++;
            }

        }

        public void StopCapture()
        {
            _isRecording = false;

            _leftRecorder?.StopRecording();
            _leftRecorder = null;

            _rightRecorder?.StopRecording();
            _rightRecorder = null;

            _zStream?.Close();
            _zStream = null;

            _metaStream?.Close();
            _metaStream = null;

            _capture?.StopCapture();
            _capture = null;

            _screenRecorder?.StopRecording();
            _screenRecorder = null;

            var json = JsonSerializer.Serialize(_stats, JSON_OPT);

            File.WriteAllText(Path.Combine(_outPath, "stats.json"), json);
        }

        public bool IsRecording => _isRecording;

        public Texture2D LeftTex => _leftTex;

        public Texture2D RightTex => _rightTex;

        public ICameraDevice? CameraLeft => _cameraLeft;

        public RecordStats? Stats => _stats;

    }
}
