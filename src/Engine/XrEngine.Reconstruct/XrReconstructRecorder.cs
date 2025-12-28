using Common.Interop;
using OpenXr.Framework;
using System.IO.Compression;
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
        private IVideoRecorder? _leftRecorder;
        private IVideoRecorder? _rightRecorder;
        private bool _isRecording;
        private readonly PerspectiveCamera? _depthCamera;
        private readonly IMemoryBuffer<byte>[]? _buffers;
        private GZipStream? _zStream;
        private FileStream? _metaStream;
        private readonly Texture2D _leftTex;
        private readonly Texture2D _rightTex;

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
            var manager = Context.Require<ICameraManager>();
            var outZPath = Path.Combine(outPath, "out-z.bin");
            var outMetaPath = Path.Combine(outPath, "out-meta.json");
            var outPath1 = Path.Combine(outPath, "outL.mp4");
            var outPath2 = Path.Combine(outPath, "outR.mp4");

            foreach (var file in new string[] { outZPath, outMetaPath, outPath1, outPath2 })
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

            _leftSurface = _leftRecorder.StartRecording(outPath1, recOptions);
            _rightSurface = _rightRecorder.StartRecording(outPath2, recOptions);

            await _cameraLeft.StartCaptureAsync(curFormat, _leftTex, _leftSurface);
            await _cameraRight.StartCaptureAsync(curFormat, _rightTex, _rightSurface);

            _isRecording = true;
        }

        public void UpdateTextures()
        {
            _cameraLeft!.UpdateTexture();
            _cameraRight!.UpdateTexture();
        }

        public void CaptureFrame(Camera activeCamera)
        {

            var tsLeft = _leftRecorder!.ProcessEncodedFrames();
            var tsRight = _rightRecorder!.ProcessEncodedFrames();

            if (tsLeft == 0 || tsRight == 0)
                return;

            var displayTimeXr = XrApp.Current!.FramePredictedDisplayTime;

            var displayTime = XrApp.Current.XrTimeToBootTime(displayTimeXr) / 1000;

            var depth = activeCamera.Feature<IEnvDepthProvider>();

            var dephTex = depth!.Acquire(_depthCamera!);

            if (dephTex != null)
            {
                OpenGLRender.Current!.ReadTexture(dephTex, dephTex.Format, 0, 0, _buffers);

                _zStream!.Write(_buffers![0].AsSpan());
                _zStream.Write(_buffers![1].AsSpan());
            }

            var leftPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, tsLeft * 1000).Pose;

            var rightPose = XrApp.Current.LocateSpace(XrApp.Current.Head, XrApp.Current.ReferenceSpace, tsRight * 1000).Pose;

            var frameData = new RecordFrameData
            {
                Frame = _frame++,
                Time = displayTime,
                LeftColor = new EyeData
                {
                    Proj = activeCamera.Eyes![0].Projection.ToFloatArray(),
                    View = activeCamera.Eyes[0].View.ToFloatArray(),
                    Pose = leftPose,
                    Time = tsLeft,
                },
                RightColor = new EyeData
                {
                    Proj = activeCamera.Eyes[1].Projection.ToFloatArray(),
                    View = activeCamera.Eyes[1].View.ToFloatArray(),
                    Pose = rightPose,
                    Time = tsRight
                },
                LeftDepth = new EyeData
                {
                    Proj = _depthCamera!.Eyes![0].Projection.ToFloatArray(),
                    View = _depthCamera.Eyes[0].View.ToFloatArray(),
                },
                RightDepth = new EyeData
                {
                    Proj = _depthCamera.Eyes[1]!.Projection.ToFloatArray(),
                    View = _depthCamera.Eyes[1].View.ToFloatArray(),
                }
            };

            if (_frame == 1)
            {
                frameData.LeftColor.CameraParams = _cameraLeft!.GetParams();
                frameData.RightColor.CameraParams = _cameraRight!.GetParams();
            }

            var json = JsonSerializer.Serialize(frameData, JSON_OPT) + "\n";
            _metaStream!.Write(Encoding.UTF8.GetBytes(json));
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
        }

        public bool IsRecording => _isRecording;

        public Texture2D LeftTex => _leftTex;

        public Texture2D RightTex => _rightTex;

    }
}
