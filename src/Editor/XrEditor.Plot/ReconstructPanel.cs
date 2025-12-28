using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using XrEditor.Abstraction;
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
        private ImageView? _colorImage;
        private XrReconstructReader? _reader;
        private int _frameIndex;
        private CameraEye _activeEye;
        private VectorAxis _axis;
        private double _overlay;
        private EyesFrame<DepthFrame>? _curDepth;
        private EyesFrame<ColorFrame>? _curColor;
        private int _colorOffset;
        private ImageView? _colorProjImage;
        private float _fovScale;
        private Vector2 _center;

        public ReconstructPanel()
        {
            _overlay = 0f;
            _fovScale = 1f;
        }

        protected override Task LoadAsync()
        {
            _reader = new XrReconstructReader();
            _reader.Open("D:\\New folder");
            OnPropertyChanged(nameof(TotalFrames));
            ReadFrame(0);
            return base.LoadAsync();
        }

        public void ReadFrame(int frameIndex)
        {
            if (_reader == null)
                return;

            _curDepth = _reader.ReadDepth(frameIndex);

            var bestColor = _reader.FindColorForDepth(_frameIndex);

            ColorOffset = (bestColor - frameIndex);

            _curColor = _reader.ReadColor(bestColor);

            UpdateImages();
        }

        protected void UpdateImages()
        {
            Debug.Assert(_curDepth != null);
            Debug.Assert(_curColor != null);
            Debug.Assert(_reader != null);

            var depthEye = _activeEye == CameraEye.Left ? _curDepth.Left : _curDepth.Right;

            _reader.ReconstructDepth(depthEye, _curDepth.Width, _curDepth.Height);
            _reader.ComputeStats(depthEye, 2);

            _reader.GenerateImage(depthEye, a => a.Y, 0, 2.5f);

            DepthImage = new ImageView
            {
                ScaleY = 1,
                Image = Context.Require<IImageFactory>().CreateImage(
                    depthEye.ImageData!.AsSpan(),
                    _curDepth.Width,
                    _curDepth.Height,
                    TextureFormat.GrayInt8)
            };


            var repData = _reader.FusionLeft.AlignColorToDepth(
                _curDepth.Left.ProjData!.AsSpan(),
                (int)_curDepth.Width,
                (int)_curDepth.Height,
                _curDepth.Left.View,
                _curDepth.Left.Proj,
                _curColor.Left.Data!.AsSpan(),
                (int)_curColor.Width,
                (int)_curColor.Height,
                _curColor.Left.View,
                _curColor.Left.Pose!.Value.ToMatrix(),
                _fovScale,
                _center);

            ColorProjImage = new ImageView
            {
                ScaleY = 1,
                Image = Context.Require<IImageFactory>().CreateImage(
                   repData,
                   _curDepth.Width,
                   _curDepth.Height,
                   TextureFormat.Rgb24)
            };

            ColorImage = new ImageView
            {
                ScaleY = 1,
                Image = Context.Require<IImageFactory>().CreateImage(
               _curColor.Left.Data.AsSpan(),
               _curColor.Width,
               _curColor.Height,
               TextureFormat.Rgb24)
            };
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

        public int TotalFrames => (_reader?.Meta?.Count - 1) ?? 0;

        public ImageView? DepthImage
        {
            get => _colorImage;
            set
            {
                _colorImage = value;
                OnPropertyChanged(nameof(DepthImage));
            }
        }

        public ImageView? ColorImage
        {
            get => _colorImage;
            set
            {
                _colorImage = value;
                OnPropertyChanged(nameof(ColorImage));
            }
        }

        public ImageView? ColorProjImage
        {
            get => _colorProjImage;
            set
            {
                _colorProjImage = value;
                OnPropertyChanged(nameof(ColorProjImage));
            }
        }

        public VectorAxis Axis
        {
            get => _axis;
            set
            {
                _axis = value;
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
        public XrReconstructReader Reader => _reader;

        public override string? Title => "Reconstruct";
    }
}
