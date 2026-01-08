using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Windows.Controls;
using XrEngine;
using XrEngine.Reconstruct;
using XrMath;

namespace XrEditor.Plot
{
    public enum VectorAxis
    {
        X,
        Y,
        Z
    }

    public enum ColorMode
    {
        Camera,
        Screen
    }

    public enum CameraEye
    {
        Left,
        Right
    }

    [Panel("d8df1d38-be27-457b-9128-8e4a2e6bf1c1")]
    [DisplayName("Reconstruct")]
    [StateManager(StateManagerMode.Explicit)]
    public class ReconstructPanel : BasePanel
    {
        private XrReconstructReader? _reader;
        private int _frameIndex;
        private CameraEye _activeEye;
        private VectorAxis _axis;
        private double _overlay;
        private EyesFrame<DepthFrame>? _curDepth;
        private EyesFrame<ColorFrame>? _curColor;
        private int _colorOffset;
        private float _fovScale;
        private Vector2 _center;
        private ColorMode _colorMode;
        private ColorFrame? _curScreen;
        private bool _autoOffset;
        private float _depthCutOff;
        private Bounds3 _roomBounds;

        public ReconstructPanel()
        {
            _overlay = 0f;
            _fovScale = 1f;
            _center.X = -530;
            _autoOffset = true;
            _depthCutOff = 1;
            _axis = VectorAxis.Y;

            _roomBounds = new Bounds3
            {
                Min = new Vector3(-3, 0, -3),
                Max = new Vector3(3, 3.3f, 3),
            };

            DepthImage.MouseMove += OnDepthMouseMove;

            VoxelSize = 0.01f;
            VoxelMinHits = 2;
            VoxelStartFrame = 141;
            VoxelEndFrame = 151;
            ExportPointsCommand = new Command(ExportPoints);
        }

        protected override Task LoadAsync()
        {
            _reader = XrReconstructReader.Current;
            _reader.Open("D:\\Projects\\XrEditor\\Capture");

            OnPropertyChanged(nameof(TotalFrames));
            ReadFrame(0);

            return base.LoadAsync();
        }

        void ExportPoints()
        {
            var volume = _reader!.ComputeVoxels(_roomBounds, VoxelSize, VoxelStartFrame, VoxelEndFrame, DepthCutOff);

            var points = _reader.ExtractPoints(volume, _roomBounds, VoxelSize, VoxelMinHits);

            _reader.SavePoints("d:\\points.xyz", points);

            Log.Info(this, "{0} Points Exported!", points.Count);
        }

        public void ReadFrame(int frameIndex)
        {
            if (_reader == null)
                return;

            _curDepth = _reader.ReadDepth(frameIndex);

            if (_colorMode == ColorMode.Camera)
            {
                if (_autoOffset)
                {
                    var bestColor = _reader.FindColorForDepth(_frameIndex);

                    _colorOffset = (bestColor - frameIndex);

                }
                if (frameIndex + _colorOffset >= TotalFrames)
                    _colorOffset = TotalFrames - frameIndex - 1;
                _curColor = _reader.ReadColor(frameIndex + _colorOffset);
            }
            else
            {
                if (_autoOffset)
                {
                    var bestColor = _reader.FindScreenForDepth(_frameIndex);

                    _colorOffset = (bestColor - frameIndex);

                }

                if (frameIndex + _colorOffset >= TotalFrames)
                    _colorOffset = TotalFrames - frameIndex - 2;
                _curScreen = _reader.ReadScreen(frameIndex + _colorOffset);

            }

            OnPropertyChanged(nameof(ColorOffset));

            UpdateImages();
        }

        protected void UpdateImages()
        {
            if (_curDepth == null)
                return;

            Debug.Assert(_curColor != null);
            Debug.Assert(_reader != null);

            var depthEye = _activeEye == CameraEye.Left ? _curDepth.Left : _curDepth.Right;

            _reader.ReconstructDepth(depthEye, _curDepth.Width, _curDepth.Height, _depthCutOff);
            _reader.ComputeStats(depthEye, 2);

            if (_axis == VectorAxis.X)
                _reader.GenerateImage(depthEye, a => a.X, _roomBounds.Min.X, _roomBounds.Max.X);
            else if (_axis == VectorAxis.Y)
                _reader.GenerateImage(depthEye, a => a.Y, _roomBounds.Min.Y, _roomBounds.Max.Y);
            else if (_axis == VectorAxis.Z)
                _reader.GenerateImage(depthEye, a => a.Z, _roomBounds.Min.Z, _roomBounds.Max.Z);

            _reader.ComputeStatsProj(depthEye);

            DepthImage.Image = null;

            DepthImage.Image = Context.Require<IImageFactory>().CreateImage(
                    depthEye.ImageData!.AsSpan(),
                    _curDepth.Width,
                    _curDepth.Height,
                    TextureFormat.GrayInt8);

            byte[] repData;

            if (_colorMode == ColorMode.Screen)
            {
                repData = _reader.AlignColorToDepth(
                     _curDepth.Left.ProjData!.AsSpan(),
                     (int)_curDepth.Width,
                     (int)_curDepth.Height,
                     depthEye.View,
                     depthEye.Proj,
                     _curScreen!.Data!.AsSpan(),
                     1280,
                     1280,
                     _curScreen.Pose!.Value,
                     _reader.LeftCamera!,
                     _fovScale,
                     _center,
                     true);

                ColorImage.Image = Context.Require<IImageFactory>().CreateImage(
                    _curScreen.Data.AsSpan(),
                    1280,
                    1280,
                    TextureFormat.Rgb24);
            }
            else
            {
                var colorEye = _activeEye == CameraEye.Left ? _curColor.Left : _curColor.Right;

                repData = _reader.AlignColorToDepth(
                     _curDepth.Left.ProjData!.AsSpan(),
                     (int)_curDepth.Width,
                     (int)_curDepth.Height,
                     depthEye.View,
                     depthEye.Proj,
                     colorEye.Data!.AsSpan(),
                     (int)_curColor.Width,
                     (int)_curColor.Height,
                      colorEye.Pose!.Value,
                     _reader.LeftCamera!,
                     _fovScale,
                     _center,
                     true);

                ColorImage.Image = Context.Require<IImageFactory>().CreateImage(
                     colorEye.Data.AsSpan(),
                     _curColor.Width,
                     _curColor.Height,
                     TextureFormat.Rgb24);

            }

            ColorProjImage.Image = Context.Require<IImageFactory>().CreateImage(
                   repData,
                   _curDepth.Width,
                   _curDepth.Height,
                   TextureFormat.Rgb24);

            Log.Info(this, "Depth: Min: {0} - Max: {1} - Size: {2}", depthEye.StatsProj.Min, depthEye.StatsProj.Max, (depthEye.StatsProj.Max - depthEye.StatsProj.Min));
        }

        private void OnDepthMouseMove(object? sender, ImageMouseMoveArgs e)
        {
            if (_curDepth == null)
                return;
            var proj = _curDepth.Left.ProjData!.AsSpan()[e.X + e.Y * (int)_curDepth.Width];
            DepthPoint = string.Format("({0} {1}) - ({2} {3} {4})", e.X, e.Y, proj.X, proj.Y, proj.Y);
            OnPropertyChanged(nameof(DepthPoint));
        }

        public int FrameIndex
        {
            get => _frameIndex;
            set
            {
                if (_frameIndex == value)
                    return;
                _frameIndex = value;
                ReadFrame(_frameIndex);
                OnPropertyChanged(nameof(FrameIndex));
            }
        }

        public int ColorOffset
        {
            get => _colorOffset;
            set
            {
                if (_colorOffset == value)
                    return;
                _colorOffset = value;
                ReadFrame(_frameIndex);
                OnPropertyChanged(nameof(ColorOffset));
            }
        }

        public VectorAxis Axis
        {
            get => _axis;
            set
            {
                _axis = value;
                UpdateImages();
                OnPropertyChanged(nameof(Axis));
            }
        }

        public CameraEye ActiveEye
        {
            get => _activeEye;
            set
            {
                _activeEye = value;
                OnPropertyChanged(nameof(ActiveEye));
            }
        }

        public double OpacityOverlay
        {
            get => _overlay;
            set
            {
                _overlay = value;
                OnPropertyChanged(nameof(OpacityOverlay));
            }
        }

        public float FovScale
        {
            get => _fovScale;
            set
            {
                _fovScale = value;
                OnPropertyChanged(nameof(FovScale));
                UpdateImages();
            }
        }

        public float DepthCutOff
        {
            get => _depthCutOff;
            set
            {
                _depthCutOff = value;
                OnPropertyChanged(nameof(DepthCutOff));
                UpdateImages();
            }
        }

        public float CenterX
        {
            get => _center.X;
            set
            {
                _center.X = value;
                OnPropertyChanged(nameof(CenterX));
                UpdateImages();
            }
        }

        public float CenterY
        {
            get => _center.Y;
            set
            {
                _center.Y = value;
                OnPropertyChanged(nameof(CenterY));
                UpdateImages();
            }
        }

        public ColorMode ColorMode
        {
            get => _colorMode;
            set
            {
                _colorMode = value;
                OnPropertyChanged(nameof(ColorMode));
                ReadFrame(_frameIndex);
            }
        }

        public bool AutoOffset
        {
            get => _autoOffset;
            set
            {
                _autoOffset = value;
                OnPropertyChanged(nameof(AutoOffset));
                ReadFrame(_frameIndex);

            }
        }

        public Command ExportPointsCommand { get; }

        public float VoxelSize { get; set; }

        public int VoxelMinHits { get; set; }

        public int VoxelStartFrame { get; set; }

        public int VoxelEndFrame { get; set; }

        public string? DepthPoint { get; set; }

        public ImageView DepthImage { get; } = new();

        public ImageView ColorImage { get; } = new();

        public ImageView ColorProjImage { get; } = new();

        public ColorMode[] ColorModeList => [ColorMode.Screen, ColorMode.Camera];

        public VectorAxis[] AxisList => [VectorAxis.X, VectorAxis.Y, VectorAxis.Z];

        public int TotalFrames => (_reader?.Meta?.Count - 1) ?? 0;

        public XrReconstructReader? Reader => _reader;

        public override string? Title => "Reconstruct";
    }
}
